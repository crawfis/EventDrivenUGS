using CrawfisSoftware.UGS.Events;

using System;

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
    ///               RemoteConfigFetchFailed, RemoteConfigFailed
    /// </summary>
    /// <remarks>
    /// <para><b>One fetch per process.</b> A second <c>RemoteConfigFetching</c> is ignored once a
    /// fetch has succeeded, because config that changed mid-session would otherwise reapply under
    /// a player already partway through a run. A host that genuinely wants to re-read - a different
    /// player signing in, say - calls <see cref="Dispose"/> and then <see cref="Initialize"/>,
    /// which is the only supported way to ask for a second fetch.</para>
    /// <para>A <em>failed</em> fetch does not latch, so a retry is always possible.</para>
    /// </remarks>
    public class RemoteConfigManager : MonoBehaviour, IDisposable
    {
        //[SerializeField] private string _remoteConfigDifficultyLevel = "Hard";
        [SerializeField] private bool _logRemoteConfigValues = true;

        //private GameDifficultyManager _gameDifficultyManager;
        //private FeatureFlagsManager _featureFlagsManager;
        //private GameBalanceManager _gameBalanceManager;
        //private CampaignEventConfigManager _eventConfigManager;
        private bool _isInitialized = false;

        //public FeatureFlags FeatureFlags => _featureFlagsManager?.FeatureFlags ?? default;
        //public GameBalance GameBalance => _gameBalanceManager?.GameBalance ?? default;
        //public CampaignEventConfig EventConfig => _eventConfigManager?.EventConfig ?? default;
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            //if(UnityServices.Instance != null && UnityServices.Instance.State == ServicesInitializationState.Initialized)
            //if (UnityServices.State == ServicesInitializationState.Initialized)
            if(UGS_State.IsRemoteConfigFetching)
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

            //InitializeManagers(difficultyLevel);
            InitializeRemoteConfig();
        }

        //private void InitializeManagers(string difficultyLevel)
        //{
        //    _gameDifficultyManager = new GameDifficultyManager();
        //    _featureFlagsManager = new FeatureFlagsManager();
        //    _gameBalanceManager = new GameBalanceManager();
        //    _eventConfigManager = new CampaignEventConfigManager();

        //    _gameDifficultyManager.Initialize(difficultyLevel, _logRemoteConfigValues);
        //    _featureFlagsManager.Initialize(_logRemoteConfigValues);
        //    _gameBalanceManager.Initialize(_logRemoteConfigValues);
        //    _eventConfigManager.Initialize(_logRemoteConfigValues);
        //}

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
                //RemoteConfigService.Instance.SetEnvironmentID("initial_development");

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
                //_gameDifficultyManager.UpdateFromRemoteConfig();
                //_featureFlagsManager?.UpdateFromRemoteConfig();
                //_gameBalanceManager?.UpdateFromRemoteConfig();
                //_eventConfigManager?.UpdateFromRemoteConfig();
                UGSBus.Publish(UGS_EventsEnum.RemoteConfigUpdated, this, UnityEngine.Time.time);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to apply remote configuration: {e.Message}");
                UGSBus.Publish(UGS_EventsEnum.RemoteConfigFailed, this, e.Message);
            }
        }

        //public bool IsFeatureEnabled(string featureName)
        //{
        //    return _featureFlagsManager?.IsFeatureEnabled(featureName) ?? false;
        //}

        private void LogRemoteConfigValues()
        {
            Debug.Log("=== Remote Config Values ===");
            foreach (var key in RemoteConfigService.Instance.appConfig.GetKeys())
            {
                Debug.Log($"Key: {key}");
            }
            //_featureFlagsManager?.LogValues();
            //_gameBalanceManager?.LogValues();
            //_eventConfigManager?.LogValues();
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