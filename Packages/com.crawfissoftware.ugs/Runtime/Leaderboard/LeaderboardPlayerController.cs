using CrawfisSoftware.UGS.Events;

using System;
using System.Threading.Tasks;

using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Leaderboard
{
    /// <summary>
    /// Submits the player's end-of-session score to a leaderboard.
    ///    Dependencies: Unity.Services.Leaderboards
    ///    Subscribes: UGS_EventsEnum.ScoreUpdating
    ///    Publishes: UGS_EventsEnum.ScoreUpdated, UGS_EventsEnum.ScoreFailedToUpdate
    /// </summary>
    public class LeaderboardPlayerController : MonoBehaviour
    {
        [SerializeField] private string LeaderboardId = "DailyDistance";

        public bool IsUpdating { get; private set; } = false;

        private void Start()
        {
            UGSBus.Subscribe(UGS_EventsEnum.ScoreUpdating, OnGameEnding);
        }
        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.ScoreUpdating, OnGameEnding);
        }
        private void OnGameEnding(string eventName, object sender, object data)
        {
            // The score arrives as a boxed payload from another domain, so its type is a convention
            // rather than a guarantee. Every numeric shape is accepted and anything else is refused
            // with a message naming what actually arrived - the previous cast assumed float and
            // threw from inside the handler on null or on any other numeric type, which took the
            // submission down without ever reporting a failure.
            if (!TryReadScore(data, out long score))
            {
                Debug.LogWarning(
                    $"{nameof(LeaderboardPlayerController)}: ignoring {UGS_EventsEnum.ScoreUpdating} - " +
                    $"expected a number, got {(data == null ? "null" : data.GetType().Name)}.");
                UGSBus.Publish(UGS_EventsEnum.ScoreFailedToUpdate, this, LeaderboardId);
                return;
            }

            IsUpdating = true;
            var _ = HandleScoreUpload(LeaderboardId, score);
        }

        private static bool TryReadScore(object data, out long score)
        {
            switch (data)
            {
                case float f: score = (long)f; return true;
                case double d: score = (long)d; return true;
                case int i: score = i; return true;
                case long l: score = l; return true;
                default: score = 0; return false;
            }
        }

        private async Task HandleScoreUpload(string leaderboardId, long score)
        {
            try
            {
                var playerEntry = await AddPlayerScore(leaderboardId, score);
                UnityEngine.Debug.Log($"Score {score} uploaded successfully! Player rank: {playerEntry.Rank}");
                UGSBus.Publish(UGS_EventsEnum.ScoreUpdated, this, (playerEntry.PlayerName, playerEntry.Score, playerEntry.PlayerId));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to upload score: {e}");
                UGSBus.Publish(UGS_EventsEnum.ScoreFailedToUpdate, this, leaderboardId);
            }
            finally
            {
                IsUpdating = false;
            }
        }

        public async Task<LeaderboardEntry> AddPlayerScore(string leaderboardId, long score)
        {
            try
            {
                var playerEntry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
                return playerEntry;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                throw; // Re-throw so calling code can handle it
            }
        }
    }
}