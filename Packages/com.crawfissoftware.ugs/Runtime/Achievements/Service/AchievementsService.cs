using System;
using System.Threading.Tasks;

using UnityEngine;

using CrawfisSoftware.UGS.Events;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// The one place achievement state is held and mutated. Owns the catalogue, picks a backend,
    /// and reports every outcome on the UGS event bus.
    ///    Dependencies: IAchievementBackend, Unity.Services.Authentication
    ///    Subscribes: UGS_EventsEnum.PlayerAuthenticated
    ///    Publishes: AchievementClaiming, AchievementClaimed, AchievementClaimFailed,
    ///               AchievementUnlocked, AchievementProgressUpdated
    /// </summary>
    /// <remarks>
    /// <para><b>Lazily created, never a scene object.</b> The achievements panel and the unlock
    /// toast live in different additively-loaded scenes and neither can be relied on to exist
    /// first, so ownership cannot sit with either. Touching <see cref="Instance"/> creates it.</para>
    /// <para>Public mutators are deliberately <c>void</c> and fire-and-forget. Callers are gameplay
    /// components on the main thread that have nothing useful to do with a Task; results arrive as
    /// events, which is also what lets a UI in another scene react.</para>
    /// </remarks>
    public sealed class AchievementsService
    {
        private static AchievementsService _instance;

        /// <summary>The shared service, created on first use.</summary>
        public static AchievementsService Instance => _instance ??= new AchievementsService();

        private IAchievementBackend _backend;
        private bool _backendAssigned;
        private bool _useTrustedClient;
        private Task _loadTask;

        private AchievementsService()
        {
            // The catalogue cannot be fetched before there is a player, and the views are built at
            // scene Awake - long before sign-in completes. Without this the catalogue stays empty,
            // every Catalog.Find returns null, and an unlock persists to the backend while
            // announcing nothing at all.
            UGSBus.Subscribe(UGS_EventsEnum.PlayerAuthenticated, OnPlayerAuthenticated);
        }

        private void OnPlayerAuthenticated(string eventName, object sender, object data) => LoadAsync();

        /// <summary>Everything currently known. Rebuilt by <see cref="LoadAsync"/>.</summary>
        public AchievementCatalog Catalog { get; } = new AchievementCatalog();

        /// <summary>
        /// Raised when an achievement transitions to unlocked, for view code that wants the object
        /// rather than an event-bus payload.
        /// </summary>
        public event Action<Achievement> AchievementUnlocked;

        /// <summary>
        /// Which backend to use. Setting it discards the current backend, so the next call builds a
        /// fresh one; it does not reload the catalogue on its own.
        /// </summary>
        public bool UseTrustedClient
        {
            get => _useTrustedClient;
            set
            {
                _useTrustedClient = value;

                // A backend handed in by the consumer outranks this flag. The achievements panel
                // sets UseTrustedClient from its own checkbox as it is constructed, which would
                // otherwise throw away a backend assigned deliberately moments earlier.
                if (!_backendAssigned) _backend = null;
            }
        }

        /// <summary>
        /// The backend in use, built on first access from <see cref="UseTrustedClient"/>. Assignable
        /// so a test, or a game with its own storage, can substitute one.
        /// </summary>
        public IAchievementBackend Backend
        {
            get => _backend ??= _useTrustedClient
                ? new CloudCodeAchievementBackend(CloudCodeEndpoints)
                : (IAchievementBackend)new CloudSaveAchievementBackend();
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
        /// <remarks>
        /// This package ships no Cloud Code module, so the default deliberately carries no module
        /// name and the trusted backend refuses to construct without one. A default that named some
        /// module would instead fail per call, at runtime, against a module that does not exist.
        /// </remarks>
        public CloudCodeAchievementEndpoints CloudCodeEndpoints { get; set; } = CloudCodeAchievementEndpoints.Default;

        /// <summary>Clear the in-memory catalogue and drop the backend. Intended for tests.</summary>
        public static void Reset()
        {
            // Release the bus subscription before dropping the instance, or the orphan keeps
            // reloading the catalogue for the rest of the process and every later sign-in fans out
            // to one more dead service.
            if (_instance != null)
                UGSBus.Unsubscribe(UGS_EventsEnum.PlayerAuthenticated, _instance.OnPlayerAuthenticated);
            _instance = null;
        }

        // A static instance outlives play mode when Enter Play Mode Options disable the domain
        // reload, carrying the previous session's backend, catalogue and subscriber list into the
        // next run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Reset();

        /// <summary>
        /// Fetch definitions and this player's records. Safe to call repeatedly; overlapping calls
        /// collapse into the one already running.
        /// </summary>
        public void LoadAsync() => _ = ReloadAsync();

        /// <summary>The load as an awaitable. Overlapping calls collapse into the one running.</summary>
        private Task ReloadAsync()
        {
            if (_loadTask != null && !_loadTask.IsCompleted) return _loadTask;
            return _loadTask = LoadCoreAsync();
        }

        private async Task LoadCoreAsync()
        {
            // Views are built at scene Awake and their constructors ask for a load, which is long
            // before sign-in completes. Going ahead anyway fetches Remote Config with no player -
            // the SDK warns "Auth Service not initialized", returns an empty config, and the missing
            // definitions then get reported as "Remote Config has no 'achievements' key", sending a
            // developer off to redeploy definitions that were never the problem. Arriving early is
            // normal, so it is not an error either: the constructor subscribes to PlayerAuthenticated
            // and that reload is the one that counts.
            if (!IsSignedIn()) return;

            try
            {
                string playerId = ResolvePlayerId();
                var achievements = await Backend.GetAchievementsAsync(playerId);
                Catalog.SetAchievements(achievements);
            }
            catch (Exception e)
            {
                Report("load achievements", null, e);
            }
        }

        /// <summary>
        /// Make sure the catalogue has been fetched at least once. A mutation announces itself by
        /// looking the achievement up, so an empty catalogue turns a successful unlock into silence.
        /// A failure here is reported and swallowed: it must not stop the unlock it precedes.
        /// </summary>
        private Task EnsureLoadedAsync() => Catalog.Count > 0 ? Task.CompletedTask : ReloadAsync();

        /// <summary>Unlock an achievement, reporting the outcome on the event bus.</summary>
        public async void UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            UGSBus.Publish(UGS_EventsEnum.AchievementClaiming, this, achievementId);
            try
            {
                await EnsureLoadedAsync();
                var dto = await Backend.UnlockAsync(achievementId);
                var achievement = Catalog.Find(achievementId);
                achievement?.Record.Apply(dto);

                UGSBus.Publish(UGS_EventsEnum.AchievementClaimed, this, achievementId);

                if (achievement != null && achievement.Record.Unlocked)
                {
                    UGSBus.Publish(UGS_EventsEnum.AchievementUnlocked, this, achievement);
                    AchievementUnlocked?.Invoke(achievement);
                }
            }
            catch (Exception e)
            {
                Report("unlock achievement", achievementId, e);
            }
        }

        /// <summary>Set absolute progress, reporting the outcome on the event bus.</summary>
        public async void SetProgress(string achievementId, int progressCount)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            try
            {
                await EnsureLoadedAsync();
                var dto = await Backend.SetProgressAsync(achievementId, progressCount);
                var achievement = Catalog.Find(achievementId);
                bool wasUnlocked = achievement?.Record.Unlocked ?? false;
                achievement?.Record.Apply(dto);

                UGSBus.Publish(UGS_EventsEnum.AchievementProgressUpdated, this, achievementId);

                // A backend may unlock on the increment that reaches the target, so a progress call
                // is a legitimate route to an unlock and has to announce it.
                if (achievement != null && !wasUnlocked && achievement.Record.Unlocked)
                {
                    UGSBus.Publish(UGS_EventsEnum.AchievementUnlocked, this, achievement);
                    AchievementUnlocked?.Invoke(achievement);
                }
            }
            catch (Exception e)
            {
                Report("update achievement progress", achievementId, e);
            }
        }

        /// <summary>Clear every record for the current player, then reload.</summary>
        public async void ResetAll()
        {
            try
            {
                await Backend.ResetAllAsync();
                LoadAsync();
            }
            catch (Exception e)
            {
                Report("reset achievements", null, e);
            }
        }

        /// <summary>Whether there is a player to load a catalogue for.</summary>
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

        private static string ResolvePlayerId()
        {
            try
            {
                return Unity.Services.Authentication.AuthenticationService.Instance?.PlayerId;
            }
            catch (Exception)
            {
                // Services not initialised yet. The backend decides whether it can proceed without
                // an id; that is not this method's call to make.
                return null;
            }
        }

        private void Report(string what, string achievementId, Exception e)
        {
            string detail = e is AchievementBackendException ? e.Message : $"{e.GetType().Name}: {e.Message}";
            Debug.LogWarning($"{nameof(AchievementsService)}: could not {what}. {detail}");

            // Only an operation on a specific achievement is a claim failure. Announcing a failed
            // catalogue load or reset as AchievementClaimFailed - carrying a null id - tells any
            // host UI bound to that event that the player's claim did not stick, when the player
            // claimed nothing.
            if (!string.IsNullOrEmpty(achievementId))
                UGSBus.Publish(UGS_EventsEnum.AchievementClaimFailed, this, achievementId);
        }
    }
}
