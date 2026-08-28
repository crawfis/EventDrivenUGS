using System;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// The response shape of the Cloud Code "get achievements" endpoint: a definition paired with
    /// this player's record.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// Declared here rather than taken from generated Cloud Code bindings. Those bindings are
    /// generated into the consumer's own project, into a fixed folder and under a fixed assembly
    /// name, and a package cannot reference an assembly that lives in the consumer's Assets - so a
    /// package that depended on them could not compile at all.
    /// </remarks>
    [Serializable]
    public class AchievementDto
    {
        public AchievementDefinition Definition { get; set; }
        public AchievementRecordDto Record { get; set; }
    }
}
