using CrawfisSoftware.UGS.Achievements.UI;
using CrawfisSoftware.UGS.Events;

using UnityEngine;
using UnityEngine.UIElements;

using UGSBus = CrawfisSoftware.Events.EventsFor<CrawfisSoftware.UGS.Events.UGS_EventsEnum>;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Monobehaviour script allowing drag and drop of the AchievementsContainer in a scene.
    ///    Dependencies: PanelRenderer (achievements panel), AchievementsContainerElement
    ///    Subscribes: UGS_EventsEnum.AchievementsOpening, UGS_EventsEnum.AchievementsClosing
    ///    Publishes: UGS_EventsEnum.AchievementsClosed
    /// </summary>
    public class AchievementsPrefab : MonoBehaviour
    {
        [SerializeField]
        bool InitOnAwake = true;
        [SerializeField]
        bool DevelopmentMode = true;
        [SerializeField]
        bool UseTrustedClient;
        [SerializeField]
        Texture2D[] m_Icons;
        [SerializeField]
        PanelRenderer m_UiPanel;

        public AchievementsContainerElement AchievementsContainer { get; private set; }

        private VisualElement _root;
        private VisualElement _externalParent;
        private bool _visible;

        void Awake()
        {
            // The panel is hidden until AchievementsOpening. Visibility is applied to the root's
            // style.display once the UIReload callback delivers the tree; the PanelRenderer itself
            // stays enabled at all times (toggling enabled trips Unity bug UUM-146174).
            _visible = false;

            if (InitOnAwake)
            {
                Initialize(UseTrustedClient);
            }

            UGSBus.Subscribe(UGS_EventsEnum.AchievementsOpening, OnAchievementsOpening);
            UGSBus.Subscribe(UGS_EventsEnum.AchievementsClosing, OnAchievementsClosing);
        }

        private void OnEnable()
        {
            if (m_UiPanel == null) return;
            m_UiPanel.RegisterUIReloadCallback(OnUIReload);
            // Force the renderer on so a scene-authored disabled checkbox cannot blank the panel.
            m_UiPanel.enabled = true;
        }

        private void OnDisable()
        {
            if (m_UiPanel != null)
                m_UiPanel.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnDestroy()
        {
            UGSBus.Unsubscribe(UGS_EventsEnum.AchievementsOpening, OnAchievementsOpening);
            UGSBus.Unsubscribe(UGS_EventsEnum.AchievementsClosing, OnAchievementsClosing);

            // The container releases its own subscription when it leaves a panel, but a container
            // that was built and never attached never gets that event - and the service it holds a
            // handler on is static, so the leak would outlive every scene.
            AchievementsContainer?.Dispose();
        }

        // The PanelRenderer surfaces its visual tree only through this callback (it has no
        // root-tree property), and a reload rebuilds the tree - so the container is re-parented
        // and the current visibility re-applied on every callback.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            AttachContainer();
            ApplyVisibility();
        }

        private void OnAchievementsOpening(string eventName, object sender, object data)
        {
            _visible = true;
            ApplyVisibility();
        }

        private void OnAchievementsClosing(string eventName, object sender, object data)
        {
            _visible = false;
            ApplyVisibility();
            UGSBus.Publish(UGS_EventsEnum.AchievementsClosed, this, null);
        }

        /// <summary>
        /// Initialize the prefab using the information set on this prefab instance
        /// </summary>
        public void Initialize()
        {
            Initialize(UseTrustedClient);
        }

        /// <summary>
        /// Initialize the prefab with client choice and potential different root UI
        /// </summary>
        /// <param name="useTrustedClient">Use local client or cloud code module</param>
        /// <param name="rootElement">UI element to parent to</param>
        public void Initialize(bool useTrustedClient, VisualElement rootElement = null)
        {
            // Icons are handed to the container, which registers them with the shared icon library.
            // The library merges rather than overwrites, so it no longer matters whether this panel
            // or the unlock toast initialises first.
            AchievementsContainer = new AchievementsContainerElement(useTrustedClient, DevelopmentMode, m_Icons);

            if (rootElement != null)
            {
                // An explicit parent wins over the PanelRenderer's own tree.
                _externalParent = rootElement;
                rootElement.Add(AchievementsContainer);
                return;
            }

            // Otherwise parent to the PanelRenderer's root - which may not exist yet (Initialize is
            // typically called from Awake, before the first UIReload). AttachContainer is idempotent
            // and is called again from the reload callback.
            AttachContainer();
        }

        private void AttachContainer()
        {
            if (_externalParent != null) return;
            if (_root == null || AchievementsContainer == null) return;
            if (AchievementsContainer.parent == _root) return;

            _root.Add(AchievementsContainer);
        }

        // Toggle the container, not the panel root. The root holds every element in the panel
        // document, so hiding it took any other UI the consumer authored in the same panel down
        // with the achievements grid - and it did nothing at all in external-parent mode, where
        // _root is never set.
        private void ApplyVisibility()
        {
            if (AchievementsContainer != null)
                AchievementsContainer.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
