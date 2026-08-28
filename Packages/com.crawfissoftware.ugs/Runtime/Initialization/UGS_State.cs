using CrawfisSoftware.UGS.Events;

using Unity.Services.Core;
using Unity.Services.Core.Components;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS
{
    internal class UGS_State : MonoBehaviour
    {
        [SerializeField] private ServicesInitialization _uGS_Services;

        public static UGS_State Instance { get; private set; }

        public static string UGS_Environment => Instance._uGS_Services?.EnvironmentName;

        // Keep track of potentially missed events in scenes that load after UGS initialization
        public static bool IsUnityServicesInitialized { get; private set; } = false;
        public static bool IsCheckForExistingSession { get; private set; } = false;
        public static bool IsPlayerSigningIn { get; private set; } = false;
        public static bool IsPlayerSignedIn { get; private set; } = false;
        public static bool IsPlayerAuthenticated { get; private set; } = false;
        public static bool IsRemoteConfigFetching { get; private set; } = false;
        public static bool IsRemoteConfigUpdated { get; private set; } = false;
        public static bool IsGameReady { get; private set; } = false;

        private void Awake()
        {
            if(Instance != null)
            {
                DestroyImmediate(Instance);
            }
            Instance = this;
            Reset();
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                IsUnityServicesInitialized = true;
            }
            else
            {
                UGSBus.Subscribe(UGS_EventsEnum.UnityServicesInitialized, OnUnityServicesInitialized);
            }
            UGSBus.Subscribe(UGS_EventsEnum.CheckForExistingSession, OnCheckingForExistingSession);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerSigningIn, OnPlayerSigningIn);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerSignedIn, OnPlayerSignedIn);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerAuthenticated, OnPlayerAuthenticated);
            UGSBus.Subscribe(UGS_EventsEnum.RemoteConfigFetching, OnRemoteConfigFetching);
            UGSBus.Subscribe(UGS_EventsEnum.RemoteConfigUpdated, OnRemoteConfigUpdated);
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.UnityServicesInitialized, OnUnityServicesInitialized);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerSigningIn, OnPlayerSigningIn);
            UGSBus.Unsubscribe(UGS_EventsEnum.RemoteConfigUpdated, OnRemoteConfigUpdated);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerSignedIn, OnPlayerSignedIn);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerAuthenticated, OnPlayerAuthenticated);
            UGSBus.Unsubscribe(UGS_EventsEnum.RemoteConfigFetching, OnRemoteConfigFetching);
            UGSBus.Unsubscribe(UGS_EventsEnum.CheckForExistingSession, OnCheckingForExistingSession);
        }

        private void Reset()
        {
            IsUnityServicesInitialized = false;
            IsCheckForExistingSession = false;
            IsPlayerSigningIn = false;
            IsPlayerSignedIn = false;
            IsPlayerAuthenticated = false;
            IsRemoteConfigFetching = false;
            IsRemoteConfigUpdated = false;
            IsGameReady = false;
        }

        private void OnUnityServicesInitialized(string eventName, object sender, object data)
        {
            IsUnityServicesInitialized = true;
        }

        private void OnCheckingForExistingSession(string eventName, object sender, object data)
        {
            IsCheckForExistingSession = true;
        }

        private void OnPlayerSigningIn(string eventName, object sender, object data)
        {
            IsPlayerSigningIn = true;
            IsPlayerSignedIn = false;
        }

        private void OnPlayerSignedIn(string eventName, object sender, object data)
        {
            IsPlayerSignedIn = true;
            IsPlayerSigningIn = false;
        }

        private void OnPlayerAuthenticated(string eventName, object sender, object data)
        {
            IsPlayerSignedIn = true;
            IsPlayerSigningIn = false;
            IsPlayerAuthenticated = true;
        }

        private void OnRemoteConfigFetching(string eventName, object sender, object data)
        {
            IsRemoteConfigFetching = true;
        }

        private void OnRemoteConfigUpdated(string eventName, object sender, object data)
        {
            IsRemoteConfigUpdated = true;
        }
    }
}