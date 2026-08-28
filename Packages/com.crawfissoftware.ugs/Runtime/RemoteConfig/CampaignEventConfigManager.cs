using System;

using Unity.Services.RemoteConfig;

using UnityEngine;

namespace CrawfisSoftware.UGS
{
    public class CampaignEventConfigManager
    {
        private CampaignEventConfig _eventConfig;
        private bool _logValues;

        public event Action<CampaignEventConfig> OnEventConfigUpdated;
        public CampaignEventConfig EventConfig => _eventConfig;

        public void Initialize(bool logValues = true)
        {
            _logValues = logValues;
            _eventConfig = CreateDefaultEventConfig();
        }

        public void UpdateFromRemoteConfig()
        {
            if (RemoteConfigService.Instance.appConfig.HasKey("event_config"))
            {
                string eventConfigJson = RemoteConfigService.Instance.appConfig.GetJson("event_config");
                JsonUtility.FromJsonOverwrite(eventConfigJson, _eventConfig);
                OnEventConfigUpdated?.Invoke(_eventConfig);
                
                if (_logValues)
                    Debug.Log("Remote event configuration loaded successfully");
            }
        }

        private CampaignEventConfig CreateDefaultEventConfig()
        {
            return new CampaignEventConfig
            {
                EventName = "",
                EventStartTime = 0,
                EventEndTime = 0,
                IsEventActive = false,
                EventScoreMultiplier = 1.0f,
                EventTheme = "default"
            };
        }

        public void LogValues()
        {
            Debug.Log($"Event - Active: {_eventConfig.IsEventActive}, Name: {_eventConfig.EventName}");
        }
    }
}