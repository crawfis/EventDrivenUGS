using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Unity.Services.CloudSave;
using Unity.Services.RemoteConfig;

using PlayerData = Unity.Services.CloudSave.Models.Data.Player;
using SaveItem = Unity.Services.CloudSave.Models.SaveItem;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Client-authoritative achievements: definitions from Remote Config, records in the player's
    /// own Cloud Save.
    ///    Dependencies: Unity.Services.CloudSave, Unity.Services.RemoteConfig, Newtonsoft.Json
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>The player's device decides when an achievement is earned, so this is appropriate for
    /// single-player progression and inappropriate for anything competitive. Use
    /// <see cref="CloudCodeAchievementBackend"/> where the answer has to be trusted.</para>
    /// <para>Definitions are read from the already-fetched Remote Config where possible. Refetching
    /// on every load would make opening the achievements panel cost a network round trip that the
    /// boot sequence has usually already paid.</para>
    /// <para>Every Cloud Save call names the <b>Public</b> access class explicitly. Public and
    /// Default are separate server-side stores on separate endpoints, not two views of one store,
    /// so the access class is as much a part of the wire contract as the key name - see
    /// <see cref="CloudSaveKey"/>.</para>
    /// <para>A mutation is a read-modify-write of the whole record array, so it carries the write
    /// lock the read returned and retries on conflict. Without that, two mutations overlapping
    /// inside one round trip both write the pre-state and the later one silently discards the
    /// earlier unlock, while the service still reports success to the player.</para>
    /// </remarks>
    public sealed class CloudSaveAchievementBackend : IAchievementBackend
    {
        /// <summary>
        /// Cloud Save player key holding the record array, read and written in the <b>Public</b>
        /// access class.
        /// <b>Wire contract</b> - changing either the key or the access class orphans every
        /// existing player's progress, silently: the load simply returns nothing and every
        /// achievement reappears locked.
        /// </summary>
        public const string CloudSaveKey = AchievementsRemoteConfigKeys.CloudSaveAchievementsKey;

        /// <summary>
        /// Remote Config key holding the definition array.
        /// <b>Wire contract</b> - must match the deployed Remote Config entry.
        /// </summary>
        public const string RemoteConfigKey = AchievementsRemoteConfigKeys.AchievementsKey;

        /// <summary>
        /// How many times a mutation re-reads and re-applies after losing a write-lock race before
        /// it gives up. Three covers the realistic case of a second writer landing mid-round-trip
        /// without turning a persistently contended key into an unbounded retry storm.
        /// </summary>
        private const int MaxSaveAttempts = 3;

        private struct EmptyAttributes
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Achievement>> GetAchievementsAsync(string playerId)
        {
            List<AchievementDefinition> definitions = await LoadDefinitionsAsync();
            var (records, _) = await LoadRecordsAsync(playerId);

            var achievements = new List<Achievement>(definitions.Count);
            foreach (var definition in definitions)
            {
                if (definition?.Id == null) continue;
                records.TryGetValue(definition.Id, out var record);
                achievements.Add(new Achievement(definition, record));
            }
            return achievements;
        }

        /// <inheritdoc />
        public Task<AchievementRecordDto> UnlockAsync(string achievementId) =>
            MutateAsync(achievementId, record => record.Unlocked = true);

        /// <inheritdoc />
        public Task<AchievementRecordDto> SetProgressAsync(string achievementId, int progressCount) =>
            MutateAsync(achievementId, record => record.ProgressCount = progressCount);

        /// <inheritdoc />
        public async Task ResetAllAsync()
        {
            try
            {
                await CloudSaveService.Instance.Data.Player.DeleteAsync(
                    CloudSaveKey,
                    new PlayerData.DeleteOptions(new PlayerData.PublicWriteAccessClassOptions()));
            }
            catch (Exception e)
            {
                throw Wrap("Could not clear saved achievement records.", e);
            }
        }

        private async Task<AchievementRecordDto> MutateAsync(string achievementId, Action<AchievementRecordDto> mutate)
        {
            if (string.IsNullOrEmpty(achievementId))
                throw new AchievementBackendException("An achievement id is required.");

            for (int attempt = 1; ; attempt++)
            {
                // A null player id makes the SDK resolve the signed-in player, which is the only
                // player a public write can ever target. Reading and writing through the same
                // resolution keeps the pair symmetric.
                var (records, writeLock) = await LoadRecordsAsync(null);
                if (!records.TryGetValue(achievementId, out var record) || record == null)
                {
                    record = new AchievementRecordDto { Id = achievementId };
                    records[achievementId] = record;
                }
                mutate(record);

                try
                {
                    await SaveRecordsAsync(records.Values, writeLock);
                    return record;
                }
                catch (CloudSaveConflictException e)
                {
                    if (attempt >= MaxSaveAttempts)
                    {
                        throw Wrap(
                            $"Could not save achievement records: {MaxSaveAttempts} attempts each lost a write race.", e);
                    }
                    // Re-read and re-apply against whatever the other writer stored. The mutation is
                    // a delegate precisely so it can be replayed rather than overwriting that write.
                }
            }
        }

        private async Task<List<AchievementDefinition>> LoadDefinitionsAsync()
        {
            var remoteConfig = RemoteConfigService.Instance;
            if (remoteConfig.appConfig == null || !remoteConfig.appConfig.HasKey(RemoteConfigKey))
            {
                try
                {
                    await remoteConfig.FetchConfigsAsync(new EmptyAttributes(), new EmptyAttributes());
                }
                catch (Exception e)
                {
                    throw Wrap("Could not fetch achievement definitions from Remote Config.", e);
                }
            }

            if (remoteConfig.appConfig == null || !remoteConfig.appConfig.HasKey(RemoteConfigKey))
            {
                // Two very different causes, and blaming the wrong one costs an afternoon. Remote
                // Config returns an empty config when it is fetched without an authenticated player,
                // which looks exactly like a key that was never deployed.
                bool signedIn = false;
                try
                {
                    signedIn = Unity.Services.Authentication.AuthenticationService.Instance?.IsSignedIn ?? false;
                }
                catch (Exception)
                {
                    // Services not initialised, so certainly not signed in.
                }

                throw new AchievementBackendException(signedIn
                    ? $"Remote Config has no '{RemoteConfigKey}' key. Deploy the achievement definitions before using achievements."
                    : $"Remote Config returned no '{RemoteConfigKey}' key, but it was fetched with no player signed in, "
                      + "so the response was empty regardless of what is deployed. Fetch after authentication.");
            }

            string json = remoteConfig.appConfig.GetJson(RemoteConfigKey);
            return Deserialize<List<AchievementDefinition>>(json, "achievement definitions") ?? new List<AchievementDefinition>();
        }

        /// <summary>
        /// Reads one player's record array, returning the write lock alongside it so a caller that
        /// intends to write back can prove nothing changed in between. A null <paramref name="playerId"/>
        /// resolves to the signed-in player; a null write lock means the key does not exist yet.
        /// </summary>
        private async Task<(Dictionary<string, AchievementRecordDto> Records, string WriteLock)> LoadRecordsAsync(
            string playerId)
        {
            var byId = new Dictionary<string, AchievementRecordDto>();
            string json;
            string writeLock;
            try
            {
                var query = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    new HashSet<string> { CloudSaveKey },
                    new PlayerData.LoadOptions(new PlayerData.PublicReadAccessClassOptions(playerId)));
                if (!query.TryGetValue(CloudSaveKey, out var item) || item == null) return (byId, null);
                json = item.Value.GetAsString();
                writeLock = item.WriteLock;
            }
            catch (Exception e)
            {
                throw Wrap("Could not read saved achievement records.", e);
            }

            if (string.IsNullOrWhiteSpace(json)) return (byId, writeLock);

            var records = Deserialize<List<AchievementRecordDto>>(json, "achievement records");
            if (records == null) return (byId, writeLock);

            foreach (var record in records)
            {
                if (record?.Id == null) continue;
                byId[record.Id] = record;
            }
            return (byId, writeLock);
        }

        /// <summary>
        /// Writes the record array back under the write lock the matching read returned.
        /// </summary>
        /// <remarks>
        /// A null <paramref name="writeLock"/> is the create case: Cloud Save has no lock to compare
        /// against for a key that does not exist yet, and passing one would fail. Two clients
        /// creating the key at the same instant is therefore still last-write-wins - it is the one
        /// window optimistic concurrency cannot close, and it can only happen once per player.
        /// </remarks>
        private async Task SaveRecordsAsync(IEnumerable<AchievementRecordDto> records, string writeLock)
        {
            string json = JsonConvert.SerializeObject(new List<AchievementRecordDto>(records));
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(
                    new Dictionary<string, SaveItem> { { CloudSaveKey, new SaveItem(json, writeLock) } },
                    new PlayerData.SaveOptions(new PlayerData.PublicWriteAccessClassOptions()));
            }
            catch (CloudSaveConflictException)
            {
                // The caller retries this one. Wrapping it here would hide the only failure that is
                // recoverable behind a message saying the write is impossible.
                throw;
            }
            catch (Exception e)
            {
                throw Wrap("Could not save achievement records.", e);
            }
        }

        private static T Deserialize<T>(string json, string what) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException e)
            {
                throw new AchievementBackendException($"Could not read {what}: the stored JSON is malformed.", e);
            }
        }

        private static AchievementBackendException Wrap(string message, Exception inner)
        {
            var wrapped = new AchievementBackendException(message, inner);
            if (wrapped.IsAccessDenied)
            {
                // Cloud Save maps every 403 to KeyLimitExceeded, so the status alone cannot tell an
                // access-policy denial from a full key store. Naming both beats confidently naming
                // the wrong one.
                return new AchievementBackendException(
                    message + " Cloud Save refused the operation with a 403. Either the project's access policy " +
                    "denies player writes to urn:ugs:cloud-save:*, or this player is at the key-value-pair limit. " +
                    "Allow player writes for this key, or switch the achievements panel to the trusted (Cloud Code) " +
                    "backend.", inner);
            }
            return wrapped;
        }
    }
}
