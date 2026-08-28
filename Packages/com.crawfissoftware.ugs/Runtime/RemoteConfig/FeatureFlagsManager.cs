using System;

using Unity.Services.RemoteConfig;

using UnityEngine;

namespace CrawfisSoftware.UGS
{
    public class FeatureFlagsManager
    {
        private FeatureFlags _featureFlags;
        private bool _logValues;

        public event Action<FeatureFlags> OnFeatureFlagsUpdated;
        public FeatureFlags FeatureFlags => _featureFlags;

        public void Initialize(bool logValues = true)
        {
            _logValues = logValues;
            _featureFlags = CreateDefaultFeatureFlags();
        }

        public void UpdateFromRemoteConfig()
        {
            if (RemoteConfigService.Instance.appConfig.HasKey("feature_flags"))
            {
                string features = RemoteConfigService.Instance.appConfig.GetJson("feature_flags");
                JsonUtility.FromJsonOverwrite(features, _featureFlags);
                OnFeatureFlagsUpdated?.Invoke(_featureFlags);
                
                if (_logValues)
                    Debug.Log("Remote feature flags loaded successfully");
            }
            else
            {
                LoadIndividualFeatureFlags();
                OnFeatureFlagsUpdated?.Invoke(_featureFlags);
            }
        }

        private void LoadIndividualFeatureFlags()
        {
            _featureFlags.EnablePowerUps = RemoteConfigService.Instance.appConfig.GetBool("enable_powerups", _featureFlags.EnablePowerUps);
            _featureFlags.EnableLeaderboards = RemoteConfigService.Instance.appConfig.GetBool("enable_leaderboards", _featureFlags.EnableLeaderboards);
            _featureFlags.EnableDailyRewards = RemoteConfigService.Instance.appConfig.GetBool("enable_daily_rewards", _featureFlags.EnableDailyRewards);
            _featureFlags.EnableVideoAds = RemoteConfigService.Instance.appConfig.GetBool("enable_video_ads", _featureFlags.EnableVideoAds);
            _featureFlags.EnableMultiplayer = RemoteConfigService.Instance.appConfig.GetBool("enable_multiplayer", _featureFlags.EnableMultiplayer);
            _featureFlags.EnableDebugMode = RemoteConfigService.Instance.appConfig.GetBool("enable_debug_mode", _featureFlags.EnableDebugMode);
            _featureFlags.EnableAnalytics = RemoteConfigService.Instance.appConfig.GetBool("enable_analytics", _featureFlags.EnableAnalytics);
        }

        private FeatureFlags CreateDefaultFeatureFlags()
        {
            return new FeatureFlags
            {
                EnablePowerUps = true,
                EnableLeaderboards = true,
                EnableDailyRewards = false,
                EnableVideoAds = true,
                EnableMultiplayer = false,
                EnableDebugMode = Application.isEditor,
                EnableAnalytics = true
            };
        }

        public bool IsFeatureEnabled(string featureName)
        {
            return featureName.ToLower() switch
            {
                "powerups" => _featureFlags.EnablePowerUps,
                "leaderboards" => _featureFlags.EnableLeaderboards,
                "dailyrewards" => _featureFlags.EnableDailyRewards,
                "videoads" => _featureFlags.EnableVideoAds,
                "multiplayer" => _featureFlags.EnableMultiplayer,
                "debug" => _featureFlags.EnableDebugMode,
                "analytics" => _featureFlags.EnableAnalytics,
                _ => false
            };
        }

        public void LogValues()
        {
            Debug.Log($"Feature Flags - PowerUps: {_featureFlags.EnablePowerUps}, Leaderboards: {_featureFlags.EnableLeaderboards}");
        }
    }
}