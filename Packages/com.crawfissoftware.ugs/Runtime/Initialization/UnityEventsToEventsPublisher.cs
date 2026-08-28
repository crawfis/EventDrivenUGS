using CrawfisSoftware.UGS;
using CrawfisSoftware.UGS.Events;

using System;

using UnityEngine;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Authentication
{
    internal class UnityEventsToEventsPublisher : MonoBehaviour
    {
        public  void UnityServicesInitialized()
        {
            UGSBus.Publish(UGS_EventsEnum.UnityServicesInitialized, this, null);
        }
        public void UnityServicesInitializationFailed(Exception ex)
        {
            // ServicesInitialization swallows the InitializeAsync exception (empty catch) and no
            // subscriber logs this event, so without this line a failed initialization is silent.
            Debug.LogError($"Unity Services initialization FAILED: {ex}");
            UGSBus.Publish(UGS_EventsEnum.UnityServicesInitializationFailed, this, ex);
        }
        public void SignedIn()
        {
            UGSBus.Publish(UGS_EventsEnum.PlayerSignedIn, this, (Unity.Services.Authentication.AuthenticationService.Instance.PlayerName, Unity.Services.Authentication.AuthenticationService.Instance.PlayerId));
        }
        public void SignInFailed(Exception ex)
        {
            UGSBus.Publish(UGS_EventsEnum.PlayerSignInFailed, this, ex);
        }
        public void SignOut()
        {
            UGSBus.Publish(UGS_EventsEnum.PlayerSignedOut, this, null);
        }
        public void SessionExpired()
        {
            UGSBus.Publish(UGS_EventsEnum.PlayerSessionExpired, this, null);
        }
    }
}