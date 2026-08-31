using CrawfisSoftware.Config;
using CrawfisSoftware.UGS.Events;
using CrawfisSoftware.UGS.RemoteConfig;

using System;
using System.Collections.Generic;

using Unity.Services.RemoteConfig;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS
{
    /// <summary>
    /// Fetches Remote Config once the services report they are ready, and announces the result.
    ///    Dependencies: Unity.Services.RemoteConfig, UGS_State
    ///    Subscribes: UGS_EventsEnum.RemoteConfigFetching
    ///    Publishes: UGS_EventsEnum.RemoteConfigFetched, RemoteConfigUpdated,
    ///               DifficultySettingsFetched, RemoteConfigFetchFailed, RemoteConfigFailed
    /// </summary>
    /// <remarks>
    /// <para><b>One fetch per process.</b> A second <c>RemoteConfigFetching</c> is ignored once a
    /// fetch has succeeded, because config that changed mid-session would otherwise reapply under
    /// a player already partway through a run. A host that genuinely wants to re-read - a different
    /// player signing in, say - calls <see cref="Dispose"/> and then <see cref="Initialize"/>,
    /// which is the only supported way to ask for a second fetch.</para>
    /// <para>A <em>failed</em> fetch does not latch, so a retry is always possible.</para>
    /// <para><b>This is the only Remote Config fetch in the package.</b> The difficulty table used
    /// to be read by a separate <c>DifficultyObserver</c> that watched for authentication and then
    /// fetched again on its own - a second round trip for a payload this fetch had already
    /// downloaded, and one nothing ever constructed, so the signal was never published at all.
    /// Reading the table out of the response we already have is why that type is gone.</para>
    /// </remarks>
    public class RemoteConfigManager : MonoBehaviour, IDisposable
    {
        [SerializeField] private bool _logRemoteConfigValues = true;

        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            if (UGS_State.IsRemoteConfigFetching)
            {
                OnFetchRemoteConfig("RemoteConfig Fetching", this, null);
            }
            else
            {
                UGSBus.Unsubscribe(UGS_EventsEnum.RemoteConfigFetching, OnFetchRemoteConfig);
                UGSBus.Subscribe(UGS_EventsEnum.RemoteConfigFetching, OnFetchRemoteConfig);
            }
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.RemoteConfigFetching, OnFetchRemoteConfig);

            // RemoteConfigService is a static singleton that outlives this component, so a fetch
            // still in flight when the scene unloads would call back into a destroyed MonoBehaviour.
            Dispose();
        }

        private void OnFetchRemoteConfig(string eventName, object sender, object data)
        {
            Initialize(_logRemoteConfigValues);
        }

        public void Initialize(bool logValues = true)
        {
            if (_isInitialized) return;

            _logRemoteConfigValues = logValues;

            // Set before starting the fetch, not after: the fetch is asynchronous, so a second
            // RemoteConfigFetching arriving while it is in flight would otherwise start another.
            // InitializeRemoteConfig clears it again if the fetch fails.
            _isInitialized = true;

            InitializeRemoteConfig();
        }

        // async void, so no caller can ever observe what this throws. Anything it throws has to be
        // caught here or it is lost entirely - and losing it means the boot waits forever for a
        // RemoteConfigFetched that is never coming.
        private async void InitializeRemoteConfig()
        {
            if (RemoteConfigService.Instance == null)
            {
                FailFetch("the Remote Config service is unavailable", null);
                return;
            }

            try
            {
                // Remove first: Initialize runs again after a failure, and the SDK event would
                // otherwise accumulate one handler per attempt.
                RemoteConfigService.Instance.FetchCompleted -= OnRemoteConfigFetched;
                RemoteConfigService.Instance.FetchCompleted += OnRemoteConfigFetched;

                var userAttributes = CreateUserAttributes();
                var appAttributes = CreateAppAttributes();
                RuntimeConfig configs = await RemoteConfigService.Instance.FetchConfigsAsync(userAttributes, appAttributes);
                foreach (var key in configs.GetKeys())
                {
                    Debug.Log($"Initial Remote Config Key: {key}");
                }
            }
            catch (Exception e)
            {
                FailFetch($"the fetch threw - {e.Message}", e);
            }
        }

        /// <summary>
        /// Report a fetch that will never produce a response, and un-latch so a retry is possible.
        /// </summary>
        private void FailFetch(string reason, Exception e)
        {
            if (RemoteConfigService.Instance != null)
                RemoteConfigService.Instance.FetchCompleted -= OnRemoteConfigFetched;

            _isInitialized = false;

            Debug.LogWarning($"{nameof(RemoteConfigManager)}: Remote Config was not fetched because {reason}.");
            UGSBus.Publish(UGS_EventsEnum.RemoteConfigFetchFailed, this, e?.Message ?? reason);
        }

        private UserAttributes CreateUserAttributes()
        {
            return new UserAttributes
            {
                DeviceType = SystemInfo.deviceType.ToString(),
                Platform = Application.platform.ToString(),
                AppVersion = Application.version,
                PlayerLevel = PlayerPrefs.GetInt("PlayerLevel", 1),
                Country = Application.systemLanguage.ToString()
            };
        }

        private AppAttributes CreateAppAttributes()
        {
            return new AppAttributes
            {
                AppVersion = Application.version,
                BuildNumber = Application.buildGUID,
                UnityVersion = Application.unityVersion,
                IsDebugBuild = Debug.isDebugBuild
            };
        }

        private void OnRemoteConfigFetched(ConfigResponse response)
        {
            RemoteConfigService.Instance.FetchCompleted -= OnRemoteConfigFetched;
            if (response.status == ConfigRequestStatus.Success)
            {
                Debug.Log($"Remote config fetched with response: {response.status}");
                UGSBus.Publish(UGS_EventsEnum.RemoteConfigFetched, this, response.status);
                ApplyRemoteConfig();
                if (_logRemoteConfigValues)
                {
                    LogRemoteConfigValues();
                }
            }
            else
            {
                Debug.LogWarning($"Remote Config fetch failed: {response.status}");
                UGSBus.Publish(UGS_EventsEnum.RemoteConfigFailed, this, response.status);
            }
        }

        private void ApplyRemoteConfig()
        {
            try
            {
                PublishDifficultySettings();
                UGSBus.Publish(UGS_EventsEnum.RemoteConfigUpdated, this, UnityEngine.Time.time);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to apply remote configuration: {e.Message}");
                UGSBus.Publish(UGS_EventsEnum.RemoteConfigFailed, this, e.Message);
            }
        }

        /// <summary>
        /// Announce the difficulty table carried by the config just fetched, if there is one.
        /// </summary>
        /// <remarks>
        /// A missing key is not a failure and is deliberately not reported as one. A game ships its
        /// own difficulty configs and only lets the environment override them, so publishing
        /// nothing leaves those local defaults standing - which is the correct behaviour for an
        /// environment that has never had the key deployed, and for an offline run.
        /// </remarks>
        private void PublishDifficultySettings()
        {
            RuntimeConfig appConfig = RemoteConfigService.Instance.appConfig;
            string key = RemoteConfigConstants.difficultySettingsKey;

            if (!appConfig.HasKey(key))
            {
                Debug.Log($"{nameof(RemoteConfigManager)}: no '{key}' in this environment, so the game keeps its local difficulty configs.");
                return;
            }

            // config[key] is the raw JSON token, not one of the typed getters: the value is an
            // array of objects, which RuntimeConfig has no accessor for.
            List<DifficultyConfig> difficulties = appConfig.config[key]?.ToObject<List<DifficultyConfig>>();
            if (difficulties == null || difficulties.Count == 0)
            {
                Debug.LogWarning($"{nameof(RemoteConfigManager)}: '{key}' is present but held no difficulty configs, so the game keeps its local ones.");
                return;
            }

            UGSBus.Publish(UGS_EventsEnum.DifficultySettingsFetched, this, difficulties);
        }

        private void LogRemoteConfigValues()
        {
            Debug.Log("=== Remote Config Values ===");
            foreach (var key in RemoteConfigService.Instance.appConfig.GetKeys())
            {
                Debug.Log($"Key: {key}");
            }
        }

        public void Dispose()
        {
            if (RemoteConfigService.Instance != null)
            {
                RemoteConfigService.Instance.FetchCompleted -= OnRemoteConfigFetched;
            }
            _isInitialized = false;
        }
    }
}
