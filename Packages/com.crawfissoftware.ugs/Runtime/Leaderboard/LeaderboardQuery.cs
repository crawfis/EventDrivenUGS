using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;

using UnityEngine;

namespace CrawfisSoftware.UGS.Leaderboard
{
    /// <summary>
    /// Reads leaderboard entries. A stateless facade over the Leaderboards SDK.
    ///    Dependencies: Unity.Services.Leaderboards, Unity.Services.Authentication, Unity.Services.Core
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>No singleton, no cache, no events. A leaderboard read is a request with an answer, and
    /// the stack this replaces wrapped that in an observer, a client interface, two client
    /// implementations and a bindable data model - none of which the display actually needed.</para>
    /// <para><b>A service failure yields an empty list, not an exception.</b> A leaderboard is
    /// decoration: no game should fail to show a menu because a score list did not load. The
    /// failure is still logged, and at a severity that distinguishes a configuration mistake you
    /// must fix from a player who simply has no score yet.</para>
    /// </remarks>
    public static class LeaderboardQuery
    {
        private static readonly IReadOnlyList<LeaderboardEntry> _empty = Array.Empty<LeaderboardEntry>();

        /// <summary>True when the services are initialised and a player is signed in.</summary>
        public static bool IsAvailable
        {
            get
            {
                try
                {
                    return UnityServices.State == ServicesInitializationState.Initialized
                           && AuthenticationService.Instance != null
                           && AuthenticationService.Instance.IsSignedIn;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>The signed-in player's id, or null. Used to highlight their own row.</summary>
        public static string CurrentPlayerId => IsAvailable ? AuthenticationService.Instance.PlayerId : null;

        /// <summary>The global top <paramref name="limit"/> entries.</summary>
        public static Task<IReadOnlyList<LeaderboardEntry>> GetTopScoresAsync(
            string leaderboardId, int limit, CancellationToken cancellationToken = default) =>
            RunAsync(leaderboardId, cancellationToken, async () =>
            {
                var page = await LeaderboardsService.Instance.GetScoresAsync(
                    leaderboardId, new GetScoresOptions { Limit = limit });
                return page?.Results;
            });

        /// <summary>The top <paramref name="limit"/> entries within one tier.</summary>
        public static Task<IReadOnlyList<LeaderboardEntry>> GetTierScoresAsync(
            string leaderboardId, string tierId, int limit, CancellationToken cancellationToken = default) =>
            RunAsync(leaderboardId, cancellationToken, async () =>
            {
                var page = await LeaderboardsService.Instance.GetScoresByTierAsync(
                    leaderboardId, tierId, new GetScoresByTierOptions { Limit = limit });
                return page?.Results;
            });

        /// <summary>The entries immediately around the signed-in player.</summary>
        public static Task<IReadOnlyList<LeaderboardEntry>> GetPlayerRangeAsync(
            string leaderboardId, int rangeLimit, CancellationToken cancellationToken = default) =>
            RunAsync(leaderboardId, cancellationToken, async () =>
            {
                var page = await LeaderboardsService.Instance.GetPlayerRangeAsync(
                    leaderboardId, new GetPlayerRangeOptions { RangeLimit = rangeLimit });
                return page?.Results;
            });

        private static async Task<IReadOnlyList<LeaderboardEntry>> RunAsync(
            string leaderboardId, CancellationToken cancellationToken,
            Func<Task<List<LeaderboardEntry>>> request)
        {
            if (!IsAvailable)
            {
                Debug.Log($"{nameof(LeaderboardQuery)}: services not ready; '{leaderboardId}' not read.");
                return _empty;
            }

            try
            {
                var results = await request();

                // The SDK takes no cancellation token, so the request always completes. Checking
                // here discards the RESULT of a request the caller no longer wants - it does not
                // abort the call, and must not be described as if it did.
                cancellationToken.ThrowIfCancellationRequested();

                return (IReadOnlyList<LeaderboardEntry>)results ?? _empty;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (LeaderboardsException e)
            {
                LogByReason(leaderboardId, e);
                return _empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(LeaderboardQuery)}: unexpected failure reading '{leaderboardId}'. {e}");
                return _empty;
            }
        }

        private static void LogByReason(string leaderboardId, LeaderboardsException e)
        {
            switch (e.Reason)
            {
                // Configuration mistakes. These must be loud: nothing the player does will fix them.
                case LeaderboardsExceptionReason.LeaderboardNotFound:
                case LeaderboardsExceptionReason.TierNotFound:
                case LeaderboardsExceptionReason.InvalidArgument:
                case LeaderboardsExceptionReason.Unauthorized:
                    Debug.LogError($"{nameof(LeaderboardQuery)}: '{leaderboardId}' - {e.Reason}. {e.Message}");
                    break;

                // Ordinary on a first run: the player has not scored yet. ScoreSubmissionRequired is
                // the bucketed-board form of the same condition - the service assigns no bucket until
                // the player has submitted once, so a read before that is not a misconfiguration.
                case LeaderboardsExceptionReason.EntryNotFound:
                case LeaderboardsExceptionReason.ScoreSubmissionRequired:
                    Debug.Log($"{nameof(LeaderboardQuery)}: no entry yet on '{leaderboardId}'.");
                    break;

                // Transient. Worth noting, not worth alarming about.
                case LeaderboardsExceptionReason.NoInternetConnection:
                case LeaderboardsExceptionReason.ServiceUnavailable:
                case LeaderboardsExceptionReason.TooManyRequests:
                    Debug.LogWarning($"{nameof(LeaderboardQuery)}: '{leaderboardId}' unavailable - {e.Reason}.");
                    break;

                default:
                    Debug.LogError($"{nameof(LeaderboardQuery)}: '{leaderboardId}' - {e.Reason}. {e.Message}");
                    break;
            }
        }
    }
}
