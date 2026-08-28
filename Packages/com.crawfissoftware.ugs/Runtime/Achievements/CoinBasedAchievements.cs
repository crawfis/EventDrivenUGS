using System.Collections.Generic;

using CrawfisSoftware.UGS.Events;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Unlocks achievements as the player's session coin count passes each threshold.
    /// Coin totals arrive as a UGS event, bridged in from the gameplay domain.
    ///    Dependencies: AchievementsService
    ///    Subscribes: UGS_EventsEnum.UGS_CoinUpdated (data: int sessionCoinCount)
    ///    Publishes: none directly (AchievementsService publishes the claim/unlock events)
    /// </summary>
    public class CoinBasedAchievements : MonoBehaviour
    {
        [Tooltip("Coin count thresholds that trigger achievement unlocks. Must be in ascending order.")]
        [SerializeField] private List<int> _coinThresholds;

        [Tooltip("Achievement IDs corresponding to each threshold. Must match _coinThresholds length.")]
        [SerializeField] private List<string> _achievementIds;

        private int _nextAchievementIndex;

        private void Awake()
        {
            UGSBus.Subscribe(UGS_EventsEnum.UGS_CoinUpdated, OnCoinUpdated);
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.UGS_CoinUpdated, OnCoinUpdated);
        }

        private void OnCoinUpdated(string eventName, object sender, object data)
        {
            if (data is int sessionCoinCount)
            {
                CheckAndUnlockAchievements(sessionCoinCount);
            }
        }

        private void CheckAndUnlockAchievements(int sessionCoinCount)
        {
            if (_coinThresholds == null) return;

            while (_nextAchievementIndex < _coinThresholds.Count &&
                   sessionCoinCount >= _coinThresholds[_nextAchievementIndex])
            {
                if (_achievementIds != null && _nextAchievementIndex < _achievementIds.Count)
                {
                    string achievementId = _achievementIds[_nextAchievementIndex];
                    Debug.Log($"Coin Achievement reached at {_coinThresholds[_nextAchievementIndex]} coins: {achievementId}");
                    AchievementsService.Instance.UnlockAchievement(achievementId);
                }
                _nextAchievementIndex++;
            }
        }
    }
}
