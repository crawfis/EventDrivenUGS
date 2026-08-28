using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using CrawfisSoftware.UGS.UI;

namespace CrawfisSoftware.UGS.Achievements.UI
{
    /// <summary>
    /// The achievements panel: a scrolling grid of <see cref="AchievementCard"/>, one per known
    /// achievement.
    ///    Dependencies: AchievementsService, AchievementIconLibrary, UgsUiTheme
    ///    Subscribes: AchievementCatalog.Changed (plain C# event)
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Constructing this element asks the service to load, so a host only has to place it.
    /// The load is fire-and-forget by design: a panel that has not received its data yet shows an
    /// empty grid and fills in when the catalogue arrives.</para>
    /// <para>Cards are rebuilt wholesale when the catalogue changes, which happens once per load.
    /// Per-record updates never come through here - each card follows its own record.</para>
    /// </remarks>
    public sealed class AchievementsContainerElement : VisualElement
    {
        private readonly VisualElement _grid;
        private readonly List<AchievementCard> _cards = new List<AchievementCard>();
        private readonly AchievementsService _service;

        /// <summary>Whether this panel was built against the server-authoritative backend.</summary>
        public bool UseTrustedClient { get; }

        /// <summary>
        /// Build the panel.
        /// </summary>
        /// <param name="useTrustedClient">
        /// True to route through the Cloud Code module (server-authoritative), false to read and
        /// write the player's Cloud Save directly.
        /// </param>
        /// <param name="isDevelopmentMode">
        /// Adds a reset control, so a developer can clear their own progress without editing
        /// player data by hand.
        /// </param>
        /// <param name="icons">
        /// Icons to register before the first draw. Merged with any already registered.
        /// </param>
        public AchievementsContainerElement(bool useTrustedClient, bool isDevelopmentMode = false,
                                            IEnumerable<Texture2D> icons = null)
        {
            UseTrustedClient = useTrustedClient;
            AchievementIconLibrary.Register(icons);

            _service = AchievementsService.Instance;
            _service.UseTrustedClient = useTrustedClient;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList(UgsUiTheme.ScrollView);
            Add(scroll);

            _grid = new VisualElement();
            _grid.AddToClassList(UgsUiTheme.Achievements.Grid);
            scroll.Add(_grid);

            if (isDevelopmentMode)
                Add(BuildDevelopmentControls());

            _service.Catalog.Changed += OnCatalogChanged;
            RegisterCallback<DetachFromPanelEvent>(_ => Dispose());

            OnCatalogChanged();
            _service.LoadAsync();
        }

        private VisualElement BuildDevelopmentControls()
        {
            var row = new VisualElement();
            row.AddToClassList(UgsUiTheme.Achievements.CardProgressHeader);

            var reset = new Button(() => _service.ResetAll()) { text = "Reset" };
            reset.AddToClassList(UgsUiTheme.Button);
            reset.AddToClassList(UgsUiTheme.ButtonSmall);
            row.Add(reset);

            var refresh = new Button(() => _service.LoadAsync()) { text = "Refresh" };
            refresh.AddToClassList(UgsUiTheme.Button);
            refresh.AddToClassList(UgsUiTheme.ButtonSmall);
            row.Add(refresh);

            return row;
        }

        private void OnCatalogChanged()
        {
            foreach (var card in _cards) card.Unbind();
            _cards.Clear();
            _grid.Clear();

            foreach (var achievement in _service.Catalog.Achievements)
            {
                var card = new AchievementCard(achievement);
                _cards.Add(card);
                _grid.Add(card);
            }
        }

        /// <summary>
        /// Drop every subscription this element holds. Called automatically when the element leaves
        /// its panel; safe to call again.
        /// </summary>
        public void Dispose()
        {
            _service.Catalog.Changed -= OnCatalogChanged;
            foreach (var card in _cards) card.Unbind();
        }
    }
}
