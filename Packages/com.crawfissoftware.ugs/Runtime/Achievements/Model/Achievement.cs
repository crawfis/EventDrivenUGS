namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// An achievement as the UI sees it: its authored definition paired with this player's record.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <see cref="Record"/> is never null. Leaving it null until a backend filled it in is what
    /// forced every element to null-check before drawing; starting from a zeroed record means an
    /// offline or not-yet-loaded achievement renders as "no progress" rather than not rendering.
    /// </remarks>
    public sealed class Achievement
    {
        public AchievementDefinition Definition { get; }
        public AchievementRecord Record { get; }

        public string Id => Definition.Id;

        public Achievement(AchievementDefinition definition, AchievementRecordDto record = null)
        {
            Definition = definition;
            Record = new AchievementRecord(definition.Id, record?.Unlocked ?? false, record?.ProgressCount ?? 0);
        }
    }
}
