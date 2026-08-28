using System;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// The persisted shape of one player's progress against one achievement.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Split out from <see cref="AchievementRecord"/> so the serializer never needs to reach
    /// a private setter, and so the persisted JSON is explicit rather than a side effect of how the
    /// runtime type happens to be written.</para>
    /// <para><b>Wire contract.</b> Serialized as a JSON array under the Cloud Save player key
    /// <c>achievements</c>. Renaming any of these three orphans every existing player's saved
    /// records - they read back as a fresh, empty set, with nothing in the console to say so.</para>
    /// </remarks>
    [Serializable]
    public class AchievementRecordDto
    {
        public string Id { get; set; }
        public bool Unlocked { get; set; }
        public int ProgressCount { get; set; }
    }
}
