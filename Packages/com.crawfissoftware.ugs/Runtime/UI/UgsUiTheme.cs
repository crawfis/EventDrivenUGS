namespace CrawfisSoftware.UGS.UI
{
    /// <summary>
    /// Every USS class name the UGS UI elements apply, in one place.
    ///    Dependencies: none (pure constants)
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Mirrors the rules in <c>Runtime/UI/Theme/UgsCore.uss</c>, <c>UgsControls.uss</c>,
    /// <c>UgsComponents.uss</c> and <c>UgsSignIn.uss</c>. A constant here without a matching rule
    /// there styles nothing and fails silently, so the two are edited together.</para>
    /// <para>Names are <c>ugs-</c> prefixed so they cannot collide with a host game's own
    /// stylesheets - a UGS panel and a game panel can share a screen.</para>
    /// </remarks>
    public static class UgsUiTheme
    {
        // ---------- HARD RUNTIME CONTRACT ----------

        /// <summary>
        /// Collapses an element. Must stay exactly "hidden": PlayerSignInController declares the
        /// same literal independently and toggles it to show and hide the sign-in modal. Renaming
        /// it here alone hides nothing, with no compile error.
        /// </summary>
        public const string Hidden = "hidden";

        // ---------- primitives: UgsControls.uss ----------

        public const string Modal = "ugs-modal";
        public const string Label = "ugs-label";
        public const string Header = "ugs-header";
        public const string HeaderSmall = "ugs-header--sm";
        public const string SpaceBottom = "ugs-space-bottom";
        public const string Button = "ugs-button";
        public const string ButtonSmall = "ugs-button--sm";
        public const string ButtonExtraSmall = "ugs-button--xs";
        public const string TextField = "ugs-textfield";
        public const string ScrollView = "ugs-scroll-view";
        public const string ProgressBar = "ugs-progress-bar";

        /// <summary>Achievement card and toast classes. See UgsComponents.uss.</summary>
        public static class Achievements
        {
            public const string Base = "ugs-achievement";
            public const string Card = "ugs-achievement-card";
            public const string CardTitle = "ugs-achievement-card__title";
            public const string CardIcon = "ugs-achievement-card__icon";
            public const string CardDescription = "ugs-achievement-card__description";
            public const string CardUnlockedLabel = "ugs-achievement-card__unlocked";
            public const string CardProgress = "ugs-achievement-card__progress";
            public const string CardProgressHeader = "ugs-achievement-card__progress-header";
            public const string Grid = "ugs-achievement-grid";
            public const string Toast = "ugs-achievement-toast";

            /// <summary>
            /// Carries the transition declaration. The toast's show/hide state machine advances on
            /// TransitionEndEvent, so if this class has no <c>transition-property</c> rule the event
            /// never fires and a shown toast never retracts.
            /// </summary>
            public const string ToastAnimated = "ugs-achievement-toast--animated";

            public const string ToastOffscreen = "ugs-achievement-toast--offscreen";
        }

        /// <summary>Leaderboard panel, tab bar and row classes. See UgsComponents.uss.</summary>
        public static class Leaderboards
        {
            /// <summary>The whole leaderboard element: title plus card.</summary>
            public const string Root = "ugs-leaderboard";

            public const string Title = "ugs-leaderboard__title";
            public const string Card = "ugs-leaderboard__card";

            /// <summary>One list: the scrolling view plus the message shown in its place.</summary>
            public const string List = "ugs-leaderboard-list";

            public const string ListView = "ugs-leaderboard-list__view";
            public const string ListMessage = "ugs-leaderboard-list__message";

            public const string TabBar = "ugs-tab-bar";
            public const string TabButton = "ugs-tab-button";
            public const string TabButtonUnderline = "ugs-tab-button__underline";
            public const string Row = "ugs-leaderboard-row";
            public const string RowCurrentPlayer = "ugs-leaderboard-row--current";
            public const string RowRank = "ugs-leaderboard-row__rank";
            public const string RowName = "ugs-leaderboard-row__name";
            public const string RowScore = "ugs-leaderboard-row__score";
        }

        /// <summary>
        /// Sign-in modal classes. These live in UgsSignIn.uss, which the sign-in UXML loads by
        /// GUID - not through the runtime theme, so the modal styles even in a panel using a
        /// different theme.
        /// </summary>
        public static class SignIn
        {
            public const string Container = "ugs-signin-container";
            public const string Modal = "ugs-signin-modal";
            public const string Options = "ugs-signin-options";
            public const string HeaderLabel = "ugs-signin-header";
            public const string Separator = "ugs-signin-separator";
            public const string Actions = "ugs-signin-actions";
            public const string Footer = "ugs-signin-footer";
            public const string ErrorMessage = "ugs-signin-error";
            public const string ErrorIcon = "ugs-error-icon";

            // Marker classes: applied so a host game can target a specific sign-in option from its
            // own stylesheet, but deliberately carrying no rule of our own.
            public const string OptionAnonymous = "ugs-signin-option--anonymous";
            public const string OptionUnityPlayerAccount = "ugs-signin-option--unity";
            public const string OptionUsernamePassword = "ugs-signin-option--password";
        }
    }
}
