using CrawfisSoftware.UGS.GameConfig;
using CrawfisSoftware.UGS.Events;

using System.Collections;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;


using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Leaderboard
{
    internal class LeaderboardController : MonoBehaviour
    {
        [SerializeField] private string LeaderboardId = "DailyDistance";
        [SerializeField] private string _tier;
        [SerializeField] private int _numberToDisplay;
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
            Debug.LogWarning("LeaderboardController: Score update failed.");
        }

        private void LoadLeaderboard(string eventName, object sender, object data)
        {
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