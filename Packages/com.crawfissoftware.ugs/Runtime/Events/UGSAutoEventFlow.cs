using CrawfisSoftware.Events;

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Services.Core;


using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Events
{
    /// <summary>
    /// Auto-chains UGS events. Entries marked with [AUTO] are active; others are published by controllers.
    ///
    /// ========================================================================================
    /// UGS EVENT FLOW TIMELINE (from actual event trace)
    /// ========================================================================================
    ///
    /// --- BOOT / INITIALIZATION ---
    /// [AUTO] UnityServicesInitialized -> CheckForExistingSession
    /// [Published] CheckForExistingSessionSucceeded (or CheckForExistingSessionFailed)
    /// [AUTO] CheckForExistingSessionSucceeded -> PlayerAuthenticating
    /// [AUTO] CheckForExistingSessionFailed -> PlayerSigningIn (show sign-in UI)
    /// [Published] PlayerAuthenticated (by auth controller)
    /// [BRIDGE->GameFlow] PlayerAuthenticated -> GameplayReady
    /// [AUTO] PlayerAuthenticated -> RemoteConfigFetching
    /// [Published] RemoteConfigFetched -> RemoteConfigUpdated
    /// [BRIDGE->GameFlow] RemoteConfigUpdated -> LoadingScreenHideRequested
    ///
    /// --- SIGN IN/OUT FLOW ---
    /// [AUTO] PlayerSignedIn -> PlayerAuthenticating
    /// [AUTO] PlayerSignedOut -> PlayerSigningIn (loop back)
    /// [AUTO] PlayerSignInFailed -> PlayerSigningIn (retry)
    ///
    /// --- POST-GAME: LEADERBOARD ---
    /// [BRIDGE: GameFlow->UGS] GameEnded -> LeaderboardOpening
    /// [AUTO] LeaderboardCloseRequested -> LeaderboardClosing -> LeaderboardClosed
    /// [AUTO] LeaderboardClosed -> AchievementsOpenRequested
    ///
    /// --- POST-GAME: ACHIEVEMENTS ---
    /// [AUTO] AchievementsOpenRequested -> AchievementsOpening
    /// [Published] AchievementClaimRequested -> AchievementClaiming -> AchievementClaimed
    /// [AUTO] AchievementsCloseRequested -> AchievementsClosing -> AchievementsClosed
    /// [AUTO] AchievementsClosed -> PlayerAuthenticating (loop back to main menu)
    ///
    /// ========================================================================================
    /// </summary>

    internal class UGSAutoEventFlow : AutoEventFlowBase<UGS_EventsEnum, UGS_EventsEnum>
    {
        private static readonly (UGS_EventsEnum From, UGS_EventsEnum To)[] ChainTable =
        {
            // --- Initialization / boot ---
            //(UGS_EventsEnum.UGS_InitializationStarted, UGS_EventsEnum.UGS_InitializationCompleted),
            (UGS_EventsEnum.UnityServicesInitialized, UGS_EventsEnum.CheckForExistingSession),
            //(UGS_EventsEnum.UnityServicesInitializationFailed, UGS_EventsEnum.UGS_InitializationFailed),

            // Session -> Auth
            //(UGS_EventsEnum.CheckForExistingSession, UGS_EventsEnum.CheckForExistingSessionSucceeded),
            (UGS_EventsEnum.CheckForExistingSessionSucceeded, UGS_EventsEnum.PlayerAuthenticating),
            (UGS_EventsEnum.CheckForExistingSessionFailed, UGS_EventsEnum.PlayerSigningIn),

            // Sign in loop
            //(UGS_EventsEnum.PlayerSigningIn, UGS_EventsEnum.PlayerSignedIn),          // Published by PlayerSignInController
            (UGS_EventsEnum.PlayerSignedIn, UGS_EventsEnum.PlayerAuthenticating),
            //(UGS_EventsEnum.PlayerAuthenticating, UGS_EventsEnum.PlayerAuthenticated), // Published by PlayerSignInController

            // Remote config refresh anytime authenticated changes
            (UGS_EventsEnum.PlayerAuthenticated, UGS_EventsEnum.RemoteConfigFetching), // First time and anytime player changes.
            //(UGS_EventsEnum.RemoteConfigFetching, UGS_EventsEnum.RemoteConfigFetched),
            //(UGS_EventsEnum.RemoteConfigFetched, UGS_EventsEnum.RemoteConfigUpdated),
            //(UGS_EventsEnum.RemoteConfigFetchFailed, UGS_EventsEnum.RemoteConfigFailed),

            // Sign out loop
            //(UGS_EventsEnum.PlayerSigningOut, UGS_EventsEnum.PlayerSignedOut),
            (UGS_EventsEnum.PlayerSignedOut, UGS_EventsEnum.PlayerSigningIn), // Loop back to allow re-sign in
            (UGS_EventsEnum.PlayerSignInFailed, UGS_EventsEnum.PlayerSigningIn), // Loop back to allow re-sign in

            // --- Post-game UGS UI loop ---
            //(UGS_EventsEnum.ScoreUpdating, UGS_EventsEnum.ScoreUpdated),

            //(UGS_EventsEnum.LeaderboardOpening, UGS_EventsEnum.LeaderboardOpened), // Published by LeaderboardController
            //(UGS_EventsEnum.LeaderboardOpened, UGS_EventsEnum.LeaderboardClosing),
            (UGS_EventsEnum.LeaderboardClosing, UGS_EventsEnum.LeaderboardClosed),
            (UGS_EventsEnum.LeaderboardClosed, UGS_EventsEnum.AchievementsOpenRequested),

            (UGS_EventsEnum.AchievementsOpenRequested, UGS_EventsEnum.AchievementsOpening),
            //(UGS_EventsEnum.AchievementsOpening, UGS_EventsEnum.AchievementsOpened),
            (UGS_EventsEnum.AchievementsCloseRequested, UGS_EventsEnum.AchievementsClosing),
            (UGS_EventsEnum.AchievementsClosing, UGS_EventsEnum.AchievementsClosed),
            // Re-running authentication republishes PlayerAuthenticated, which is what re-announces
            // ServicesReady to the host and so brings the main menu back after a run. Closing the
            // last post-game panel is the only trigger for that, so the edge has to start here.
            // It used to run through RewardAdWatching -> RewardAdWatched, carried over from a build
            // that had an ads SDK. This package ships none and nothing subscribes to those events,
            // so the detour asserted the player had watched a rewarded ad that was never shown.
            // The RewardAd* enum members stay for a host that really integrates ads; nothing in
            // this package publishes them.
            (UGS_EventsEnum.AchievementsClosed, UGS_EventsEnum.PlayerAuthenticating),
        };

        protected override IReadOnlyList<(UGS_EventsEnum From, UGS_EventsEnum To)> Chains => ChainTable;

        protected virtual void Start()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                UGSBus.Publish(UGS_EventsEnum.UnityServicesInitialized, this, null);
            }
        }
    }
}