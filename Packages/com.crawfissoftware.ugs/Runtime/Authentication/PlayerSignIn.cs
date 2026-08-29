using System;
using System.Threading.Tasks;

using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;

using UnityEngine;
using UnityEngine.UIElements;

using CrawfisSoftware.UGS.UI;

namespace CrawfisSoftware.UGS.Authentication
{
    /// <summary>
    /// The sign-in modal: anonymous, Unity Player Account, or username and password.
    ///    Dependencies: Unity.Services.Authentication, Unity.Services.Authentication.PlayerAccounts
    ///    Subscribes: PlayerAccountService.Instance.SignedIn (SDK callback)
    ///    Publishes: none (PlayerAuthenticationManager owns the UGS events)
    /// </summary>
    /// <remarks>
    /// <para><b>This type is named in UXML by its fully qualified name.</b> A UXML tag is resolved
    /// by string at import time, so renaming this class or its namespace does not fail to compile -
    /// the element simply never instantiates and the modal renders empty. If it moves, every
    /// <c>&lt;CrawfisSoftware.UGS.Authentication.PlayerSignIn/&gt;</c> tag has to move with it.</para>
    /// <para>Each option is hidden unless it is actually usable, rather than shown and then failing:
    /// the Unity Player Account button needs that service configured with a client id, and there is
    /// no value in offering a button that can only produce an error.</para>
    /// </remarks>
    [UxmlElement]
    public partial class PlayerSignIn : VisualElement
    {
        private readonly Button _anonymousButton;
        private readonly Button _unityAccountButton;
        private readonly TextField _username;
        private readonly TextField _password;
        private readonly Button _passwordSignInButton;
        private readonly Button _passwordSignUpButton;
        private readonly Label _error;
        private readonly VisualElement _errorRow;

        private bool _busy;

        public PlayerSignIn()
        {
            AddToClassList(UgsUiTheme.SignIn.Modal);

            var header = new Label("SIGN IN");
            header.AddToClassList(UgsUiTheme.SignIn.HeaderLabel);
            Add(header);

            var options = new VisualElement();
            options.AddToClassList(UgsUiTheme.SignIn.Options);
            Add(options);

            _anonymousButton = MakeButton("Play as Guest", UgsUiTheme.SignIn.OptionAnonymous, SignInAnonymously);
            options.Add(_anonymousButton);

            _unityAccountButton = MakeButton("Unity Player Account", UgsUiTheme.SignIn.OptionUnityPlayerAccount, SignInWithUnityAccount);
            options.Add(_unityAccountButton);

            var separator = new VisualElement();
            separator.AddToClassList(UgsUiTheme.SignIn.Separator);
            options.Add(separator);

            _username = new TextField { label = null };
            _username.AddToClassList(UgsUiTheme.TextField);
            _username.AddToClassList(UgsUiTheme.SignIn.OptionUsernamePassword);
            _username.textEdition.placeholder = "Username";
            options.Add(_username);

            _password = new TextField { label = null, isPasswordField = true };
            _password.AddToClassList(UgsUiTheme.TextField);
            _password.AddToClassList(UgsUiTheme.SignIn.OptionUsernamePassword);
            _password.textEdition.placeholder = "Password";
            options.Add(_password);

            var actions = new VisualElement();
            actions.AddToClassList(UgsUiTheme.SignIn.Actions);
            options.Add(actions);

            _passwordSignInButton = MakeButton("Sign In", null, SignInWithPassword);
            _passwordSignInButton.AddToClassList(UgsUiTheme.ButtonSmall);
            actions.Add(_passwordSignInButton);

            _passwordSignUpButton = MakeButton("Create Account", null, SignUpWithPassword);
            _passwordSignUpButton.AddToClassList(UgsUiTheme.ButtonSmall);
            actions.Add(_passwordSignUpButton);

            _errorRow = new VisualElement();
            _errorRow.AddToClassList(UgsUiTheme.SignIn.ErrorMessage);
            var errorIcon = new VisualElement();
            errorIcon.AddToClassList(UgsUiTheme.SignIn.ErrorIcon);
            _errorRow.Add(errorIcon);
            _error = new Label();
            _error.AddToClassList(UgsUiTheme.Label);
            _errorRow.Add(_error);
            Add(_errorRow);

            var footer = new Label("Guest progress stays on this device until you link an account.");
            footer.AddToClassList(UgsUiTheme.SignIn.Footer);
            Add(footer);

            RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            RegisterCallback<DetachFromPanelEvent>(_ => OnDetach());
        }

        private static Button MakeButton(string text, string extraClass, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList(UgsUiTheme.Button);
            if (extraClass != null) button.AddToClassList(extraClass);
            return button;
        }

        private void OnAttach()
        {
            // Only offer the Unity Player Account route when the service is actually configured;
            // without a client id it can only ever fail.
            bool playerAccountsAvailable = false;
            try
            {
                playerAccountsAvailable = PlayerAccountService.Instance != null;
            }
            catch (Exception)
            {
                playerAccountsAvailable = false;
            }
            _unityAccountButton.style.display = playerAccountsAvailable ? DisplayStyle.Flex : DisplayStyle.None;

            if (playerAccountsAvailable)
                PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;
        }

        private void OnDetach()
        {
            try
            {
                if (PlayerAccountService.Instance != null)
                    PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;
            }
            catch (Exception)
            {
                // Service was never available; nothing to detach from.
            }
        }

        private async void OnPlayerAccountSignedIn()
        {
            await RunAsync(async () =>
                await AuthenticationService.Instance.SignInWithUnityAsync(
                    PlayerAccountService.Instance.AccessToken));
        }

        private async void SignInAnonymously() =>
            await RunAsync(async () => await AuthenticationService.Instance.SignInAnonymouslyAsync());

        private async void SignInWithUnityAccount() =>
            await RunAsync(async () => await PlayerAccountService.Instance.StartSignInAsync());

        private async void SignInWithPassword() =>
            await RunAsync(async () => await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(
                _username.value, _password.value));

        private async void SignUpWithPassword() =>
            await RunAsync(async () => await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(
                _username.value, _password.value));

        /// <summary>
        /// Run one sign-in attempt with the buttons disabled, surfacing any failure in the modal
        /// rather than only in the console - a player who cannot see why sign-in failed will just
        /// press the button again.
        /// </summary>
        private async Task RunAsync(Func<Task> attempt)
        {
            if (_busy) return;
            SetBusy(true);
            ShowError(null);
            try
            {
                await attempt();
            }
            catch (AuthenticationException e)
            {
                ShowError(e.Message);
            }
            catch (RequestFailedException e)
            {
                ShowError(e.Message);
            }
            catch (Exception e)
            {
                ShowError("Sign-in failed. Check the connection and try again.");
                Debug.LogWarning($"{nameof(PlayerSignIn)}: {e}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _anonymousButton.SetEnabled(!busy);
            _unityAccountButton.SetEnabled(!busy);
            _passwordSignInButton.SetEnabled(!busy);
            _passwordSignUpButton.SetEnabled(!busy);
        }

        private void ShowError(string message)
        {
            _error.text = message ?? string.Empty;
            _errorRow.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
