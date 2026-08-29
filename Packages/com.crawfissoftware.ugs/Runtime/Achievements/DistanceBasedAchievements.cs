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
    /// <remarks>
    /// Each distance gets its own achievement id from <c>_achievementIds</c>, the same pairing
    /// CoinBasedAchievements uses. Thresholds with no matching entry fall back to the single
    /// <c>_achievementId</c>, which is what scene instances authored before the list existed
    /// still carry. Whatever the source, an id is sent to the backend at most once per component
    /// lifetime: an unlock is a read-modify-write of the whole achievement record set, so
    /// repeating one id across ten thresholds cost ten round trips whose writes overwrote each
    /// other. The trade is that a failed unlock is not retried on the next threshold, which
    /// matches the pre-existing behaviour - this component never retried, it advanced past the
    /// threshold regardless of the outcome.
    /// </remarks>
    public class DistanceBasedAchievements : MonoBehaviour
    {
        [Tooltip("Distances that trigger an unlock. Must be in ascending order.")]
        [SerializeField] private List<float> _distances;

        [Tooltip("Achievement ids, one per entry in Distances. Must exist in the deployed achievement definitions.")]
        [SerializeField] private List<string> _achievementIds;

        [Tooltip("Achievement id used for distances with no entry in Achievement Ids.")]
        [SerializeField] private string _achievementId = "first_achievement";

        private readonly HashSet<string> _requestedIds = new HashSet<string>();

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
                float threshold = _distances[_nextAchievementIndex];
                string achievementId = ResolveAchievementId(_nextAchievementIndex);
                _nextAchievementIndex++;

                // _requestedIds.Add reports false for an id an earlier threshold already sent.
                if (string.IsNullOrEmpty(achievementId) || !_requestedIds.Add(achievementId))
                    continue;

                Debug.Log($"Distance Achievement reached at {threshold}: {achievementId}");

                // Unlock by id rather than by looking the achievement up first. The lookup used to
                // run against a catalogue that may not have been fetched yet, and dereferencing the
                // null it returned threw before the unlock was ever attempted.
                AchievementsService.Instance.UnlockAchievement(achievementId);
            }
        }

        private string ResolveAchievementId(int index)
        {
            if (_achievementIds != null && index < _achievementIds.Count &&
                !string.IsNullOrEmpty(_achievementIds[index]))
            {
                return _achievementIds[index];
            }

            return _achievementId;
        }
    }
}
