using UnityEngine.UIElements;

using CrawfisSoftware.UGS.UI;

namespace CrawfisSoftware.UGS.Achievements.UI
{
    /// <summary>
    /// A labelled progress bar for one achievement: "3 / 10" over a filled track.
    ///    Dependencies: UgsUiTheme
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// Collapses itself for an achievement with no progress target, so a simple unlock does not
    /// render an always-empty or always-full bar.
    /// </remarks>
    public sealed class AchievementProgressView : VisualElement
    {
        private readonly Label _counter;
        private readonly ProgressBar _bar;

        public AchievementProgressView()
        {
            AddToClassList(UgsUiTheme.Achievements.CardProgress);

            var header = new VisualElement();
            header.AddToClassList(UgsUiTheme.Achievements.CardProgressHeader);
            _counter = new Label();
            _counter.AddToClassList(UgsUiTheme.Label);
            header.Add(_counter);
            Add(header);

            _bar = new ProgressBar { lowValue = 0f, highValue = 1f };
            _bar.AddToClassList(UgsUiTheme.ProgressBar);
            Add(_bar);
        }

        /// <summary>Show this achievement's progress, or hide the view when it tracks none.</summary>
        public void Show(Achievement achievement)
        {
            if (achievement == null || !achievement.Definition.HasProgress)
            {
                style.display = DisplayStyle.None;
                return;
            }

            int target = achievement.Definition.ProgressTarget;
            int count = achievement.Record.ProgressCount;
            if (count > target) count = target;

            style.display = DisplayStyle.Flex;
            _counter.text = $"{count} / {target}";
            _bar.value = target > 0 ? (float)count / target : 0f;
        }
    }
}
