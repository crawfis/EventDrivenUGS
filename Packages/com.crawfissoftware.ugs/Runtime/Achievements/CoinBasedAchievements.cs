using Blocks.Achievements;

using CrawfisSoftware.UGS.Events;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Unlocks achievements based on coins collected in a session.
    /// Receives coin updates via UGS events (bridged from TempleRun domain).
    ///    Dependencies: EventsPublisherUGS, AchievementsObserver
    ///    Subscribes: UGS_EventsEnum.CoinUpdated (data: int sessionCoinCount)
    /// </summary>
    public class CoinBasedAchievements : MonoBehaviour
    {
        [Tooltip("Coin count thresholds that trigger achievement unlocks. Must be in ascending order.")]
        [SerializeField] private List<int> _coinThresholds;

        [Tooltip("Achievement IDs corresponding to each threshold. Must match _coinThresholds length.")]
        [SerializeField] private List<string> _achievementIds;

        private int _nextAchievementIndex = 0;

        private void Awake()
        {
            UGSBus.Subscribe(
                UGS_EventsEnum.UGS_CoinUpdated,
                OnCoinUpdated);
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(
                UGS_EventsEnum.UGS_CoinUpdated,
                OnCoinUpdated);
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
            while (_nextAchievementIndex < _coinThresholds.Count &&
                   sessionCoinCount >= _coinThresholds[_nextAchievementIndex])
            {
                if (_nextAchievementIndex < _achievementIds.Count)
                {
                    string achievementId = _achievementIds[_nextAchievementIndex];
                    Debug.Log($"Coin Achievement reached at {_coinThresholds[_nextAchievementIndex]} coins: {achievementId}");
                    StartCoroutine(UnlockAchievementAsync(achievementId));
                }
                _nextAchievementIndex++;
            }
        }

        private IEnumerator UnlockAchievementAsync(string achievementId)
        {
            if (AchievementsObserver.Instance != null)
            {
                yield return AchievementsObserver.Instance.UnlockAchievementAsync(achievementId);
            }
        }
    }
}
