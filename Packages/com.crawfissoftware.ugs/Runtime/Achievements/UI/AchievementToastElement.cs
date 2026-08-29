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
    /// <para>The animation classes shape the movement, but they are deliberately not what advances
    /// the state machine. <see cref="TransitionEndEvent"/> fires only when a transition really runs,
    /// and it does not run for an element that is <c>display:none</c>, never laid out, or whose
    /// stylesheet has lost its <c>transition-property</c> rule - so a toast driven by that event
    /// alone arrives and never leaves, stranding every queued unlock behind it. Each leg therefore
    /// carries a timer, and the transition event is an early-out rather than the only way forward.
    /// </para>
    /// <para>Unlocks that arrive while a toast is showing are queued rather than dropped, so
    /// earning two achievements at once shows both. The queue survives a panel reload; the
    /// half-shown toast does not.</para>
    /// </remarks>
    public sealed class AchievementToastElement : AchievementViewBase
    {
        /// <summary>How long a toast stays fully visible before retracting, in milliseconds.</summary>
        public const long DwellMilliseconds = 2500;

        /// <summary>
        /// How long to wait for the retract transition to report an end before advancing anyway.
        /// Generous on purpose: it is a backstop for a transition that will never fire, not a
        /// second animation clock racing the first.
        /// </summary>
        private const long RetractWatchdogMilliseconds = 1500;

        private readonly VisualElement _icon;
        private readonly Label _title;
        private readonly Label _description;
        private readonly Queue<Achievement> _pending = new Queue<Achievement>();
        private readonly AchievementsService _service;

        private bool _showing;
        private bool _subscribed;
        private int _generation;
        private IVisualElementScheduledItem _advance;

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

            // Subscribing is tied to the ATTACH lifecycle, not to the constructor. A PanelRenderer
            // rebuilds its whole tree on every reload, so this element is detached and re-added -
            // and an element that only ever unsubscribes is deaf from the first reload onward.
            RegisterCallback<AttachToPanelEvent>(_ => Subscribe());
            RegisterCallback<DetachFromPanelEvent>(_ => Unsubscribe());

            _service = AchievementsService.Instance;
        }

        private void Subscribe()
        {
            if (!_subscribed)
            {
                _service.AchievementUnlocked += OnAchievementUnlocked;
                _subscribed = true;
            }

            // Anything earned while this element was detached is still queued; show it now.
            if (!_showing) ShowNext();
        }

        private void Unsubscribe()
        {
            if (_subscribed)
            {
                _service.AchievementUnlocked -= OnAchievementUnlocked;
                _subscribed = false;
            }

            // Detaching cancels every scheduled item on this element, so an in-flight toast would
            // resume with no timer to advance it. Park the machine instead: the queue survives the
            // reload, the half-shown toast does not.
            CancelPending();
            _showing = false;
            AddToClassList(UgsUiTheme.Achievements.ToastOffscreen);
            Unbind();
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
            int generation = ++_generation;
            Bind(_pending.Dequeue());
            RemoveFromClassList(UgsUiTheme.Achievements.ToastOffscreen);

            // The dwell is timed rather than started by TransitionEndEvent. That event only fires
            // if a transition actually runs, and it does not run for an element that is display:none,
            // never laid out, or whose stylesheet lost the transition rule - in which case the toast
            // used to arrive and stay, taking every queued unlock with it.
            Schedule(() => Retract(generation), DwellMilliseconds);
        }

        private void Retract(int generation)
        {
            if (generation != _generation) return;

            AddToClassList(UgsUiTheme.Achievements.ToastOffscreen);

            // Same reasoning on the way out: if the retract transition never reports an end, advance
            // anyway rather than stranding the queue behind a toast that is already invisible.
            Schedule(() => Advance(generation), RetractWatchdogMilliseconds);
        }

        private void Advance(int generation)
        {
            if (generation != _generation) return;

            // Bump the generation so a watchdog still pending for this toast cannot fire again.
            _generation++;
            CancelPending();
            Unbind();
            _showing = false;
            ShowNext();
        }

        private void OnTransitionEnd(TransitionEndEvent evt)
        {
            // Only the retract leg is interesting; the arrival leg is on its own timer.
            if (_showing && ClassListContains(UgsUiTheme.Achievements.ToastOffscreen))
                Advance(_generation);
        }

        private void Schedule(System.Action action, long delayMilliseconds)
        {
            CancelPending();

            // ExecuteLater returns void, so the item has to be kept from the Execute call itself -
            // it is the only handle that can cancel the timer when the toast is superseded.
            IVisualElementScheduledItem item = schedule.Execute(action);
            item.ExecuteLater(delayMilliseconds);
            _advance = item;
        }

        private void CancelPending()
        {
            _advance?.Pause();
            _advance = null;
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

        /// <summary>
        /// Stop listening for unlocks and park the toast. Detaching does this too; calling it
        /// explicitly is for an element that was created but never attached, which would otherwise
        /// hold a handler on the static service for the life of the process.
        /// </summary>
        public void Dispose() => Unsubscribe();
    }
}
