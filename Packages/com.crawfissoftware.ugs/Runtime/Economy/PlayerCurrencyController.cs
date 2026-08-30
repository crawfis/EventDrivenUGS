using UnityEngine;

using CrawfisSoftware.UGS.Events;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// Configures <see cref="PlayerCurrencyManager"/> from the scene and banks each run's coins into
    /// the lifetime balance when the run ends.
    ///    Dependencies: PlayerCurrencyManager
    ///    Subscribes: UGS_EventsEnum.UGS_CoinUpdated (data: int session total),
    ///                UGS_EventsEnum.CurrencySyncRequested,
    ///                UGS_EventsEnum.PlayerSignedOut, PlayerSessionExpired, UserAccountDeleted
    ///    Publishes: none directly (PlayerCurrencyManager publishes the balance and failure events)
    /// </summary>
    /// <remarks>
    /// <para><b>Why banking rather than crediting each pickup.</b> A coin pickup is a per-frame
    /// event and a balance write is a network round trip; crediting per pickup would issue hundreds
    /// of calls a run. The run is the natural unit, and it is also when the score is submitted.</para>
    /// <para><b>UGS_CoinUpdated carries a running total, not a delta.</b> The gameplay side publishes
    /// its whole session count on every pickup, so this assigns rather than accumulates. Adding it
    /// would over-count within a run by roughly the square of the coins collected.</para>
    /// <para><b>Coins survive a failed credit.</b> Pending coins are cleared only once the service
    /// confirms them, so a credit that fails - offline, or a misconfigured trusted backend - leaves
    /// them carried for the next run to bank alongside its own. Clearing them when the credit is
    /// issued, which is the obvious way to write this, discards a run's coins on any transient
    /// failure and cannot retry even in principle.</para>
    /// <para>Coins are still lost if the application dies mid-run, which is the deliberate cost of
    /// banking per run rather than per pickup.</para>
    /// </remarks>
    public class PlayerCurrencyController : MonoBehaviour
    {
        /// <summary>UGS events that mean the pending coins belong to a player who has left.</summary>
        private static readonly UGS_EventsEnum[] PlayerLostEvents =
        {
            UGS_EventsEnum.PlayerSignedOut,
            UGS_EventsEnum.PlayerSessionExpired,
            UGS_EventsEnum.UserAccountDeleted,
        };

        [Tooltip("Economy currency id to credit. Must match a currency defined in the Unity Dashboard.")]
        [SerializeField] private string _currencyId = PlayerCurrencyManager.DefaultCurrencyId;

        [Tooltip("Route balance changes through a Cloud Code module instead of writing them from " +
                 "the client. Requires the module name below, and that module to be deployed.")]
        [SerializeField] private bool _useTrustedClient;

        [Tooltip("Cloud Code module holding the currency endpoints. Only read when Use Trusted " +
                 "Client is on; there is no default because this package deploys no module.")]
        [SerializeField] private string _cloudCodeModuleName;

        // Two totals, because they accumulate differently. The session total is ASSIGNED from each
        // UGS_CoinUpdated, since that event carries the run's running total. Coins that failed to
        // bank cannot live in that field - the next run's first pickup would assign straight over
        // them - so they are carried separately and added back in at the next sync.
        private int _sessionCoinTotal;
        private int _unbankedCoins;
        private bool _bankingInFlight;

        private void Awake()
        {
            var manager = PlayerCurrencyManager.Instance;

            if (!string.IsNullOrEmpty(_currencyId)) manager.CurrencyId = _currencyId;

            if (!string.IsNullOrEmpty(_cloudCodeModuleName))
            {
                var endpoints = manager.CloudCodeEndpoints;
                endpoints.ModuleName = _cloudCodeModuleName;
                manager.CloudCodeEndpoints = endpoints;
            }

            // Set last: assigning it is what discards any backend already built, so a backend built
            // from a stale flag cannot survive into the first call.
            manager.UseTrustedClient = _useTrustedClient;

            UGSBus.Subscribe(UGS_EventsEnum.UGS_CoinUpdated, OnSessionCoinsChanged);
            UGSBus.Subscribe(UGS_EventsEnum.CurrencySyncRequested, OnSyncRequested);
            for (int i = 0; i < PlayerLostEvents.Length; i++)
            {
                UGSBus.Subscribe(PlayerLostEvents[i], OnPlayerLost);
            }

            // This scene may load after sign-in has already happened, in which case the manager
            // fired its own refresh against defaults that had not been configured yet. Harmless
            // before sign-in: a refresh with no player returns without a call.
            manager.Refresh();
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.UGS_CoinUpdated, OnSessionCoinsChanged);
            UGSBus.Unsubscribe(UGS_EventsEnum.CurrencySyncRequested, OnSyncRequested);
            for (int i = 0; i < PlayerLostEvents.Length; i++)
            {
                UGSBus.Unsubscribe(PlayerLostEvents[i], OnPlayerLost);
            }
        }

        private void OnSessionCoinsChanged(string eventName, object sender, object data)
        {
            if (data is int sessionCoinTotal) _sessionCoinTotal = sessionCoinTotal;
        }

        private void OnPlayerLost(string eventName, object sender, object data)
        {
            // Whatever is pending was earned by the player who just left. Banking it into the next
            // player's balance would be worse than losing it.
            _sessionCoinTotal = 0;
            _unbankedCoins = 0;
        }

        private async void OnSyncRequested(string eventName, object sender, object data)
        {
            // An in-flight credit is what guards against double banking. The pending total cannot
            // do that job, because it has to survive a failure.
            if (_bankingInFlight) return;

            int pending = _sessionCoinTotal + _unbankedCoins;
            if (pending <= 0) return;

            // The run is over, so its total has been consumed into `pending` and must not be counted
            // again by a later sync that collects no coins of its own.
            _sessionCoinTotal = 0;
            _unbankedCoins = pending;

            _bankingInFlight = true;
            try
            {
                bool banked = await PlayerCurrencyManager.Instance.AddAsync(pending);

                // Cleared only on confirmation. On failure the coins stay carried and the next
                // sync banks them together with that run's own. Coins collected while the call was
                // in flight belong to the next run and sit in the session total, untouched here.
                if (banked) _unbankedCoins = 0;
            }
            finally
            {
                _bankingInFlight = false;
            }
        }
    }
}
