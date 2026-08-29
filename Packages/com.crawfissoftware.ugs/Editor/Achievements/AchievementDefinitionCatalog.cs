using System.Collections.Generic;
using System.IO;

using CrawfisSoftware.UGS.Achievements;

using UnityEditor;

using UnityEngine;

namespace CrawfisSoftware.UGS.Editor.Achievements
{
    /// <summary>
    /// The authoring catalog of achievement definitions. Editor-only; never loaded at runtime.
    ///    Dependencies: AchievementDefinition, AchievementDefinitionExporter
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Definitions reach the game exclusively through Remote Config. This asset exists only
    /// to author the JSON that <see cref="AchievementDefinitionExporter"/> writes to a <c>.rc</c>
    /// deployment file, which is why it is a plain <see cref="ScriptableObject"/> and not something
    /// with an importer: Unity's own dirty/undo/save already does everything the Apply and Revert
    /// buttons of the stack this replaces were doing by hand.</para>
    /// <para><b>Named for what it holds.</b> This is a catalog of <i>definitions</i> - the authored
    /// half. The runtime <c>CrawfisSoftware.UGS.Achievements.AchievementCatalog</c> is the set of
    /// definitions already joined to this player's records, which is a different thing; sharing a
    /// name across those two namespaces made any editor script that imported both fail to
    /// compile.</para>
    /// <para><b>Create this asset in your own project, not inside the package.</b> The default
    /// export target is derived from where the asset lives, so a catalog in the consumer's project
    /// exports next to itself; a catalog inside a package would try to write into the package
    /// cache. An <c>Editor/</c> folder is the recommended home because it also guarantees the asset
    /// is excluded from player builds.</para>
    /// </remarks>
    [CreateAssetMenu(
        fileName = "AchievementDefinitions",
        menuName = "CrawfisSoftware/UGS/Achievement Definitions",
        order = 300)]
    public sealed class AchievementDefinitionCatalog : ScriptableObject
    {
        /// <summary>File name used when no export path has been chosen.</summary>
        public const string DefaultExportFileName = "Achievements";

        [Tooltip("The authored definitions, in the order they will be written.")]
        [SerializeField] private List<AchievementDefinition> _achievements = new List<AchievementDefinition>();

        [Tooltip("Project-relative path the exporter writes to. Empty exports beside this asset.")]
        [SerializeField] private string _exportAssetPath = string.Empty;

        /// <summary>The authored definitions, in declaration order.</summary>
        public IReadOnlyList<AchievementDefinition> Achievements => _achievements;

        /// <summary>
        /// Project-relative path the exporter writes to, for example
        /// <c>Assets/UGS/Editor/Achievements/Achievements.rc</c>. When nothing has been chosen this
        /// resolves to a file beside this asset.
        /// </summary>
        /// <remarks>
        /// Stored as a serialized string rather than an EditorPrefs value or a path recomputed at
        /// import time: a serialized path is version-controlled and reviewable, and two people on
        /// the same project export to the same place.
        /// </remarks>
        public string ExportAssetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_exportAssetPath)) return _exportAssetPath;

                string assetPath = AssetDatabase.GetAssetPath(this);
                string folder = string.IsNullOrEmpty(assetPath) ? "Assets" : Path.GetDirectoryName(assetPath);
                folder = (folder ?? "Assets").Replace('\\', '/');

                return $"{folder}/{DefaultExportFileName}{AchievementsRemoteConfigKeys.RemoteConfigFileExtension}";
            }
            set
            {
                string path = (value ?? string.Empty).Replace('\\', '/').Trim();

                // Without the right extension the Deployment window will not list the file, because
                // it is com.unity.remote-config's importer - not this package - that claims it.
                if (path.Length > 0 &&
                    !path.EndsWith(AchievementsRemoteConfigKeys.RemoteConfigFileExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    path += AchievementsRemoteConfigKeys.RemoteConfigFileExtension;
                }

                _exportAssetPath = path;
            }
        }

        /// <summary>Replace the whole list. Marks the asset dirty; the caller still saves.</summary>
        public void SetAchievements(IEnumerable<AchievementDefinition> definitions)
        {
            _achievements.Clear();
            if (definitions != null)
            {
                foreach (AchievementDefinition definition in definitions)
                {
                    if (definition != null) _achievements.Add(definition);
                }
            }

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Whether every id is present, whitespace-free and unique. Reports every offender at once
        /// rather than the first, so a catalog can be fixed in one pass.
        /// </summary>
        public bool TryValidate(out string error) =>
            AchievementDefinitionExporter.TryValidateIds(_achievements, out error);
    }
}
