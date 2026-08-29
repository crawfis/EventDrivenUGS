namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Every contractual string in the achievements pipeline, in one place.
    ///    Dependencies: none (pure constants)
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>These values are shared by three parties that cannot reference each other: the editor
    /// exporter that authors the JSON, the runtime backend that reads it, and the Cloud Code module
    /// - a separate .NET project that cannot reference a Unity assembly at all. Two of the three
    /// can at least share this file; the third is why every value here is documented rather than
    /// merely named.</para>
    /// <para>Changing a value here means redeploying the Remote Config entry and editing the Cloud
    /// Code module to match. A mismatch does not fail to compile: the definitions simply arrive
    /// empty and no achievement ever unlocks.</para>
    /// </remarks>
    public static class AchievementsRemoteConfigKeys
    {
        /// <summary>Remote Config entry key holding the authored definition array.</summary>
        public const string AchievementsKey = "achievements";

        /// <summary>
        /// Cloud Save player-data key holding this player's records. The same literal as
        /// <see cref="AchievementsKey"/> today, but a different service - kept as two constants so
        /// one can move without dragging the other with it.
        /// </summary>
        public const string CloudSaveAchievementsKey = "achievements";

        /// <summary>Remote Config's value-type literal for a JSON entry.</summary>
        public const string JsonValueType = "JSON";

        /// <summary>Unity-published schema URL for Remote Config authoring files.</summary>
        public const string RemoteConfigSchemaUrl =
            "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json";

        /// <summary>
        /// The extension com.unity.remote-config's own importer claims. Exporting to it is what
        /// makes a written file appear in the Deployment window without this package registering a
        /// ScriptedImporter of its own - and therefore without an extension to collide over.
        /// Includes the dot.
        /// </summary>
        public const string RemoteConfigFileExtension = ".rc";
    }
}
