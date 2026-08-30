using System.Collections.Generic;

using CrawfisSoftware.UGS.Economy;
using CrawfisSoftware.UGS.Events;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Unlocks achievements as the player's lifetime currency balance passes each threshold.
    ///    Dependencies: AchievementsService, PlayerCurrencyManager (indirectly, via the event)
    ///    Subscribes: UGS_EventsEnum.CurrencyBalanceChanged (data: CurrencyBalanceUpdate),
    ///                UGS_EventsEnum.PlayerSignedOut, PlayerSessionExpired, UserAccountDeleted
    ///    Publishes: none directly (AchievementsService publishes the claim/unlock events)
    /// </summary>
    /// <remarks>
    /// <para><b>Lifetime, not per-run.</b> This used to read <c>UGS_CoinUpdated</c>, which carries
    /// the current run's running total and is reset to zero by the gameplay side at the end of every
    /// run. A "collect 500 coins" achievement written against that number could only ever mean 500
    /// coins in one run, which is not what such an achievement says.
    /// <see cref="PlayerCurrencyManager"/> owns the lifetime balance and announces it here.</para>
    /// <para><b>The first balance READ primes rather than unlocks.</b> A returning player's balance
    /// already sits above every threshold they have earned, and those were announced in the session
    /// that earned them; announcing again would fire a toast per achievement on every sign-in. So
    /// the first balance obtained by reading the store advances the ratchet silently, and only later
    /// increases unlock.</para>
    /// <para><b>Only a read may prime.</b> If the launch read fails and the first balance of the
    /// session is instead the result of banking that run's coins, priming from it would silently
    /// consume the very threshold the player just crossed - permanently, since the ratchet only
    /// moves forward. A credit arriving before any successful read is therefore announced normally.
    /// The cost is a possible duplicate toast for a player who launches offline; the alternative
    /// costs them the achievement with nothing said.</para>
    /// <para><b>The ratchet belongs to a player, not to a process.</b> Signing out clears
    /// credentials here, so the next sign-in is a different person with a different balance, and a
    /// ratchet carried across would either deny them every threshold below the previous player's
    /// total or congratulate them for it. Note the reset hangs off sign-out rather than
    /// <c>PlayerAuthenticated</c>, which is republished on ordinary navigation
    /// (<c>AchievementsClosed -&gt; PlayerAuthenticating</c>) and would reset the ratchet mid-session.</para>
    /// </remarks>
    public class CoinBasedAchievements : MonoBehaviour
    {
        /// <summary>UGS events that mean the ratchet describes a player who has left.</summary>
        private static readonly UGS_EventsEnum[] PlayerLostEvents =
        {
            UGS_EventsEnum.PlayerSignedOut,
            UGS_EventsEnum.PlayerSessionExpired,
            UGS_EventsEnum.UserAccountDeleted,
        };

        [Tooltip("Lifetime coin totals that trigger achievement unlocks. Must be in ascending order.")]
        [SerializeField] private List<int> _coinThresholds;

        [Tooltip("Achievement IDs corresponding to each threshold. Must match _coinThresholds length.")]
        [SerializeField] private List<string> _achievementIds;

        private readonly HashSet<string> _requestedIds = new HashSet<string>();
        private int _nextAchievementIndex;
        private bool _primed;

        private void Awake()
        {
            UGSBus.Subscribe(UGS_EventsEnum.CurrencyBalanceChanged, OnBalanceChanged);
            for (int i = 0; i < PlayerLostEvents.Length; i++)
            {
                UGSBus.Subscribe(PlayerLostEvents[i], OnPlayerLost);
            }
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.CurrencyBalanceChanged, OnBalanceChanged);
            for (int i = 0; i < PlayerLostEvents.Length; i++)
            {
                UGSBus.Unsubscribe(PlayerLostEvents[i], OnPlayerLost);
            }
        }

        private void OnPlayerLost(string eventName, object sender, object data)
        {
            _nextAchievementIndex = 0;
            _primed = false;
            _requestedIds.Clear();
        }

        private void OnBalanceChanged(string eventName, object sender, object data)
        {
            if (data is CurrencyBalanceUpdate update)
            {
                CheckAndUnlockAchievements(update.Balance, update.FromRead);
            }
        }

        private void CheckAndUnlockAchievements(long lifetimeBalance, bool fromRead)
        {
            if (_coinThresholds == null) return;

            // Priming is the one thing a read can do that a credit cannot, so the flag is set only
            // by a read. Everything else announces.
            bool priming = !_primed && fromRead;
            if (fromRead) _primed = true;

            while (_nextAchievementIndex < _coinThresholds.Count &&
                   lifetimeBalance >= _coinThresholds[_nextAchievementIndex])
            {
                if (!priming && _achievementIds != null && _nextAchievementIndex < _achievementIds.Count)
                {
                    string achievementId = _achievementIds[_nextAchievementIndex];

                    // A balance can be re-announced within a session - a refresh after a credit, say -
                    // and asking twice would claim twice.
                    if (!string.IsNullOrEmpty(achievementId) && _requestedIds.Add(achievementId))
                    {
                        Debug.Log($"Coin Achievement reached at {_coinThresholds[_nextAchievementIndex]} lifetime coins: {achievementId}");
                        AchievementsService.Instance.UnlockAchievement(achievementId);
                    }
                }
                _nextAchievementIndex++;
            }
        }
    }
}
