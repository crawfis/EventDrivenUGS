using Blocks.Achievements.UI;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// Monobehaviour allowing drag and drop of the AchievementNotificationElement in a scene.
    ///    Dependencies: PanelRenderer (notification panel), AchievementNotificationElement
    /// </summary>
    /// <remarks>
    /// <para>A PanelRenderer fork of <c>Blocks.Achievements.AchievementsNotificationPrefab</c>,
    /// matching the existing fork of <c>AchievementsPrefab</c>. The Blocks original is vendored
    /// Unity sample code and is deliberately left untouched - re-importing the sample would
    /// overwrite an in-place edit.</para>
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
        /// The UI control for the notification
        /// </summary>
        public AchievementNotificationElement AchievementsNotification;

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
            // AchievementBaseElement.Icons is STATIC and shared with AchievementsPrefab, which
            // ships four icons while this prefab ships none - in the Blocks original too. Assigning
            // unconditionally means whichever initialises last wins, so an empty array here wipes
            // the icons the achievements panel supplied and toasts render blank. Publish ours only
            // if we actually have any; otherwise just guarantee the list is non-null, since
            // AchievementBaseElement calls Icons.Find without a null check.
            if (m_Icons != null && m_Icons.Length > 0)
            {
                AchievementBaseElement.Icons = m_Icons.ToList();
            }
            else
            {
                AchievementBaseElement.Icons ??= new List<Texture2D>();
            }

            AchievementsNotification = new AchievementNotificationElement();

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
