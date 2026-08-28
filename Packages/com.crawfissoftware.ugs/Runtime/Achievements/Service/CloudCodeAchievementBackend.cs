using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Unity.Services.CloudCode;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Server-authoritative achievements: every read and write goes through a Cloud Code module,
    /// so the client never decides what was earned.
    ///    Dependencies: Unity.Services.CloudCode
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Calls the module through <c>CallModuleEndpointAsync</c> with request and response
    /// types declared in this package, rather than through generated bindings. Generated bindings
    /// are emitted into the consumer's own Assets folder under a fixed assembly name, and a package
    /// assembly cannot reference an assembly that lives there - depending on them would make this
    /// package impossible to compile.</para>
    /// <para>The endpoint names are supplied by <see cref="CloudCodeAchievementEndpoints"/> so the
    /// package never assumes a particular module is deployed.</para>
    /// </remarks>
    public sealed class CloudCodeAchievementBackend : IAchievementBackend
    {
        private readonly CloudCodeAchievementEndpoints _endpoints;

        /// <summary>The module and endpoint names this backend calls.</summary>
        public CloudCodeAchievementEndpoints Endpoints => _endpoints;

        public CloudCodeAchievementBackend(CloudCodeAchievementEndpoints endpoints)
        {
            if (!endpoints.IsComplete)
            {
                throw new AchievementBackendException(
                    "CloudCodeAchievementEndpoints is incomplete. Every module and endpoint name must be set; " +
                    "use CloudCodeAchievementEndpoints.Default as a starting point.");
            }
            _endpoints = endpoints;
        }

        public CloudCodeAchievementBackend() : this(CloudCodeAchievementEndpoints.Default)
        {
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Achievement>> GetAchievementsAsync(string playerId)
        {
            var dtos = await CallAsync<List<AchievementDto>>(
                _endpoints.GetAchievements,
                new Dictionary<string, object> { { "playerId", playerId } });

            var achievements = new List<Achievement>();
            if (dtos == null) return achievements;

            foreach (var dto in dtos)
            {
                if (dto?.Definition?.Id == null) continue;
                achievements.Add(new Achievement(dto.Definition, dto.Record));
            }
            return achievements;
        }

        /// <inheritdoc />
        public Task<AchievementRecordDto> UnlockAsync(string achievementId) =>
            CallAsync<AchievementRecordDto>(
                _endpoints.UnlockAchievement,
                new Dictionary<string, object> { { "achievementId", achievementId } });

        /// <inheritdoc />
        public Task<AchievementRecordDto> SetProgressAsync(string achievementId, int progressCount) =>
            CallAsync<AchievementRecordDto>(
                _endpoints.UpdateAchievementProgress,
                new Dictionary<string, object>
                {
                    { "achievementId", achievementId },
                    { "count", progressCount },
                });

        /// <inheritdoc />
        public async Task ResetAllAsync()
        {
            try
            {
                await CloudCodeService.Instance.CallModuleEndpointAsync(
                    _endpoints.ModuleName, _endpoints.ResetAllAchievements,
                    new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                throw Wrap(_endpoints.ResetAllAchievements, e);
            }
        }

        private async Task<T> CallAsync<T>(string endpoint, Dictionary<string, object> arguments)
        {
            try
            {
                return await CloudCodeService.Instance.CallModuleEndpointAsync<T>(
                    _endpoints.ModuleName, endpoint, arguments);
            }
            catch (Exception e)
            {
                throw Wrap(endpoint, e);
            }
        }

        private AchievementBackendException Wrap(string endpoint, Exception inner) =>
            new AchievementBackendException(
                $"Cloud Code call '{_endpoints.ModuleName}/{endpoint}' failed. " +
                "Check that the module is deployed and that the endpoint name matches.", inner);
    }
}
