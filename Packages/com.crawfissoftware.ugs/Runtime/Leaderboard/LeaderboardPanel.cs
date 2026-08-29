using System;
using System.Threading;
using System.Threading.Tasks;

using CrawfisSoftware.UGS.Events;
using CrawfisSoftware.UGS.Leaderboard.UI;

using UnityEngine;
using UnityEngine.UIElements;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Leaderboard
{
    /// <summary>
    /// Drag-and-drop host for the leaderboard display.
    ///    Dependencies: PanelRenderer, LeaderboardQuery, LeaderboardView
    ///    Subscribes: UGS_EventsEnum.LeaderboardOpened, UGS_EventsEnum.ScoreUpdated,
    ///                UGS_EventsEnum.LeaderboardClosing
    ///    Publishes: none - LeaderboardController owns the open/close flow
    /// </summary>
    /// <remarks>
    /// <para>Follows the PanelRenderer pattern already used by <c>AchievementsPrefab</c>: the
    /// visual tree arrives only through the UIReload callback, a reload rebuilds it, so attaching
    /// has to be idempotent and repeated on every callback. The renderer is never disabled -
    /// toggling <c>enabled</c> trips Unity bug UUM-146174 and leaves the panel blank.</para>
    /// <para><b>Why it refreshes when it does.</b> Reading on Start alone shows a list assembled
    /// before the player's just-finished run was submitted, which is the one moment they most want
    /// to see. So it reads again on <c>LeaderboardOpened</c> (published after the additive scene
    /// load) and again on <c>ScoreUpdated</c>. Those three paths overlap, so each read carries a
    /// generation number and a completion from a superseded read is discarded rather than allowed
    /// to overwrite a newer one.</para>
    /// <para>Styling ships as serialized <see cref="StyleSheet"/> references added straight to the
    /// panel root, not as a theme <c>@import</c> chain. The panel then styles correctly whatever
    /// PanelSettings the host scene happens to use, and a mis-resolved import path cannot silently
    /// blank it.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LeaderboardPanel : MonoBehaviour
    {
        private const string UnavailableMessage = "Sign in to see the leaderboard.";

        [Tooltip("The PanelRenderer whose root this panel attaches to.")]
        [SerializeField] private PanelRenderer _panel;

        [Tooltip("Stylesheets added to the panel root. Normally UgsCore.uss then UgsComponents.uss.")]
        [SerializeField] private StyleSheet[] _styleSheets;

        [Tooltip("Leaderboard id to read. Overridden by the id carried on LeaderboardOpened.")]
        [SerializeField] private string _leaderboardId = "DailyDistance";

        [Tooltip("Optional tier. Empty reads the global board.")]
        [SerializeField] private string _tierId = "";

        [Tooltip("Heading above the card.")]
        [SerializeField] private string _title = "LEADERBOARD";

        [SerializeField] private int _numberToDisplay = 25;
        [SerializeField] private int _playerRangeLimit = 5;

        [Tooltip("Offer a second tab showing the entries around the signed-in player.")]
        [SerializeField] private bool _showPlayerTab = true;

        [Tooltip("Read once on Start, before any event arrives.")]
        [SerializeField] private bool _refreshOnStart = true;

        [Tooltip("Numeric format for the score column. N0 is a whole number with separators.")]
        [SerializeField] private string _scoreFormat = "N0";

        private LeaderboardView _view;
        private VisualElement _root;
        private CancellationTokenSource _cts;
        private int _generation;
        private bool _destroyed;

        /// <summary>
        /// The board being read. Setting this does not start a read - call <see cref="Refresh"/>.
        /// </summary>
        public string LeaderboardId
        {
            get => _leaderboardId;
            set => _leaderboardId = value;
        }

        /// <summary>The tier being read, or empty for the global board. Does not start a read.</summary>
        public string TierId
        {
            get => _tierId;
            set => _tierId = value;
        }

        private void Awake()
        {
            _view = new LeaderboardView(_scoreFormat)
            {
                PlayerTabVisible = _showPlayerTab,
            };
            _view.SetTitle(_title);

            UGSBus.Subscribe(UGS_EventsEnum.LeaderboardOpened, OnLeaderboardOpened);
            UGSBus.Subscribe(UGS_EventsEnum.ScoreUpdated, OnScoreUpdated);
            UGSBus.Subscribe(UGS_EventsEnum.LeaderboardClosing, OnLeaderboardClosing);
        }

        private void OnEnable()
        {
            if (_panel == null) return;
            _panel.RegisterUIReloadCallback(OnUIReload);
            _panel.enabled = true;
        }

        private void OnDisable()
        {
            if (_panel != null)
                _panel.UnregisterUIReloadCallback(OnUIReload);
        }

        private void Start()
        {
            if (_refreshOnStart) Refresh();
        }

        private void OnDestroy()
        {
            _destroyed = true;

            UGSBus.Unsubscribe(UGS_EventsEnum.LeaderboardOpened, OnLeaderboardOpened);
            UGSBus.Unsubscribe(UGS_EventsEnum.ScoreUpdated, OnScoreUpdated);
            UGSBus.Unsubscribe(UGS_EventsEnum.LeaderboardClosing, OnLeaderboardClosing);

            CancelInFlight();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// Read both tabs again. Safe to call repeatedly - a read already in flight is superseded
        /// rather than cancelled, so the newest call always wins.
        /// </summary>
        public void Refresh()
        {
            if (_destroyed || _view == null) return;

            if (_cts == null || _cts.IsCancellationRequested)
            {
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
            }

            _ = RunRefreshAsync(++_generation, _cts.Token);
        }

        // The PanelRenderer surfaces its tree only here, and a reload rebuilds it - so the
        // stylesheets and the view are re-applied on every callback, not just the first.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            ApplyStyleSheets();
            AttachView();
        }

        private void ApplyStyleSheets()
        {
            if (_root == null || _styleSheets == null) return;

            foreach (StyleSheet sheet in _styleSheets)
            {
                if (sheet != null && !_root.styleSheets.Contains(sheet))
                    _root.styleSheets.Add(sheet);
            }
        }

        private void AttachView()
        {
            if (_root == null || _view == null) return;
            if (_view.parent == _root) return;

            _root.Add(_view);
        }

        private void OnLeaderboardOpened(string eventName, object sender, object data)
        {
            // The controller publishes the id it opened with. Adopting it keeps one source of
            // truth: a panel configured for a different board would otherwise show the wrong
            // scores under the right heading, with nothing in the console to say so.
            if (data is string id && !string.IsNullOrEmpty(id)) _leaderboardId = id;

            Refresh();
        }

        private void OnScoreUpdated(string eventName, object sender, object data) => Refresh();

        private void OnLeaderboardClosing(string eventName, object sender, object data) => CancelInFlight();

        private void CancelInFlight()
        {
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already torn down; nothing to cancel.
            }
        }

        private async Task RunRefreshAsync(int generation, CancellationToken token)
        {
            try
            {
                await RefreshAsync(generation, token);
            }
            catch (OperationCanceledException)
            {
                // Expected whenever the panel closes mid-read.
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(LeaderboardPanel)}: refresh of '{_leaderboardId}' failed. {e}");
            }
        }

        private async Task RefreshAsync(int generation, CancellationToken token)
        {
            if (!LeaderboardQuery.IsAvailable)
            {
                if (IsCurrent(generation))
                {
                    _view.CurrentPlayerId = null;
                    _view.ShowMessage(UnavailableMessage);
                }
                return;
            }

            _view.CurrentPlayerId = LeaderboardQuery.CurrentPlayerId;
            _view.ShowBusy();

            var global = string.IsNullOrEmpty(_tierId)
                ? await LeaderboardQuery.GetTopScoresAsync(_leaderboardId, _numberToDisplay, token)
                : await LeaderboardQuery.GetTierScoresAsync(_leaderboardId, _tierId, _numberToDisplay, token);

            if (!IsCurrent(generation)) return;
            _view.ShowGlobal(global);

            if (!_showPlayerTab) return;

            var around = await LeaderboardQuery.GetPlayerRangeAsync(_leaderboardId, _playerRangeLimit, token);

            if (!IsCurrent(generation)) return;
            _view.ShowPlayerRange(around);
        }

        // A completion is only allowed to touch the tree if it is still the newest read AND the
        // component is still alive - an await that resumes after the scene unloaded would
        // otherwise write into VisualElements belonging to a destroyed panel.
        private bool IsCurrent(int generation) => !_destroyed && _view != null && generation == _generation;
    }
}
