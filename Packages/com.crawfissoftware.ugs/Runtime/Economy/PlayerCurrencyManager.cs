using System;
using System.Threading.Tasks;

using UnityEngine;

using CrawfisSoftware.UGS.Events;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Economy
{
    /// <summary>
    /// The one place a player's lifetime soft-currency balance is held and moved. Picks a backend,
    /// caches the last authoritative balance, and reports every outcome on the UGS event bus.
    ///    Dependencies: ICurrencyBackend, Unity.Services.Authentication
    ///    Subscribes: UGS_EventsEnum.PlayerAuthenticated, PlayerSignedOut, PlayerSessionExpired,
    ///                UserAccountDeleted
    ///    Publishes: CurrencyBalanceChanged (CurrencyBalanceUpdate), CurrencySyncFailed (string currencyId)
    /// </summary>
    /// <remarks>
    /// <para><b>Lazily created, never a scene object</b>, for the same reason as
    /// <see cref="CrawfisSoftware.UGS.Achievements.AchievementsService"/>: the components that read
    /// a balance and the component that credits one live in different additively-loaded scenes and
    /// neither can be relied on to exist first.</para>
    /// <para><b>The cached balance only ever comes from the backend.</b> Nothing here adds a credit
    /// to <see cref="Balance"/> locally. A second device crediting the same account would make local
    /// arithmetic wrong, and it would stay wrong for the rest of the session.</para>
    /// <para><b>A balance belongs to a player, not to a process.</b> Signing out invalidates it.
    /// This package treats a mid-session player change as a normal flow - signing out clears
    /// credentials, so the next anonymous sign-in is a different person - and a balance carried
    /// across that boundary would be someone else's.</para>
    /// </remarks>
    public sealed class PlayerCurrencyManager
    {
        /// <summary>
        /// The currency id used unless a project sets <see cref="CurrencyId"/>.
        /// <b>Wire contract</b> - this must match a currency defined in the project's Economy
        /// configuration in the Unity Dashboard. Nothing checks it at compile time, and a mismatch
        /// surfaces only as a failed call at runtime.
        /// </summary>
        public const string DefaultCurrencyId = "COIN";

        /// <summary>UGS events that mean the current player is no longer the current player.</summary>
        private static readonly UGS_EventsEnum[] PlayerLostEvents =
        {
            UGS_EventsEnum.PlayerSignedOut,
            UGS_EventsEnum.PlayerSessionExpired,
            UGS_EventsEnum.UserAccountDeleted,
        };

        private static PlayerCurrencyManager _instance;

        /// <summary>The shared manager, created on first use.</summary>
        public static PlayerCurrencyManager Instance => _instance ??= new PlayerCurrencyManager();

        private ICurrencyBackend _backend;
        private bool _backendAssigned;
        private bool _useTrustedClient;
        private string _currencyId = DefaultCurrencyId;
        private Task _refreshTask;

        // Reads can answer out of order, and a read issued before a credit can answer after it with
        // pre-credit state. Ordering by issue number keeps the newer read; a credit outranks both,
        // because its result is what the server computed for a write that has already committed.
        private int _operationSequence;
        private int _appliedSequence;

        private PlayerCurrencyManager()
        {
            // A balance cannot be read before there is a player, and scene components are built at
            // Awake, long before sign-in completes. Without this the balance stays unknown for the
            // whole session and nothing ever says why.
            UGSBus.Subscribe(UGS_EventsEnum.PlayerAuthenticated, OnPlayerAuthenticated);
            for (int i = 0; i < PlayerLostEvents.Length; i++)
            {
                UGSBus.Subscribe(PlayerLostEvents[i], OnPlayerLost);
            }
        }

        private void OnPlayerAuthenticated(string eventName, object sender, object data) => Refresh();

        private void OnPlayerLost(string eventName, object sender, object data) => InvalidateBalance();

        /// <summary>The last balance the backing service reported. Zero until one has been read.</summary>
        public long Balance { get; private set; }

        /// <summary>
        /// False until a balance has actually been read. Distinguishes "this player has no coins"
        /// from "we have not asked yet", which otherwise look identical.
        /// </summary>
        public bool HasBalance { get; private set; }

        /// <summary>Raised whenever an authoritative balance arrives, for code that wants a callback.</summary>
        public event Action<CurrencyBalanceUpdate> BalanceChanged;

        /// <summary>
        /// Which currency this manager reads and moves. Changing it drops the cached balance, since
        /// the cached number describes the previous currency.
        /// </summary>
        public string CurrencyId
        {
            get => _currencyId;
            set
            {
                if (_currencyId == value) return;
                _currencyId = value;
                InvalidateBalance();
            }
        }

        /// <summary>
        /// Which backend to use. Setting it discards the current backend so the next call builds a
        /// fresh one; it does not re-read the balance on its own.
        /// </summary>
        public bool UseTrustedClient
        {
            get => _useTrustedClient;
            set
            {
                _useTrustedClient = value;

                // A backend handed in by the consumer outranks this flag, so a scene component
                // applying its own checkbox cannot throw away a backend assigned deliberately.
                if (!_backendAssigned) _backend = null;
            }
        }

        /// <summary>
        /// The backend in use, built on first access from <see cref="UseTrustedClient"/>. Assignable
        /// so a test, or a game with its own currency store, can substitute one.
        /// </summary>
        public ICurrencyBackend Backend
        {
            get => _backend ??= _useTrustedClient
                ? new CloudCodeCurrencyBackend(CloudCodeEndpoints)
                : (ICurrencyBackend)new EconomyCurrencyBackend();
            set
            {
                _backend = value;
                _backendAssigned = value != null;
            }
        }

        /// <summary>
        /// Which Cloud Code module and functions the trusted backend calls. Set this before anything
        /// touches <see cref="Backend"/> with <see cref="UseTrustedClient"/> on.
        /// </summary>
        public CloudCodeCurrencyEndpoints CloudCodeEndpoints { get; set; } = CloudCodeCurrencyEndpoints.Default;

        /// <summary>Drop the cached balance, the backend and the bus subscriptions. Intended for tests.</summary>
        public static void Reset()
        {
            // Release the subscriptions before dropping the instance, or the orphan keeps refreshing
            // for the rest of the process and every later sign-in fans out to one more dead manager.
            if (_instance != null)
            {
                UGSBus.Unsubscribe(UGS_EventsEnum.PlayerAuthenticated, _instance.OnPlayerAuthenticated);
                for (int i = 0; i < PlayerLostEvents.Length; i++)
                {
                    UGSBus.Unsubscribe(PlayerLostEvents[i], _instance.OnPlayerLost);
                }
            }
            _instance = null;
        }

        // A static instance outlives play mode when Enter Play Mode Options disable the domain
        // reload, carrying the previous session's balance and subscriber list into the next run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Reset();

        /// <summary>
        /// Re-read the balance from the backing service. Safe to call repeatedly and before sign-in;
        /// overlapping calls collapse into the one already running.
        /// </summary>
        public void Refresh() => _ = RefreshAsync();

        /// <summary>The refresh as an awaitable. Overlapping calls collapse into the one running.</summary>
        public Task RefreshAsync()
        {
            if (_refreshTask != null && !_refreshTask.IsCompleted) return _refreshTask;
            return _refreshTask = RefreshCoreAsync();
        }

        private async Task RefreshCoreAsync()
        {
            // Called from scene Awake as well as from sign-in, so being early is normal rather than
            // exceptional. Announcing a failure here would put an error on screen at every boot.
            if (!IsSignedIn()) return;

            int operation = ++_operationSequence;
            try
            {
                long balance = await Backend.GetBalanceAsync(_currencyId);

                // A completion older than one already applied is stale by definition.
                if (operation < _appliedSequence) return;
                _appliedSequence = operation;
                ApplyBalance(balance, fromRead: true);
            }
            catch (Exception e)
            {
                Report("read the balance", e);
            }
        }

        /// <summary>
        /// Move the balance by <paramref name="amount"/> - negative to spend - reporting the outcome
        /// on the event bus. Fire-and-forget; callers that need to know whether it landed should
        /// await <see cref="AddAsync"/> instead.
        /// </summary>
        public void Add(int amount) => _ = AddAsync(amount);

        /// <summary>
        /// Move the balance by <paramref name="amount"/> and report whether it landed.
        /// </summary>
        /// <remarks>
        /// The result matters to the caller: coins that failed to bank have to stay pending
        /// somewhere, or they are gone with nothing but a log line to say so.
        /// </remarks>
        /// <returns>True when the backing service confirmed the change.</returns>
        public async Task<bool> AddAsync(int amount)
        {
            if (amount == 0) return true;

            if (!IsSignedIn())
            {
                // Unlike a refresh, this one is worth announcing: coins the player earned cannot be
                // banked, and silence here is the failure mode this class exists to remove.
                Report("credit the balance", new CurrencyBackendException(
                    "No player is signed in, so there is no balance to credit."));
                return false;
            }

            try
            {
                long balance = await Backend.AddAsync(_currencyId, amount);

                // A write result outranks every read issued up to now: it is the server's own
                // post-write value, while an outstanding read may still answer with the state
                // before this credit. Claiming the next sequence number is what says so.
                _appliedSequence = ++_operationSequence;
                ApplyBalance(balance, fromRead: false);
                return true;
            }
            catch (Exception e)
            {
                Report(amount > 0 ? "credit the balance" : "debit the balance", e);
                return false;
            }
        }

        private void ApplyBalance(long balance, bool fromRead)
        {
            Balance = balance;
            HasBalance = true;

            var update = new CurrencyBalanceUpdate(_currencyId, balance, fromRead);
            UGSBus.Publish(UGS_EventsEnum.CurrencyBalanceChanged, this, update);
            BalanceChanged?.Invoke(update);
        }

        /// <summary>
        /// Forget the cached balance, and make sure anything already in flight cannot land on top of
        /// whatever comes next. Used when the balance stops describing the current player.
        /// </summary>
        private void InvalidateBalance()
        {
            // Claiming a sequence number here is what discards results still in flight for the
            // player who just left; without it, a slow read could repopulate their balance under
            // the next player.
            _appliedSequence = ++_operationSequence;
            Balance = 0L;
            HasBalance = false;
        }

        private static bool IsSignedIn()
        {
            try
            {
                return Unity.Services.Authentication.AuthenticationService.Instance?.IsSignedIn ?? false;
            }
            catch (Exception)
            {
                // Services not initialised yet, which is a "no" rather than an error.
                return false;
            }
        }

        private void Report(string what, Exception e)
        {
            string detail = e is CurrencyBackendException ? e.Message : $"{e.GetType().Name}: {e.Message}";

            // The two configuration causes get named, because both otherwise present to a developer
            // as "the number never changes" and neither is fixed by editing code.
            var backendException = e as CurrencyBackendException;
            if (backendException != null && backendException.IsCurrencyNotFound)
                detail += " (the currency id does not exist in this project's Economy configuration)";
            else if (backendException != null && backendException.IsAccessDenied)
                detail += " (this project's access policy denies player writes to Economy)";

            Debug.LogWarning($"{nameof(PlayerCurrencyManager)}: could not {what} for '{_currencyId}'. {detail}");
            UGSBus.Publish(UGS_EventsEnum.CurrencySyncFailed, this, _currencyId);
        }
    }
}
