using CrawfisSoftware.UGS.Events;

using UnityEngine;
using UnityEngine.UIElements;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Authentication
{
    /// <summary>
    /// Shows/hides the player sign-in panel around the UGS authentication flow.
    ///    Dependencies: PanelRenderer (sign-in panel), PlayerSignIn
    ///    Subscribes: UGS_EventsEnum.PlayerSigningIn, UGS_EventsEnum.PlayerAuthenticated,
    ///                UGS_EventsEnum.PlayerSignedOut
    ///    Publishes: UGS_EventsEnum.PlayerSigningOut
    /// </summary>
    public class PlayerSignInController : MonoBehaviour
    {
        [SerializeField] PanelRenderer signInPanel;

        const string k_HiddenClass = "hidden";
        VisualElement _root;
        VisualElement _signInElement;
        bool _signedIn = false;
        bool _hidden = false;

        private void Awake()
        {
            UGSBus.Subscribe(UGS_EventsEnum.PlayerSigningIn, OnPlayerSigningIn);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerAuthenticated, OnSignIn);
            UGSBus.Subscribe(UGS_EventsEnum.PlayerSignedOut, OnPlayerSignOut);

            if (UGS_State.IsPlayerSigningIn)
            {
                OnPlayerSigningIn("UGS_EventsEnum/" + UGS_EventsEnum.PlayerSigningIn.ToString(), this, null);
            }
        }

        private void OnEnable()
        {
            if (signInPanel == null) return;
            signInPanel.RegisterUIReloadCallback(OnUIReload);
            // Visibility is driven by style.display / the "hidden" class, so the PanelRenderer must
            // stay enabled for its tree to build. Force it on in case the scene authored it
            // disabled (a panel disabled before its first init trips Unity bug UUM-146174).
            signInPanel.enabled = true;
        }

        private void OnDisable()
        {
            if (signInPanel != null)
                signInPanel.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerSigningIn, OnPlayerSigningIn);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerAuthenticated, OnSignIn);
            UGSBus.Unsubscribe(UGS_EventsEnum.PlayerSignedOut, OnPlayerSignOut);
        }

        // The PanelRenderer surfaces its visual tree only through this callback (it has no
        // root-tree property). A reload rebuilds the tree, so the sign-in element is re-queried and
        // the current hidden/shown state re-applied every time.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            _signInElement = root.Q<PlayerSignIn>();
            ApplyHidden();
        }

        private void OnSignIn(string eventName, object sender, object data)
        {
            _signedIn = true;
            SetHidden(true);
        }

        private void OnPlayerSignOut(string eventName, object sender, object data)
        {
            _signedIn = false;
            SetHidden(false);
            UGSBus.Publish(UGS_EventsEnum.PlayerSigningOut, this, null);
        }

        private void OnPlayerSigningIn(string eventName, object sender, object data)
        {
            if (_signedIn)
            {
                return;
            }
            SetHidden(false);
        }

        private void SetHidden(bool hidden)
        {
            _hidden = hidden;
            ApplyHidden();
        }

        // The panel GameObject is deliberately left active (the pre-PanelRenderer version called
        // SetActive here): deactivating it would tear the visual tree down and unregister the
        // reload callback, and re-activating is subject to UUM-146174. Hiding is purely visual.
        private void ApplyHidden()
        {
            if (_root == null) return;

            if (_hidden)
            {
                _root.AddToClassList(k_HiddenClass);
                _signInElement?.AddToClassList(k_HiddenClass);
            }
            else
            {
                _root.RemoveFromClassList(k_HiddenClass);
                _signInElement?.RemoveFromClassList(k_HiddenClass);
            }

            _root.style.display = _hidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void Start()
        {
            if (signInPanel == null)
            {
                Debug.LogError("No PanelRenderer assigned on PlayerSignInController!");
            }

            // There is deliberately no second "signed in" callback here. The PlayerAuthenticated
            // subscription in Awake already hides the panel, and the observer this replaced
            // registered a duplicate that did exactly the same thing - with no way to unregister it.
        }
    }
}
