using System.Collections.Generic;

using CrawfisSoftware.UGS.Events;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Unlocks an achievement once the player passes each configured distance.
    /// Distance arrives as a UGS event, bridged in from the gameplay domain.
    ///    Dependencies: AchievementsService
    ///    Subscribes: UGS_EventsEnum.UGS_DistanceUpdated, UGS_EventsEnum.AchievementUnlocked
    ///    Publishes: none directly (AchievementsService publishes the claim/unlock events)
    /// </summary>
    public class DistanceBasedAchievements : MonoBehaviour
    {
        [Tooltip("Distances that trigger an unlock. Must be in ascending order.")]
        [SerializeField] private List<float> _distances;

        [Tooltip("Achievement id to unlock. Must exist in the deployed achievement definitions.")]
        [SerializeField] private string _achievementId = "first_achievement";

        private float _currentDistance;
        private int _nextAchievementIndex;

        private void Awake()
        {
            UGSBus.Subscribe(UGS_EventsEnum.UGS_DistanceUpdated, OnDistanceUpdated);
            UGSBus.Subscribe(UGS_EventsEnum.AchievementUnlocked, OnAchievementUnlocked);
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.UGS_DistanceUpdated, OnDistanceUpdated);
            UGSBus.Unsubscribe(UGS_EventsEnum.AchievementUnlocked, OnAchievementUnlocked);
        }

        private void OnDistanceUpdated(string eventName, object sender, object data)
        {
            if (data is float distance)
            {
                _currentDistance = distance;
                CheckAndUnlockAchievements();
            }
        }

        private void OnAchievementUnlocked(string eventName, object sender, object data)
        {
            if (data is Achievement achievement)
                Debug.Log($"Achievement Unlocked: {achievement.Id}");
        }

        private void CheckAndUnlockAchievements()
        {
            if (_distances == null) return;

            while (_nextAchievementIndex < _distances.Count &&
                   _currentDistance > _distances[_nextAchievementIndex])
            {
                Debug.Log($"Distance Achievement reached at {_distances[_nextAchievementIndex]}");

                // Unlock by id rather than by looking the achievement up first. The lookup used to
                // run against a catalogue that may not have been fetched yet, and dereferencing the
                // null it returned threw before the unlock was ever attempted.
                AchievementsService.Instance.UnlockAchievement(_achievementId);
                _nextAchievementIndex++;
            }
        }
    }
}
