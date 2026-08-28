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
    ///    Subscribes: none
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
        private bool _useTrustedClient;
        private bool _loading;

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
                if (_useTrustedClient == value && _backend != null) return;
                _useTrustedClient = value;
                _backend = null;
            }
        }

        /// <summary>
        /// The backend in use, built on first access from <see cref="UseTrustedClient"/>. Assignable
        /// so a test, or a game with its own storage, can substitute one.
        /// </summary>
        public IAchievementBackend Backend
        {
            get => _backend ??= _useTrustedClient
                ? new CloudCodeAchievementBackend()
                : (IAchievementBackend)new CloudSaveAchievementBackend();
            set => _backend = value;
        }

        /// <summary>Clear the in-memory catalogue and drop the backend. Intended for tests.</summary>
        public static void Reset()
        {
            _instance = null;
        }

        /// <summary>
        /// Fetch definitions and this player's records. Safe to call repeatedly; overlapping calls
        /// collapse into the one already running.
        /// </summary>
        public async void LoadAsync()
        {
            if (_loading) return;
            _loading = true;
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
            finally
            {
                _loading = false;
            }
        }

        /// <summary>Unlock an achievement, reporting the outcome on the event bus.</summary>
        public async void UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            UGSBus.Publish(UGS_EventsEnum.AchievementClaiming, this, achievementId);
            try
            {
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
            UGSBus.Publish(UGS_EventsEnum.AchievementClaimFailed, this, achievementId);
        }
    }
}
