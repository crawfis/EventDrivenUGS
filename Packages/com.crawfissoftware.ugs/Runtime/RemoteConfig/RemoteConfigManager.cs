using CrawfisSoftware.UGS.Events;

using System;

using Unity.Services.RemoteConfig;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS
{
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

        }
        private void OnFetchRemoteConfig(string eventName, object sender, object data)
        {
            Initialize(_logRemoteConfigValues);
        }

        public void Initialize(bool logValues = true)
        {
            if (_isInitialized) return;

            _logRemoteConfigValues = logValues;
            
            //InitializeManagers(difficultyLevel);
            InitializeRemoteConfig();

            _isInitialized = true;
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

        private async void InitializeRemoteConfig()
        {
            if (RemoteConfigService.Instance != null)
            {
                //RemoteConfigService.Instance.SetEnvironmentID("initial_development");
                RemoteConfigService.Instance.FetchCompleted += OnRemoteConfigFetched;
                var userAttributes = CreateUserAttributes();
                var appAttributes = CreateAppAttributes();
                RuntimeConfig configs = await RemoteConfigService.Instance.FetchConfigsAsync(userAttributes, appAttributes);
                foreach (var key in configs.GetKeys())
                {
                    Debug.Log($"Initial Remote Config Key: {key}");
                }
            }
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