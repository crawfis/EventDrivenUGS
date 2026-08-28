using CrawfisSoftware.Events;

namespace CrawfisSoftware.UGS.Events
{
    [EventEnum]
    public enum UGS_EventsEnum
    {
        None,
        // --- Initialization ---
        UnityServicesInitialized, UnityServicesInitializationFailed,
        UGS_InitializationStarted, UGS_InitializationCompleted, UGS_InitializationFailed,
        CheckForExistingSession, CheckForExistingSessionFailed, CheckForExistingSessionSucceeded,
        // --- Authentication ---
        PlayerResumedFromExpiredToken, PlayerSigningIn, PlayerSignedIn, PlayerAuthenticating, PlayerAuthenticated,
        PlayerSigningOut, PlayerSignedOut, PlayerSignInFailed, PlayerSessionExpired,
        UserAccountDeleted,
        GameManagerInstantiated,
        // --- Remote Config ---
        RemoteConfigFetching, RemoteConfigFetched, RemoteConfigFetchFailed, RemoteConfigUpdated, RemoteConfigFailed,
        DifficultySettingsFetched,
        // --- Leaderboards ---
        ScoreUpdating, ScoreUpdated, ScoreFailedToUpdate,
        LeaderboardOpening, LeaderboardOpened, LeaderboardCloseRequested, LeaderboardClosing, LeaderboardClosed,
        // --- Achievements ---
        AchievementsOpenRequested, AchievementsOpening, AchievementsOpened,
        AchievementsCloseRequested, AchievementsClosing, AchievementsClosed,
        AchievementClaimRequested, AchievementClaiming, AchievementClaimed, AchievementClaimFailed,
        AchievementUnlocked, AchievementProgressUpdated,
        UGS_DistanceUpdated,
        // --- Economy ---
        UGS_CoinUpdated,
        // --- Rewarded Ads ---
        RewardAdWatching, RewardAdWatched,
        RewardAdFailedToShow, RewardAdClosedWithoutReward,
    }
}