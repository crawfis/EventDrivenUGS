using System;
using System.Collections.Generic;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// The set of achievements currently known, and the thing the achievements panel renders.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: Changed (a plain C# event - view state, not a cross-system signal)
    /// </summary>
    /// <remarks>
    /// Deliberately a plain list plus an <see cref="Action"/>. The panel is built imperatively in
    /// C# with no UXML and no data binding, so the property-binding machinery this replaces had no
    /// consumer at all - it only made the type harder to read.
    /// </remarks>
    public sealed class AchievementCatalog
    {
        private readonly List<Achievement> _achievements = new List<Achievement>();
        private readonly Dictionary<string, Achievement> _byId = new Dictionary<string, Achievement>();

        /// <summary>Raised when the set itself changes - not when one record inside it changes.</summary>
        public event Action Changed;

        public IReadOnlyList<Achievement> Achievements => _achievements;

        public int Count => _achievements.Count;

        public void SetAchievements(IEnumerable<Achievement> achievements)
        {
            _achievements.Clear();
            _byId.Clear();
            if (achievements != null)
            {
                foreach (var achievement in achievements)
                {
                    if (achievement?.Definition?.Id == null) continue;
                    if (_byId.ContainsKey(achievement.Id)) continue;
                    _achievements.Add(achievement);
                    _byId[achievement.Id] = achievement;
                }
            }
            Changed?.Invoke();
        }

        public bool TryGet(string achievementId, out Achievement achievement)
        {
            if (achievementId == null)
            {
                achievement = null;
                return false;
            }
            return _byId.TryGetValue(achievementId, out achievement);
        }

        /// <summary>The achievement with this id, or null when it is not in the catalogue.</summary>
        public Achievement Find(string achievementId) =>
            TryGet(achievementId, out var achievement) ? achievement : null;

        public void Clear()
        {
            if (_achievements.Count == 0) return;
            _achievements.Clear();
            _byId.Clear();
            Changed?.Invoke();
        }
    }
}
