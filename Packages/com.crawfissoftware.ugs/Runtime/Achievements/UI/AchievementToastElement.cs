using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using CrawfisSoftware.UGS.UI;

namespace CrawfisSoftware.UGS.Achievements.UI
{
    /// <summary>
    /// The unlock toast: slides one achievement in from off-panel, dwells, and slides back out.
    ///    Dependencies: AchievementsService, AchievementIconLibrary, UgsUiTheme
    ///    Subscribes: AchievementsService.AchievementUnlocked (plain C# event)
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para><b>The animation classes are load-bearing.</b> The state machine advances on
    /// <see cref="TransitionEndEvent"/>, so if the stylesheet ever loses the
    /// <c>transition-property</c> rule on the animated class, the toast slides in and never
    /// leaves. The dwell timer is the only part not driven by that event.</para>
    /// <para>Unlocks that arrive while a toast is showing are queued rather than dropped, so
    /// earning two achievements at once shows both.</para>
    /// </remarks>
    public sealed class AchievementToastElement : AchievementViewBase
    {
        /// <summary>How long a toast stays fully visible before retracting, in milliseconds.</summary>
        public const long DwellMilliseconds = 2500;

        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly Label _description;
        private readonly Queue<Achievement> _pending = new Queue<Achievement>();
        private readonly AchievementsService _service;

        private bool _showing;

        public AchievementToastElement(IEnumerable<Texture2D> icons = null)
        {
            AchievementIconLibrary.Register(icons);

            AddToClassList(UgsUiTheme.Achievements.Toast);
            AddToClassList(UgsUiTheme.Achievements.ToastAnimated);
            AddToClassList(UgsUiTheme.Achievements.ToastOffscreen);

            var header = new Label("ACHIEVEMENT UNLOCKED");
            header.AddToClassList(UgsUiTheme.Header);
            header.AddToClassList(UgsUiTheme.HeaderSmall);
            Add(header);

            _icon = new VisualElement();
            _icon.AddToClassList(UgsUiTheme.Achievements.CardIcon);
            Add(_icon);

            _title = new Label();
            _title.AddToClassList(UgsUiTheme.Achievements.CardTitle);
            Add(_title);

            _description = new Label();
            _description.AddToClassList(UgsUiTheme.Achievements.CardDescription);
            Add(_description);

            RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
            RegisterCallback<DetachFromPanelEvent>(_ => Dispose());

            _service = AchievementsService.Instance;
            _service.AchievementUnlocked += OnAchievementUnlocked;
        }

        private void OnAchievementUnlocked(Achievement achievement)
        {
            if (achievement == null) return;
            _pending.Enqueue(achievement);
            if (!_showing) ShowNext();
        }

        private void ShowNext()
        {
            if (_pending.Count == 0)
            {
                _showing = false;
                return;
            }

            _showing = true;
            Bind(_pending.Dequeue());
            RemoveFromClassList(UgsUiTheme.Achievements.ToastOffscreen);
        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            bool offscreen = ClassListContains(UgsUiTheme.Achievements.ToastOffscreen);
            if (offscreen)
            {
                // Finished retracting: release the achievement and take the next one.
                Unbind();
                ShowNext();
                return;
            }

            // Finished arriving: dwell, then retract.
            schedule.Execute(() => AddToClassList(UgsUiTheme.Achievements.ToastOffscreen))
                    .ExecuteLater(DwellMilliseconds);
        }

        protected override void Redraw()
        {
            var achievement = Achievement;
            if (achievement == null) return;

            _title.text = achievement.Definition.Title;
            _description.text = achievement.Definition.Description;

            var texture = AchievementIconLibrary.Get(achievement.Definition.Icon);
            _icon.style.backgroundImage = texture == null ? null : new StyleBackground(texture);
        }

        /// <summary>Stop listening for unlocks. Called automatically on detach.</summary>
        public void Dispose()
        {
            _service.AchievementUnlocked -= OnAchievementUnlocked;
            Unbind();
        }
    }
}
