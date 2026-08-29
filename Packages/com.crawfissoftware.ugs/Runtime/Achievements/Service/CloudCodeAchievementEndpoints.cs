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

        /// <summary>
        /// The conventional function names, with <b>no module name</b>.
        /// </summary>
        /// <remarks>
        /// This package ships no Cloud Code module, so there is no honest default for
        /// <see cref="ModuleName"/> and it is deliberately left null: <see cref="IsComplete"/> then
        /// fails, and <see cref="CloudCodeAchievementBackend"/> refuses to construct with a message
        /// naming the thing to configure. A placeholder module name would instead have produced a
        /// module-not-found error on every call, at runtime, far from the cause.
        /// </remarks>
        public static CloudCodeAchievementEndpoints Default => new CloudCodeAchievementEndpoints
        {
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
