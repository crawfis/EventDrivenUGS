using CrawfisSoftware.UGS.GameConfig;
using CrawfisSoftware.UGS.Events;

using System.Collections;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;


using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Leaderboard
{
    /// <summary>
    /// Drives the leaderboard scene: loads it additively when the leaderboard opens, then closes it
    /// once an in-flight score submission has settled and the display time has elapsed.
    ///    Dependencies: UnityEngine.SceneManagement, UGSConstants
    ///    Subscribes: UGS_EventsEnum.LeaderboardOpening, UGS_EventsEnum.ScoreUpdating,
    ///                UGS_EventsEnum.ScoreUpdated, UGS_EventsEnum.ScoreFailedToUpdate
    ///    Publishes: UGS_EventsEnum.LeaderboardOpened, UGS_EventsEnum.LeaderboardClosing
    /// </summary>
    /// <remarks>The tier and the row count are not configured here. LeaderboardPanel performs the
    /// query and carries its own _tierId and _numberToDisplay; the fields that once sat on this
    /// controller were read by nothing, so a value set on them silently did not apply.</remarks>
    internal class LeaderboardController : MonoBehaviour
    {
        [SerializeField] private string LeaderboardId = "DailyDistance";
        [SerializeField] private string _sceneToLoad;

        private bool _isUpdating = false;

        // TCS to signal when a score update finishes (success or failure)
        private TaskCompletionSource<bool> _scoreUpdatedTcs;

        private void Start()
        {
            //TempleRunBus.Subscribe(GamePlayEvents.GameScenesUnloaded, OnGameOver);
            UGSBus.Subscribe(UGS_EventsEnum.LeaderboardOpening, LoadLeaderboard);
            UGSBus.Subscribe(UGS_EventsEnum.ScoreUpdating, OnScoreUpdating);
            UGSBus.Subscribe(UGS_EventsEnum.ScoreUpdated, OnScoreUpdated);
            UGSBus.Subscribe(UGS_EventsEnum.ScoreFailedToUpdate, OnScoreUpdateFailed);
        }

        private void OnDestroy()
        {
            //TempleRunBus.Unsubscribe(GamePlayEvents.GameScenesUnloaded, OnGameOver);
            UGSBus.Unsubscribe(UGS_EventsEnum.LeaderboardOpening, LoadLeaderboard);
            UGSBus.Unsubscribe(UGS_EventsEnum.ScoreUpdating, OnScoreUpdating);
            UGSBus.Unsubscribe(UGS_EventsEnum.ScoreUpdated, OnScoreUpdated);
            UGSBus.Unsubscribe(UGS_EventsEnum.ScoreFailedToUpdate, OnScoreUpdateFailed);
            // sceneLoaded is static and is otherwise removed only by a matching load, so a component
            // destroyed while a load is pending would leave it holding a destroyed MonoBehaviour.
            SceneManager.sceneLoaded -= OnLeaderboardSceneLoaded;
        }

        private void OnScoreUpdating(string eventName, object sender, object data)
        {
            _isUpdating = true;
            // Create a fresh TCS for the new update cycle
            _scoreUpdatedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private void OnScoreUpdated(string eventName, object sender, object data)
        {
            _isUpdating = false;
            // Signal anyone awaiting the update completion
            _scoreUpdatedTcs?.TrySetResult(true);
        }

        private void OnScoreUpdateFailed(string eventName, object sender, object data)
        {
            // A failure has to release the close coroutine exactly as a success does. Leaving
            // _isUpdating set parks CloseLeaderboardAfterDelay on its WaitUntil forever, so
            // LeaderboardClosing is never published and the leaderboard scene never unloads.
            _isUpdating = false;
            // Signal anyone awaiting the update completion
            _scoreUpdatedTcs?.TrySetResult(false);
            Debug.LogWarning("LeaderboardController: Score update failed.");
        }

        private void LoadLeaderboard(string eventName, object sender, object data)
        {
            // Remove before adding: a second LeaderboardOpening arriving before the load completes
            // would otherwise register the handler twice and publish LeaderboardOpened twice.
            SceneManager.sceneLoaded -= OnLeaderboardSceneLoaded;
            SceneManager.sceneLoaded += OnLeaderboardSceneLoaded;
            SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Additive);
        }

        private void OnLeaderboardSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name != _sceneToLoad) return;

            SceneManager.sceneLoaded -= OnLeaderboardSceneLoaded;
            UGSBus.Publish(UGS_EventsEnum.LeaderboardOpened, this, LeaderboardId);
            StartCoroutine(CloseLeaderboardAfterDelay());
        }

        private IEnumerator CloseLeaderboardAfterDelay()
        {
            yield return new WaitUntil(() => !_isUpdating);
            yield return new WaitForSeconds(UGSConstants.LeaderboardDisplayTime);
            UGSBus.Publish(UGS_EventsEnum.LeaderboardClosing, "Leaderboard Controller", LeaderboardId);
        }
    }
}