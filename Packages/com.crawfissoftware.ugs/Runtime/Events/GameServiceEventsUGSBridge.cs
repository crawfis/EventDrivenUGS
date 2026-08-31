using CrawfisSoftware.Contracts;
using CrawfisSoftware.Events;

using UnityEngine;
using GameServiceBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.Contracts.GameServiceEvents>;
using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Events
{
    /// <summary>
    /// Translates between the game-agnostic <see cref="GameServiceEvents"/> contract and this
    /// layer's own UGS events. The only boundary UGS has with the outside world.
    /// </summary>
    /// <remarks>
    /// <para>Still generic: it names <see cref="GameServiceEvents"/> and
    /// <see cref="UGS_EventsEnum"/>, and no game type at all.</para>
    /// <para>It also owns the services <em>level</em>. A plain event-to-event mapping cannot do
    /// that, because a dispatcher forwards the source event's payload and the level needs a
    /// <see cref="ServicesStatus"/> value chosen per source. So the status is published here, by
    /// hand, from the events that actually change it.</para>
    /// </remarks>
    internal class GameServiceEventsUGSBridge : MonoBehaviour
    {
        private static readonly EventId<ServicesStatus> StatusChanged =
            GameServiceBus.Id<ServicesStatus>(GameServiceEvents.ServicesStatusChanged);

        private static readonly (GameServiceEvents From, UGS_EventsEnum To)[] GameServiceToUGS =
        {
            // The game's score metric drives distance-based achievements. UGS does not know the
            // metric is metres - only that it is the number the game scores on.
            (GameServiceEvents.ScoreUpdated, UGS_EventsEnum.UGS_DistanceUpdated),

            // Soft-currency total drives economy sync and coin achievements.
            (GameServiceEvents.CurrencyTotalChanged, UGS_EventsEnum.UGS_CoinUpdated),

            // A finished run is a score to submit, coins to bank, then a leaderboard to show.
            // SessionEnding appears twice deliberately: the pair list exists so one event can
            // declare several consequences, and these two are independent of each other.
            (GameServiceEvents.SessionEnding, UGS_EventsEnum.ScoreUpdating),
            (GameServiceEvents.SessionEnding, UGS_EventsEnum.CurrencySyncRequested),
            (GameServiceEvents.SessionEnded, UGS_EventsEnum.LeaderboardOpening),
        };

        private static readonly (UGS_EventsEnum From, GameServiceEvents To)[] UGSToGameService =
        {
            // The edges, for anything that wants the moment rather than the state.
            (UGS_EventsEnum.PlayerAuthenticated, GameServiceEvents.ServicesReady),
            (UGS_EventsEnum.PlayerSignedOut, GameServiceEvents.ServicesUnavailable),

            // UGS announces that config arrived. What the host does about it - hiding a loading
            // screen, say - is the host's business, not this layer's.
            (UGS_EventsEnum.RemoteConfigUpdated, GameServiceEvents.RemoteConfigApplied),
            (UGS_EventsEnum.DifficultySettingsFetched, GameServiceEvents.DifficultySettingsAvailable),
        };

        /// <summary>UGS events that mean "services are no longer usable".</summary>
        private static readonly UGS_EventsEnum[] FailureEvents =
        {
            UGS_EventsEnum.UnityServicesInitializationFailed,
            UGS_EventsEnum.UGS_InitializationFailed,
            UGS_EventsEnum.PlayerSignInFailed,
            UGS_EventsEnum.PlayerSignedOut,
            UGS_EventsEnum.PlayerSessionExpired,
        };

        private readonly EventChainDispatcher<GameServiceEvents, UGS_EventsEnum> _gameServiceToUGS =
            new EventChainDispatcher<GameServiceEvents, UGS_EventsEnum>(GameServiceToUGS);

        private readonly EventChainDispatcher<UGS_EventsEnum, GameServiceEvents> _ugsToGameService =
            new EventChainDispatcher<UGS_EventsEnum, GameServiceEvents>(UGSToGameService);

        protected virtual void Awake()
        {
            _gameServiceToUGS.Attach();
            _ugsToGameService.Attach();

            UGSBus.Subscribe(UGS_EventsEnum.PlayerAuthenticated, OnAuthenticated);
            for (int i = 0; i < FailureEvents.Length; i++)
            {
                UGSBus.Subscribe(FailureEvents[i], OnFailed);
            }

            // Publish the starting level before anything can ask. This scene loads first in the
            // UGS chain, so "Connecting" is the honest answer until authentication says otherwise,
            // and a host that subscribes late still gets a state rather than silence.
            StatusChanged.Publish(this, ServicesStatus.Connecting);
        }

        protected virtual void OnDestroy()
        {
            _gameServiceToUGS.Detach();
            _ugsToGameService.Detach();

            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerAuthenticated, OnAuthenticated);
            for (int i = 0; i < FailureEvents.Length; i++)
            {
                UGSBus.Unsubscribe(FailureEvents[i], OnFailed);
            }
        }

        private void OnAuthenticated(string eventName, object sender, object data)
        {
            StatusChanged.Publish(this, ServicesStatus.Ready);
        }

        private void OnFailed(string eventName, object sender, object data)
        {
            StatusChanged.Publish(this, ServicesStatus.Unavailable);
        }
    }
}
