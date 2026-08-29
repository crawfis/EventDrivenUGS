using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Unity.Services.CloudSave;
using Unity.Services.RemoteConfig;

using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;

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
    /// </remarks>
    public sealed class CloudSaveAchievementBackend : IAchievementBackend
    {
        /// <summary>
        /// Cloud Save player key holding the record array.
        /// <b>Wire contract</b> - changing it orphans every existing player's progress.
        /// </summary>
        public const string CloudSaveKey = AchievementsRemoteConfigKeys.CloudSaveAchievementsKey;

        /// <summary>
        /// Remote Config key holding the definition array.
        /// <b>Wire contract</b> - must match the deployed Remote Config entry.
        /// </summary>
        public const string RemoteConfigKey = AchievementsRemoteConfigKeys.AchievementsKey;

        private struct EmptyAttributes
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Achievement>> GetAchievementsAsync(string playerId)
        {
            List<AchievementDefinition> definitions = await LoadDefinitionsAsync();
            Dictionary<string, AchievementRecordDto> records = await LoadRecordsAsync();

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
                await CloudSaveService.Instance.Data.Player.DeleteAsync(CloudSaveKey, new PlayerDeleteOptions());
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

            Dictionary<string, AchievementRecordDto> records = await LoadRecordsAsync();
            if (!records.TryGetValue(achievementId, out var record) || record == null)
            {
                record = new AchievementRecordDto { Id = achievementId };
                records[achievementId] = record;
            }
            mutate(record);

            await SaveRecordsAsync(records.Values);
            return record;
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
                throw new AchievementBackendException(
                    $"Remote Config has no '{RemoteConfigKey}' key. Deploy the achievement definitions before using achievements.");
            }

            string json = remoteConfig.appConfig.GetJson(RemoteConfigKey);
            return Deserialize<List<AchievementDefinition>>(json, "achievement definitions") ?? new List<AchievementDefinition>();
        }

        private async Task<Dictionary<string, AchievementRecordDto>> LoadRecordsAsync()
        {
            var byId = new Dictionary<string, AchievementRecordDto>();
            string json;
            try
            {
                var query = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    new HashSet<string> { CloudSaveKey });
                if (!query.TryGetValue(CloudSaveKey, out var item) || item == null) return byId;
                json = item.Value.GetAsString();
            }
            catch (Exception e)
            {
                throw Wrap("Could not read saved achievement records.", e);
            }

            if (string.IsNullOrWhiteSpace(json)) return byId;

            var records = Deserialize<List<AchievementRecordDto>>(json, "achievement records");
            if (records == null) return byId;

            foreach (var record in records)
            {
                if (record?.Id == null) continue;
                byId[record.Id] = record;
            }
            return byId;
        }

        private async Task SaveRecordsAsync(IEnumerable<AchievementRecordDto> records)
        {
            string json = JsonConvert.SerializeObject(new List<AchievementRecordDto>(records));
            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(
                    new Dictionary<string, object> { { CloudSaveKey, json } });
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
                return new AchievementBackendException(
                    message + " The project's access policy denies player writes to saved data, so this backend " +
                    "cannot be used. Either allow player writes for this key, or switch the achievements panel to " +
                    "the trusted (Cloud Code) backend.", inner);
            }
            return wrapped;
        }
    }
}
