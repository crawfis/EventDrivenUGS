using System;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// The authored, player-independent half of an achievement: what it is called, what it looks
    /// like, and what it takes to earn.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para><b>These field names are a wire contract.</b> They are the JSON keys of every entry
    /// under the Remote Config <c>achievements</c> key, which lives server-side. Renaming one here
    /// does not fail to compile and does not fail to deserialize - the field simply arrives empty,
    /// so an achievement silently loses its title or its icon.</para>
    /// <para>Public fields rather than properties for the same reason: the names are the schema,
    /// and a field makes that impossible to miss.</para>
    /// </remarks>
    [Serializable]
    public class AchievementDefinition
    {
        /// <summary>Stable identifier. Also the Cloud Save record key.</summary>
        public string Id;

        /// <summary>
        /// Icon name, matched against <c>Texture2D.name</c> by <see cref="AchievementIconLibrary"/>.
        /// Not a path and not a GUID - a bare asset name.
        /// </summary>
        public string Icon;

        public string Title;
        public string Description;

        /// <summary>Hide the title and description until the achievement is unlocked.</summary>
        public bool IsHidden;

        /// <summary>
        /// How many increments earn it. 1 (or 0) means a simple unlock with no progress bar.
        /// </summary>
        public int ProgressTarget;

        public AchievementDefinition()
        {
        }

        /// <summary>Whether this achievement should render a progress bar.</summary>
        public bool HasProgress => ProgressTarget > 1;
    }
}
