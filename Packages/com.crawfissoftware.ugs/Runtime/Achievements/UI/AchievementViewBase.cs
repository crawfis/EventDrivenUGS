using UnityEngine.UIElements;

using CrawfisSoftware.UGS.UI;

namespace CrawfisSoftware.UGS.Achievements.UI
{
    /// <summary>
    /// Shared behaviour for any element that displays a single achievement: it holds the
    /// achievement, follows its record, and redraws when that record moves.
    ///    Dependencies: AchievementIconLibrary, UgsUiTheme
    ///    Subscribes: AchievementRecord.Changed (plain C# event)
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// The subscription is torn down in <see cref="Unbind"/> and whenever a new achievement is
    /// bound, so a recycled element never keeps redrawing for the achievement it used to show.
    /// </remarks>
    public abstract class AchievementViewBase : VisualElement
    {
        private Achievement _achievement;

        /// <summary>The achievement currently displayed, or null when nothing is bound.</summary>
        public Achievement Achievement => _achievement;

        protected AchievementViewBase()
        {
            AddToClassList(UgsUiTheme.Achievements.Base);
        }

        /// <summary>Display this achievement and follow its record until something else is bound.</summary>
        public void Bind(Achievement achievement)
        {
            Unbind();
            _achievement = achievement;
            if (_achievement != null)
                _achievement.Record.Changed += OnRecordChanged;
            Redraw();
        }

        /// <summary>Stop following the current achievement. Safe to call more than once.</summary>
        public void Unbind()
        {
            if (_achievement != null)
                _achievement.Record.Changed -= OnRecordChanged;
            _achievement = null;
        }

        private void OnRecordChanged() => Redraw();

        /// <summary>
        /// Push the bound achievement's current state into this element's widgets. Called on bind
        /// and on every record change; must tolerate a null <see cref="Achievement"/>.
        /// </summary>
        protected abstract void Redraw();

        /// <summary>
        /// The title to show, honouring <see cref="AchievementDefinition.IsHidden"/>: a hidden
        /// achievement reveals nothing about itself until it is earned.
        /// </summary>
        protected static string ResolveTitle(Achievement achievement) =>
            achievement.Definition.IsHidden && !achievement.Record.Unlocked
                ? "???"
                : achievement.Definition.Title;

        /// <summary>The description to show, honouring the same hidden rule as the title.</summary>
        protected static string ResolveDescription(Achievement achievement) =>
            achievement.Definition.IsHidden && !achievement.Record.Unlocked
                ? "Hidden achievement"
                : achievement.Definition.Description;
    }
}
