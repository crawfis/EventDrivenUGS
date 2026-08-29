using System.Collections.Generic;

using CrawfisSoftware.UGS.UI;

using Unity.Services.Leaderboards.Models;

using UnityEngine.UIElements;

namespace CrawfisSoftware.UGS.Leaderboard.UI
{
    /// <summary>
    /// A scrollable list of leaderboard rows, plus the message shown when there is nothing to list.
    ///    Dependencies: Unity.Services.Leaderboards (LeaderboardEntry), LeaderboardRow, UgsUiTheme
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>The empty/busy/error message is <b>our own</b> <see cref="Label"/>, toggled against
    /// the list with <c>style.display</c>. The stack this replaces reached into the built-in
    /// <see cref="ListView"/> and re-used Unity's internal empty-state label, which is a private
    /// implementation detail that can move between editor versions without notice.</para>
    /// <para>Entries are copied into a <see cref="List{T}"/> because <see cref="ListView"/> binds
    /// to an <see cref="System.Collections.IList"/>, and the SDK hands back a read-only view. The
    /// copy is per refresh and bounded by the display limit, so it is not worth avoiding.</para>
    /// </remarks>
    internal sealed class LeaderboardList : VisualElement
    {
        /// <summary>Shown when a fetch is in flight.</summary>
        public const string BusyMessage = "Loading scores...";

        /// <summary>Shown when a fetch succeeded and returned nothing.</summary>
        public const string EmptyMessage = "No scores yet.";

        private readonly ListView _listView;
        private readonly Label _message = new Label();
        private readonly List<LeaderboardEntry> _entries = new List<LeaderboardEntry>();

        public LeaderboardList(string scoreFormat)
        {
            AddToClassList(UgsUiTheme.Leaderboards.List);

            _listView = new ListView
            {
                itemsSource = _entries,
                fixedItemHeight = LeaderboardRow.Height,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.None,
                showBorder = false,
                makeItem = () => new LeaderboardRow(scoreFormat),
                bindItem = (element, index) => ((LeaderboardRow)element).Bind(_entries[index], CurrentPlayerId),
                unbindItem = (element, _) => ((LeaderboardRow)element).Unbind(),
            };
            _listView.AddToClassList(UgsUiTheme.Leaderboards.ListView);

            _message.AddToClassList(UgsUiTheme.Leaderboards.ListMessage);

            Add(_listView);
            Add(_message);

            SetMessage(BusyMessage);
        }

        /// <summary>
        /// The signed-in player's id, forwarded to every row so it can highlight itself. Null
        /// disables the highlight.
        /// </summary>
        public string CurrentPlayerId { get; set; }

        /// <summary>
        /// Show <paramref name="entries"/>. A null or empty list shows <see cref="EmptyMessage"/>
        /// instead, so a successful fetch with no scores does not read as a failed one.
        /// </summary>
        public void SetEntries(IReadOnlyList<LeaderboardEntry> entries)
        {
            _entries.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null) _entries.Add(entries[i]);
                }
            }

            if (_entries.Count == 0)
            {
                SetMessage(EmptyMessage);
                return;
            }

            ShowList(true);

            // Rebuild, not RefreshItems: the item count changed, and RefreshItems only re-binds the
            // rows the view already built.
            _listView.Rebuild();
        }

        /// <summary>Hide the list and show <paramref name="message"/> in its place.</summary>
        public void SetMessage(string message)
        {
            _message.text = message ?? string.Empty;
            ShowList(false);
        }

        private void ShowList(bool listVisible)
        {
            _listView.style.display = listVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _message.style.display = listVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
