using CrawfisSoftware.UGS.Events;

using System.Threading.Tasks;

using Unity.Services.Authentication;
using Unity.Services.Core;

using UnityEngine;

using Logger = CrawfisSoftware.Utilities.Logger;
using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS
{
    /// <summary>
    /// Manages player authentication lifecycle with Unity Gaming Services.
    /// Handles initial sign-in with cached credentials, access token expiry recovery,
    /// and authentication state changes through events. 
    /// </summary>
    public class PlayerAuthenticationManager : MonoBehaviour
    {
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        private bool m_IsResumingFromExpiredToken = false;
        
        private const string k_KeyEmoji = "🔑";
        
        public void Awake()
        {
            // SwitchProfile is deferred to EnsureUnityServicesInitialized(): AuthenticationService.Instance
            // throws ServicesInitializationException until UnityServices.InitializeAsync() completes,
            // and this scene can finish loading before that async init does. Every entry into a
            // sign-in flow funnels through SignInCachedPlayerAsync, which is only reached via the
            // CheckForExistingSession event (auto-chained from UnityServicesInitialized) or the
            // UGS_State.IsCheckForExistingSession fallback — both guaranteed post-initialization.
            UGSBus.Subscribe(UGS_EventsEnum.CheckForExistingSession, HandleCheckForExistingSession);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerAuthenticating, HandleSuccessfulSignIn);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerSigningOut, HandleSignedOut);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerSessionExpired, HandleSessionExpired);
            //AuthenticationService.Instance.SignedIn += HandleSuccessfulSignIn;
            //AuthenticationService.Instance.SignedOut += HandleSignedOut;
            //AuthenticationService.Instance.Expired += HandleSessionExpired;
        }

        public void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.CheckForExistingSession, HandleCheckForExistingSession);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerAuthenticating, HandleSuccessfulSignIn);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerSigningOut, HandleSignedOut);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerSessionExpired, HandleSessionExpired);
            //AuthenticationService.Instance.SignedIn -= HandleSuccessfulSignIn;
            //AuthenticationService.Instance.SignedOut -= HandleSignedOut;
            //AuthenticationService.Instance.Expired -= HandleSessionExpired;
        }

        private void Start()
        {
            if (UGS_State.IsCheckForExistingSession) // Missed the event being published.
            {
                // Sign in here automatically from cached session on game start
                SignInCachedPlayerAsync();
            }
        }

        /// <summary>
        /// Note: Assumes the player is already signed in!
        /// Attempts to sign in the player using a cached authentication session, if available.
        /// </summary>
        /// <remarks>If no cached session exists, the sign-in attempt fails and the SignInFailed event is
        /// invoked. If the cached session is invalid or the sign-in fails due to authentication or network errors, the
        /// SignInFailed event is also invoked. This method does not prompt the user for credentials and only succeeds
        /// if a valid cached session is present.</remarks>
        /// <returns>A task that represents the asynchronous sign-in operation.</returns>
        public void SignInCachedPlayerAsync()
        {
            EnsureUnityServicesInitialized();
            if (!AuthenticationService.Instance.SessionTokenExists)
            {
                Logger.LogDemo($"{k_KeyEmoji} No cached session found");
                UGSBus.Publish(UGS_EventsEnum.CheckForExistingSessionFailed, this, null);
                return;
            }
            Logger.Log($"{k_KeyEmoji} Existing player returned");
            Debug.Log($"Returning Player ID: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"Returning Player is Authorized: {AuthenticationService.Instance.IsAuthorized}");

            UGSBus.Publish(UGS_EventsEnum.CheckForExistingSessionSucceeded, this, null);

        }
        
        /// <summary>
        /// Unity Authentication's access tokens are valid for 1 hour and refreshed when necessary.
        /// If the token can't be refreshed (e.g. the player is offline), the token expires.
        /// In this case, when the player goes back online, they need to be signed in again to obtain authorization to call Unity services
        /// </summary>
        public async Task SignInResumeFromExpiredAccessTokenAsync()
        {
            if (!AuthenticationService.Instance.IsExpired)
            {
                Logger.LogWarning("Sign in not required, access token has not expired");
                return;
            }
            
            try
            {
                Logger.Log($"{k_KeyEmoji} Signing in again due to expired access token");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                m_IsResumingFromExpiredToken = true;
            }
            catch (RequestFailedException ex) 
            {
                Logger.LogWarning($"Network error during sign-in: {ex.Message}");
                UGSBus.Publish(UGS_EventsEnum.PlayerSignInFailed, this, null);
            }
        }

        private void HandleCheckForExistingSession(string eventName, object sender, object data)
        {
            SignInCachedPlayerAsync();
        }

        private bool _profileSwitched;

        /// <summary>
        /// Switches to the environment-specific credentials profile before the first sign-in.
        /// Must only run after UnityServices.InitializeAsync() has completed, and SwitchProfile
        /// itself is only legal while signed out — both guarded here.
        /// </summary>
        private void EnsureUnityServicesInitialized()
        {
            if (_profileSwitched || AuthenticationService.Instance.IsSignedIn) return;
            AuthenticationService.Instance.SwitchProfile(UGS_State.UGS_Environment);
            _profileSwitched = true;
        }

        private void HandleSuccessfulSignIn(string eventName, object sender, object data)
        {
            // For simplicity, requires being online
            if (m_IsResumingFromExpiredToken)
            {
                // An event for handling coming online after being offline for a while (e.g. player progress is validated in and saved to cloud)
                UGSBus.Publish(UGS_EventsEnum.PlayerResumedFromExpiredToken, this, UnityEngine.Time.time);
                m_IsResumingFromExpiredToken = false;
                //return;
            }
            // Start the async handling without awaiting it here
            _ = HandleSuccessfulSignInAsync();
        }
        private async Task HandleSuccessfulSignInAsync()
        {
            if(AuthenticationService.Instance.IsSignedIn)
            {
                Logger.LogDemo($"{k_KeyEmoji} Player already signed in");
                UGSBus.Publish(UGS_EventsEnum.PlayerAuthenticated, this, (AuthenticationService.Instance.PlayerName, AuthenticationService.Instance.PlayerId));
                LogPlayerInfo();
                return;
            }
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (AuthenticationService.Instance.IsAuthorized)
                {
                    UGSBus.Publish(UGS_EventsEnum.PlayerAuthenticated, this, (AuthenticationService.Instance.PlayerName, AuthenticationService.Instance.PlayerId));
                    LogPlayerInfo();
                    return;
                }
            }
            catch (AuthenticationException ex)
            {
                Logger.LogWarning($"💡 Authentication failed - if testing, try enabling 'Delete Account On Start' in GameInitializer to reset state {ex.Message}");
                UGSBus.Publish(UGS_EventsEnum.PlayerSignInFailed, this, null);
            }
            catch (RequestFailedException ex)
            {
                Logger.LogWarning($"Network error during sign-in: {ex.Message}");
                UGSBus.Publish(UGS_EventsEnum.PlayerSignInFailed, this, null);
            }
            UGSBus.Publish(UGS_EventsEnum.PlayerSignInFailed, this, null);
        }

        private void HandleSignedOut(string eventName, object sender, object data)
        {
            AuthenticationService.Instance.SignOut(true);
            Logger.LogDemo($"{k_KeyEmoji} Player signed out");
        }

        private void HandleSessionExpired(string eventName, object sender, object data)
        {
            Logger.LogDemo($"{k_KeyEmoji} Session expired! You'll need to sign in again when possible");
        }
        
        private void LogPlayerInfo()
        {
            var playerId = AuthenticationService.Instance.PlayerId;
            var accessToken = AuthenticationService.Instance.AccessToken;
            Logger.Log($"{k_KeyEmoji} Authentication successful!" +
                $"\n{k_KeyEmoji} PlayerID: {playerId}" +
                $"\n{k_KeyEmoji} Token: {accessToken}");
        }
    }
}