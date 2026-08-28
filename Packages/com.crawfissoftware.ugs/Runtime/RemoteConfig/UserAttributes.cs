namespace CrawfisSoftware.UGS
{
    /// <summary>
    /// Remote Config's FetchConfigs call requires passing user-specific attributes
    /// for personalization and targeting of configuration values.
    /// </summary>
    public struct UserAttributes
    {
        /// <summary>
        /// The type of device the game is running on (e.g., Desktop, Handheld)
        /// </summary>
        public string DeviceType;
        
        /// <summary>
        /// The platform the game is running on (e.g., WindowsPlayer, Android, iOS)
        /// </summary>
        public string Platform;
        
        /// <summary>
        /// The version of the application
        /// </summary>
        public string AppVersion;
        
        /// <summary>
        /// The player's current level for difficulty targeting
        /// </summary>
        public int PlayerLevel;
        
        /// <summary>
        /// The country/region for localization and regional targeting
        /// </summary>
        public string Country;
    }
}