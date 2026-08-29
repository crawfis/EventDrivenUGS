using System;
using System.Threading.Tasks;

using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;

using UnityEngine;
using UnityEngine.UIElements;

using CrawfisSoftware.UGS.Events;
using CrawfisSoftware.UGS.UI;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Authentication
{
    /// <summary>
    /// The sign-in modal: anonymous, Unity Player Account, or username and password.
    ///    Dependencies: Unity.Services.Authentication, Unity.Services.Authentication.PlayerAccounts
    ///    Subscribes: PlayerAccountService.Instance.SignedIn, .SignInFailed (SDK callbacks),
    ///                UGS_EventsEnum.UnityServicesInitialized
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
            RefreshPlayerAccountAvailability();
            UGSBus.Subscribe(UGS_EventsEnum.UnityServicesInitialized, OnUnityServicesInitialized);
        }

        private void OnDetach()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.UnityServicesInitialized, OnUnityServicesInitialized);
            try
            {
                var service = PlayerAccountService.Instance;
                service.SignedIn -= OnPlayerAccountSignedIn;
                service.SignInFailed -= OnPlayerAccountSignInFailed;
            }
            catch (Exception)
            {
                // Service was never available; nothing to detach from.
            }
        }

        private void OnUnityServicesInitialized(string eventName, object sender, object data) =>
            RefreshPlayerAccountAvailability();

        /// <summary>
        /// Decides whether the Unity Player Account button is usable, and hooks the service's
        /// callbacks once it is.
        /// </summary>
        /// <remarks>
        /// <para>Only offer the Unity Player Account route when the service is actually configured;
        /// without a client id it can only ever fail.</para>
        /// <para>This cannot be decided once at attach. PlayerAccountService.Instance is assigned by a
        /// package initializer that runs inside UnityServices.InitializeAsync(), and this element can
        /// attach while that is still in flight - probing then and never again hides the button for the
        /// life of the panel. Re-running on UnityServicesInitialized covers the cold start; the probe
        /// is idempotent so the two paths cannot double-register the handlers.</para>
        /// <para>The SDK getter throws rather than returning null until the service is registered, so
        /// the try/catch is what decides availability - a null test against the property alone can
        /// never evaluate false.</para>
        /// </remarks>
        private void RefreshPlayerAccountAvailability()
        {
            IPlayerAccountService service = null;
            try
            {
                service = PlayerAccountService.Instance;
            }
            catch (Exception)
            {
                service = null;
            }

            _unityAccountButton.style.display = service != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (service == null) return;

            service.SignedIn -= OnPlayerAccountSignedIn;
            service.SignedIn += OnPlayerAccountSignedIn;
            service.SignInFailed -= OnPlayerAccountSignInFailed;
            service.SignInFailed += OnPlayerAccountSignInFailed;
        }

        private async void OnPlayerAccountSignedIn()
        {
            await RunAsync(async () =>
                await AuthenticationService.Instance.SignInWithUnityAsync(
                    PlayerAccountService.Instance.AccessToken));
        }

        /// <summary>
        /// Surfaces a Player Account sign-in failure that the awaited call cannot report.
        /// </summary>
        /// <remarks>
        /// StartSignInAsync hands the authorization code to a token exchange that the SDK does not
        /// await, so a failure there reaches neither RunAsync's catch blocks nor the console in a form
        /// the player can see - the modal would just go idle with no error and no sign-in. The busy
        /// state is deliberately left alone here: RunAsync's finally has already cleared it by the time
        /// the exchange completes, and clearing it again could re-enable the buttons underneath a
        /// different attempt that is still in flight.
        /// </remarks>
        private void OnPlayerAccountSignInFailed(RequestFailedException exception)
        {
            ShowError(exception.Message);
        }

        private async void SignInAnonymously() =>
            await RunAsync(async () => await AuthenticationService.Instance.SignInAnonymouslyAsync());

        /// <summary>
        /// Signs in through the Unity Player Account, reusing an existing Player Account session when
        /// there is one.
        /// </summary>
        /// <remarks>
        /// The two sessions come apart routinely: PlayerAuthenticationManager signs out of UGS only
        /// and nothing signs out of Player Accounts, and a failed SignInWithUnityAsync leaves the
        /// Player Account authorized on its own. StartSignInAsync throws "Player is already signed in."
        /// in that state, so without this branch the button dead-ends for the rest of the process,
        /// telling a player who is demonstrably not signed in that they already are. Mirrors the SDK's
        /// own UnityPlayerAccountsUIExample. The token exchange cannot be delegated to
        /// OnPlayerAccountSignedIn because that goes back through RunAsync, which is already busy here.
        /// </remarks>
        private async void SignInWithUnityAccount() =>
            await RunAsync(async () =>
            {
                if (PlayerAccountService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInWithUnityAsync(
                        PlayerAccountService.Instance.AccessToken);
                    return;
                }
                await PlayerAccountService.Instance.StartSignInAsync();
            });

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
