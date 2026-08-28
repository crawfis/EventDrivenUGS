namespace CrawfisSoftware.UGS
{
    /// <summary>
    /// Remote Config's FetchConfigs call requires passing application-specific attributes
    /// for targeting configuration values based on app characteristics.
    /// </summary>
    public struct AppAttributes
    {
        /// <summary>
        /// The version of the application
        /// </summary>
        public string AppVersion;
        
        /// <summary>
        /// The build number/GUID for this specific build
        /// </summary>
        public string BuildNumber;
        
        /// <summary>
        /// The version of Unity used to build the application
        /// </summary>
        public string UnityVersion;
        
        /// <summary>
        /// Whether this is a debug build or release build
        /// </summary>
        public bool IsDebugBuild;
    }
}