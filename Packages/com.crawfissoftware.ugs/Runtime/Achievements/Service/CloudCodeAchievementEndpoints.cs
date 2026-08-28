using System;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Names the Cloud Code module and the four endpoints the trusted backend calls.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// These are data rather than constants because a package must not hard-code a Cloud Code
    /// module that the consumer may not have deployed, may have named differently, or may have
    /// merged into a larger module. A project can point the backend at whatever it actually
    /// deployed without editing package code.
    /// </remarks>
    [Serializable]
    public struct CloudCodeAchievementEndpoints
    {
        public string ModuleName;
        public string GetAchievements;
        public string UnlockAchievement;
        public string UpdateAchievementProgress;
        public string ResetAllAchievements;

        /// <summary>The endpoint names this package's own sample module publishes.</summary>
        public static CloudCodeAchievementEndpoints Default => new CloudCodeAchievementEndpoints
        {
            ModuleName = "AchievementsModule",
            GetAchievements = "GetAchievements",
            UnlockAchievement = "UnlockAchievement",
            UpdateAchievementProgress = "UpdateAchievementProgress",
            ResetAllAchievements = "ResetAllAchievements",
        };

        /// <summary>True when every endpoint name has been filled in.</summary>
        public bool IsComplete =>
            !string.IsNullOrEmpty(ModuleName) &&
            !string.IsNullOrEmpty(GetAchievements) &&
            !string.IsNullOrEmpty(UnlockAchievement) &&
            !string.IsNullOrEmpty(UpdateAchievementProgress) &&
            !string.IsNullOrEmpty(ResetAllAchievements);
    }
}
