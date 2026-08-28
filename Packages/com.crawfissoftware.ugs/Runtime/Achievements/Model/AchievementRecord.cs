using System;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// One player's mutable progress against one achievement, with a change notification the UI
    /// binds to.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: Changed (a plain C# event, not the event bus - this is view state)
    /// </summary>
    /// <remarks>
    /// <para><b>Mutated in place, never replaced.</b> A card element subscribes to
    /// <see cref="Changed"/> once, when it is built. Swapping in a new record object on unlock -
    /// which a purely immutable model would force - would leave every bound card subscribed to an
    /// orphan, and the panel would quietly stop updating with nothing in the console to explain
    /// it.</para>
    /// <para><see cref="Changed"/> is raised only on an actual transition, so a backend that
    /// re-sends identical state does not churn the UI.</para>
    /// </remarks>
    public sealed class AchievementRecord
    {
        /// <summary>Raised after <see cref="Unlocked"/> or <see cref="ProgressCount"/> changes.</summary>
        public event Action Changed;

        public string Id { get; }
        public bool Unlocked { get; private set; }
        public int ProgressCount { get; private set; }

        public AchievementRecord(string id, bool unlocked = false, int progressCount = 0)
        {
            Id = id;
            Unlocked = unlocked;
            ProgressCount = progressCount;
        }

        public void SetUnlocked(bool unlocked)
        {
            if (Unlocked == unlocked) return;
            Unlocked = unlocked;
            Changed?.Invoke();
        }

        public void SetProgress(int progressCount)
        {
            if (ProgressCount == progressCount) return;
            ProgressCount = progressCount;
            Changed?.Invoke();
        }

        /// <summary>
        /// Adopt authoritative state from a backend, raising <see cref="Changed"/> at most once
        /// even when both fields move.
        /// </summary>
        public void Apply(AchievementRecordDto dto)
        {
            if (dto == null) return;
            bool changed = Unlocked != dto.Unlocked || ProgressCount != dto.ProgressCount;
            Unlocked = dto.Unlocked;
            ProgressCount = dto.ProgressCount;
            if (changed) Changed?.Invoke();
        }

        public AchievementRecordDto ToDto() =>
            new AchievementRecordDto { Id = Id, Unlocked = Unlocked, ProgressCount = ProgressCount };
    }
}
