using System;

using UnityEngine;

namespace CrawfisSoftware.UGS
{
    [Serializable]
    public struct CampaignEventConfig
    {
        [Tooltip("Current active event name")]
        public string EventName;
        
        [Tooltip("Event start time (UTC timestamp)")]
        public long EventStartTime;
        
        [Tooltip("Event end time (UTC timestamp)")]
        public long EventEndTime;
        
        [Tooltip("Is event currently active")]
        public bool IsEventActive;
        
        [Tooltip("Event bonus score multiplier")]
        public float EventScoreMultiplier;
        
        [Tooltip("Event theme name")]
        public string EventTheme;
    }
}