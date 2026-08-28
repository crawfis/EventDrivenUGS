using System;

using UnityEngine;

namespace CrawfisSoftware.UGS
{
    [Serializable]
    public struct GameBalance
    {
        [Tooltip("Global speed multiplier")]
        [Range(0.1f, 3.0f)]
        public float GlobalSpeedMultiplier;
        
        [Tooltip("Global score multiplier")]
        [Range(0.1f, 5.0f)]
        public float GlobalScoreMultiplier;
        
        [Tooltip("Coins per completed run")]
        public int CoinsPerRun;
        
        [Tooltip("Coins per video ad watched")]
        public int CoinsPerVideoAd;
        
        [Tooltip("Lives refill time in minutes")]
        public int LivesRefillTimeMinutes;
        
        [Tooltip("Maximum number of lives")]
        public int MaxLives;
    }
}