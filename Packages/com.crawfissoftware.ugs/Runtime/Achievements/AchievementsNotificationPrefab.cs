using System;

using CrawfisSoftware.UGS.Achievements.UI;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Monobehaviour allowing drag and drop of the AchievementToastElement in a scene.
    ///    Dependencies: PanelRenderer (notification panel), AchievementToastElement
    /// </summary>
    /// <remarks>
    /// <para>See docs/playbooks/uidocument-to-panel-renderer.md. The shape is Pattern 1: the
    /// visual tree is reached through the UIReload callback rather than a <c>rootVisualElement</c>
    /// property, and a reload rebuilds the tree, so re-parenting has to be idempotent and repeated
    /// on every callback.</para>
    /// </remarks>
    public class AchievementsNotificationPrefab : MonoBehaviour
    {
        [SerializeField]
        bool InitOnAwake = true;
        [SerializeField]
        Texture2D[] m_Icons;
        [SerializeField]
        PanelRenderer m_UiPanel;

        /// <summary>
        /// The UI control for the notification. Created by <see cref="Init"/> at runtime.
        /// </summary>
        /// <remarks>
        /// A <see cref="VisualElement"/> is neither a <see cref="UnityEngine.Object"/> nor
        /// <c>[Serializable]</c>, so Unity cannot serialize this and warns (UAC1001) on a public
        /// field that looks like it should be wired in the Inspector. It is runtime-only; saying so
        /// silences the analyser and stops anyone expecting to drag something into it.
        /// </remarks>
        [NonSerialized]
        public AchievementToastElement AchievementsNotification;

        private VisualElement _root;
        private VisualElement _externalParent;

        void Awake()
        {
            if (InitOnAwake)
            {
                Init();
            }
        }

        private void OnEnable()
        {
            if (m_UiPanel == null) return;
            m_UiPanel.RegisterUIReloadCallback(OnUIReload);
            // Force the renderer on so a scene-authored disabled checkbox cannot blank the panel
            // (Unity bug UUM-146174: a PanelRenderer disabled before its first init never re-fires
            // UIReloaded, leaving the panel blank until someone toggles it in the Inspector).
            m_UiPanel.enabled = true;
        }

        private void OnDisable()
        {
            if (m_UiPanel != null)
                m_UiPanel.UnregisterUIReloadCallback(OnUIReload);
        }

        // The PanelRenderer surfaces its visual tree only through this callback, and a reload
        // rebuilds the tree - so the notification element is re-parented on every callback.
        private void OnUIReload(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            AttachNotification();
        }

        /// <summary>
        /// Initialize the prefab
        /// </summary>
        /// <param name="rootElement">UI element to parent to; defaults to the panel's own root.</param>
        public void Init(VisualElement rootElement = null)
        {
            // Icons go to the shared library, which merges by name and keeps the first texture
            // registered under each. That removes the ordering hazard this method used to guard
            // against by hand: this prefab typically ships none while the achievements panel ships
            // the full set, and a plain assignment let whichever ran last blank the other's icons.
            AchievementsNotification = new AchievementToastElement(m_Icons);

            if (rootElement != null)
            {
                // An explicit parent wins over the PanelRenderer's own tree.
                _externalParent = rootElement;
                rootElement.Add(AchievementsNotification);
                return;
            }

            // Otherwise parent to the PanelRenderer's root - which may not exist yet, since Init is
            // typically called from Awake, before the first UIReload. AttachNotification is
            // idempotent and is called again from the reload callback.
            AttachNotification();
        }

        private void AttachNotification()
        {
            if (_externalParent != null) return;
            if (_root == null || AchievementsNotification == null) return;
            if (AchievementsNotification.parent == _root) return;

            _root.Add(AchievementsNotification);
        }
    }
}
