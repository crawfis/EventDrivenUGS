using System.Globalization;

using CrawfisSoftware.UGS.UI;

using Unity.Services.Leaderboards.Models;

using UnityEngine.UIElements;

namespace CrawfisSoftware.UGS.Leaderboard.UI
{
    /// <summary>
    /// One leaderboard row: rank, player name, score.
    ///    Dependencies: Unity.Services.Leaderboards (LeaderboardEntry), UgsUiTheme
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Rows are recycled by the owning <see cref="ListView"/>, so every field the row can
    /// show has to be written on each <see cref="Bind"/> - including the current-player class,
    /// which must be removed as well as added or a highlight sticks to whatever row inherits the
    /// recycled element.</para>
    /// <para>The current-player test compares <see cref="LeaderboardEntry.PlayerId"/>, not the
    /// display name: names are not unique, and two players sharing one would both light up.</para>
    /// </remarks>
    internal sealed class LeaderboardRow : VisualElement
    {
        /// <summary>
        /// Row height in pixels. The <see cref="ListView"/> is fixed-height virtualised, so this
        /// value and the <c>height</c> in <c>.ugs-leaderboard-row</c> have to agree: a mismatch
        /// leaves rows overlapping or gapped rather than failing outright.
        /// </summary>
        public const float Height = 32f;

        private readonly Label _rank = new Label();
        private readonly Label _name = new Label();
        private readonly Label _score = new Label();
        private readonly string _scoreFormat;

        public LeaderboardRow(string scoreFormat)
        {
            _scoreFormat = string.IsNullOrEmpty(scoreFormat) ? "N0" : scoreFormat;

            AddToClassList(UgsUiTheme.Leaderboards.Row);
            _rank.AddToClassList(UgsUiTheme.Leaderboards.RowRank);
            _name.AddToClassList(UgsUiTheme.Leaderboards.RowName);
            _score.AddToClassList(UgsUiTheme.Leaderboards.RowScore);

            Add(_rank);
            Add(_name);
            Add(_score);
        }

        /// <summary>Fill the row from <paramref name="entry"/>.</summary>
        /// <param name="entry">The entry to display.</param>
        /// <param name="currentPlayerId">
        /// The signed-in player's id, or null when nobody is signed in - in which case no row is
        /// highlighted, which is the correct outcome rather than a case to guard against.
        /// </param>
        public void Bind(LeaderboardEntry entry, string currentPlayerId)
        {
            if (entry == null)
            {
                Unbind();
                return;
            }

            // Rank is 0-based on the wire and 1-based to a human.
            _rank.text = $"#{entry.Rank + 1}";
            _name.text = string.IsNullOrEmpty(entry.PlayerName) ? "Anonymous" : entry.PlayerName;

            // InvariantCulture: a leaderboard is shared, so a score must not read differently
            // depending on the device's decimal separator.
            _score.text = entry.Score.ToString(_scoreFormat, CultureInfo.InvariantCulture);

            bool isCurrentPlayer = !string.IsNullOrEmpty(currentPlayerId) && entry.PlayerId == currentPlayerId;
            EnableInClassList(UgsUiTheme.Leaderboards.RowCurrentPlayer, isCurrentPlayer);
        }

        /// <summary>Clear the row before it is recycled.</summary>
        public void Unbind()
        {
            _rank.text = string.Empty;
            _name.text = string.Empty;
            _score.text = string.Empty;
            EnableInClassList(UgsUiTheme.Leaderboards.RowCurrentPlayer, false);
        }
    }
}
