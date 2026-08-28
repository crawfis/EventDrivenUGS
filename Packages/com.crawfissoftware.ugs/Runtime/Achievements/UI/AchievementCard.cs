using UnityEngine.UIElements;

using CrawfisSoftware.UGS.UI;

namespace CrawfisSoftware.UGS.Achievements.UI
{
    /// <summary>
    /// One achievement rendered as a card: icon, title, description, unlocked stamp and progress.
    ///    Dependencies: AchievementIconLibrary, UgsUiTheme
    ///    Subscribes: AchievementRecord.Changed (via AchievementViewBase)
    ///    Publishes: none
    /// </summary>
    public sealed class AchievementCard : AchievementViewBase
    {
        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly Label _description;
        private readonly Label _unlocked;
        private readonly AchievementProgressView _progress;

        public AchievementCard()
        {
            AddToClassList(UgsUiTheme.Achievements.Card);

            _icon = new VisualElement();
            _icon.AddToClassList(UgsUiTheme.Achievements.CardIcon);
            Add(_icon);

            _title = new Label();
            _title.AddToClassList(UgsUiTheme.Achievements.CardTitle);
            Add(_title);

            _description = new Label();
            _description.AddToClassList(UgsUiTheme.Achievements.CardDescription);
            Add(_description);

            _unlocked = new Label("UNLOCKED");
            _unlocked.AddToClassList(UgsUiTheme.Achievements.CardUnlockedLabel);
            Add(_unlocked);

            _progress = new AchievementProgressView();
            Add(_progress);
        }

        public AchievementCard(Achievement achievement) : this()
        {
            Bind(achievement);
        }

        protected override void Redraw()
        {
            var achievement = Achievement;
            if (achievement == null)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            _title.text = ResolveTitle(achievement);
            _description.text = ResolveDescription(achievement);

            var texture = AchievementIconLibrary.Get(achievement.Definition.Icon);
            _icon.style.backgroundImage = texture == null ? null : new StyleBackground(texture);

            // The stylesheet's base state for this label is display:none, so an inline style is the
            // only thing that can reveal it.
            _unlocked.style.display = achievement.Record.Unlocked ? DisplayStyle.Flex : DisplayStyle.None;

            _progress.Show(achievement);
        }
    }
}
