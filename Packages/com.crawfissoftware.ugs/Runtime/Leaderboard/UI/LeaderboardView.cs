using System.Collections.Generic;

using CrawfisSoftware.UGS.UI;

using Unity.Services.Leaderboards.Models;

using UnityEngine.UIElements;

namespace CrawfisSoftware.UGS.Leaderboard.UI
{
    /// <summary>
    /// The leaderboard's whole visual tree: a title, and a card holding a tab bar over two lists.
    ///    Dependencies: LeaderboardList, UgsUiTheme
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>A pure view. It reads no service, owns no leaderboard id and starts no fetch - it is
    /// handed entries and shows them. That is what lets <see cref="LeaderboardPanel"/> keep every
    /// decision about *when* to read in one place.</para>
    /// <para>The tabs are two <see cref="Toggle"/>s rather than a <see cref="TabView"/>. The theme
    /// styles <c>.ugs-tab-button .unity-toggle__checkmark</c>, which is a Toggle's part name, so a
    /// TabView would silently lose the tab styling; and two mutually exclusive toggles are less
    /// machinery than a TabView whose default chrome we would then have to override.</para>
    /// <para>Not registered as a <c>[UxmlElement]</c>: nothing instantiates a leaderboard from
    /// UXML, and registering it would create another type name resolved by string at import - the
    /// hazard the sign-in modal already has to carry.</para>
    /// </remarks>
    internal sealed class LeaderboardView : VisualElement
    {
        private const string GlobalTabText = "TOP";
        private const string PlayerTabText = "YOU";

        private readonly Label _title = new Label();
        private readonly VisualElement _tabBar = new VisualElement();
        private readonly Toggle _globalTab = new Toggle { text = GlobalTabText };
        private readonly Toggle _playerTab = new Toggle { text = PlayerTabText };
        private readonly LeaderboardList _global;
        private readonly LeaderboardList _player;

        private bool _playerTabVisible = true;
        private string _currentPlayerId;

        public LeaderboardView(string scoreFormat)
        {
            _global = new LeaderboardList(scoreFormat);
            _player = new LeaderboardList(scoreFormat);

            AddToClassList(UgsUiTheme.Leaderboards.Root);
            _title.AddToClassList(UgsUiTheme.Leaderboards.Title);
            _tabBar.AddToClassList(UgsUiTheme.Leaderboards.TabBar);
            _globalTab.AddToClassList(UgsUiTheme.Leaderboards.TabButton);
            _playerTab.AddToClassList(UgsUiTheme.Leaderboards.TabButton);

            // SetValueWithoutNotify on the sibling: assigning value would re-enter this callback
            // through the other toggle and fight over which tab is selected.
            _globalTab.RegisterValueChangedCallback(_ => SelectTab(showPlayer: false));
            _playerTab.RegisterValueChangedCallback(_ => SelectTab(showPlayer: true));

            var card = new VisualElement();
            card.AddToClassList(UgsUiTheme.Leaderboards.Card);

            _tabBar.Add(_globalTab);
            _tabBar.Add(_playerTab);
            card.Add(_tabBar);
            card.Add(_global);
            card.Add(_player);

            Add(_title);
            Add(card);

            SelectTab(showPlayer: false);
        }

        /// <summary>
        /// The signed-in player's id, forwarded to both lists for the row highlight. Null disables
        /// it, which is what a signed-out player should see.
        /// </summary>
        public string CurrentPlayerId
        {
            get => _currentPlayerId;
            set
            {
                _currentPlayerId = value;
                _global.CurrentPlayerId = value;
                _player.CurrentPlayerId = value;
            }
        }

        /// <summary>
        /// Whether the player's own neighbourhood is offered as a second tab. Turning it off while
        /// that tab is selected falls back to the global tab, so the card cannot end up showing a
        /// list the player can no longer reach.
        /// </summary>
        public bool PlayerTabVisible
        {
            get => _playerTabVisible;
            set
            {
                _playerTabVisible = value;
                _playerTab.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                if (!value) SelectTab(showPlayer: false);
            }
        }

        public void SetTitle(string title) => _title.text = title ?? string.Empty;

        public void ShowGlobal(IReadOnlyList<LeaderboardEntry> entries) => _global.SetEntries(entries);

        public void ShowPlayerRange(IReadOnlyList<LeaderboardEntry> entries) => _player.SetEntries(entries);

        /// <summary>Put both lists into their loading state.</summary>
        public void ShowBusy() => ShowMessage(LeaderboardList.BusyMessage);

        /// <summary>Replace both lists with <paramref name="message"/> - offline, or signed out.</summary>
        public void ShowMessage(string message)
        {
            _global.SetMessage(message);
            _player.SetMessage(message);
        }

        private void SelectTab(bool showPlayer)
        {
            if (showPlayer && !_playerTabVisible) showPlayer = false;

            _globalTab.SetValueWithoutNotify(!showPlayer);
            _playerTab.SetValueWithoutNotify(showPlayer);

            _global.style.display = showPlayer ? DisplayStyle.None : DisplayStyle.Flex;
            _player.style.display = showPlayer ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
