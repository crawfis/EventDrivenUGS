using System;

using UnityEngine;

namespace CrawfisSoftware.UGS
{
    [Serializable]
    public struct FeatureFlags
    {
        [Tooltip("Enable power-up system")]
        public bool EnablePowerUps;
        
        [Tooltip("Enable leaderboard functionality")]
        public bool EnableLeaderboards;
        
        [Tooltip("Enable daily rewards")]
        public bool EnableDailyRewards;
        
        [Tooltip("Enable video advertisements")]
        public bool EnableVideoAds;
        
        [Tooltip("Enable multiplayer features")]
        public bool EnableMultiplayer;
        
        [Tooltip("Enable debug mode")]
        public bool EnableDebugMode;
        
        [Tooltip("Enable analytics tracking")]
        public bool EnableAnalytics;
    }
}