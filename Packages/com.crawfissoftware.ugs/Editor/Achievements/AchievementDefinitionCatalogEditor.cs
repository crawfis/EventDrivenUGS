using System.Collections.Generic;
using System.IO;

using CrawfisSoftware.UGS.Achievements;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.UGS.Editor.Achievements
{
    /// <summary>
    /// Inspector for <see cref="AchievementDefinitionCatalog"/>: the definition list, the export path, live
    /// id validation, and the Export and Import buttons.
    ///    Dependencies: AchievementDefinitionCatalog, AchievementDefinitionExporter
    ///    Subscribes: none (editor-only; no event bus involvement)
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Built entirely in C#. It ships no <c>.uxml</c> and no <c>.uss</c>, so there is nothing
    /// here located by a name search at runtime - which is how the inspector this replaces found
    /// its own layout, and why moving that file would have broken it silently.</para>
    /// <para>Apply and Revert are gone. On a real <c>.asset</c> Unity's own dirty/undo/save does
    /// that job, and does it better than a hand-rolled serialized-object diff.</para>
    /// </remarks>
    [CustomEditor(typeof(AchievementDefinitionCatalog))]
    public sealed class AchievementDefinitionCatalogEditor : UnityEditor.Editor
    {
        private const string EditorFolderSegment = "/Editor/";

        public override VisualElement CreateInspectorGUI()
        {
            var catalog = (AchievementDefinitionCatalog)target;
            var root = new VisualElement();

            // The returned element is auto-bound to serializedObject, so a PropertyField on the
            // list yields Unity's reorderable list for free.
            var achievements = new PropertyField(serializedObject.FindProperty("_achievements"));
            root.Add(achievements);

            root.Add(Spacer());
            root.Add(BuildExportPathRow(catalog));

            var idProblems = new HelpBox(string.Empty, HelpBoxMessageType.Error) { style = { display = DisplayStyle.None } };
            root.Add(idProblems);
            RefreshValidation(catalog, idProblems);

            // Re-validate on any edit rather than only on export: a duplicate id is much cheaper to
            // notice while typing it than at deployment time.
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshValidation(catalog, idProblems));

            if (!IsUnderEditorFolder(catalog))
            {
                root.Add(new HelpBox(
                    "This catalog is editor-only authoring data. Keeping it under an Editor/ folder " +
                    "keeps it out of player builds.",
                    HelpBoxMessageType.Info));
            }

            root.Add(Spacer());
            root.Add(new Button(() => Export(catalog)) { text = "Export to Remote Config (.rc)" });
            root.Add(new Button(() => Import(catalog)) { text = "Import from JSON..." });

            return root;
        }

        private VisualElement BuildExportPathRow(AchievementDefinitionCatalog catalog)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var path = new TextField("Export To") { value = catalog.ExportAssetPath };
            path.style.flexGrow = 1f;
            path.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(catalog, "Set achievement export path");
                catalog.ExportAssetPath = evt.newValue;
                EditorUtility.SetDirty(catalog);
            });

            var browse = new Button(() =>
            {
                string startFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(catalog));
                string chosen = EditorUtility.SaveFilePanelInProject(
                    "Export achievements",
                    AchievementDefinitionCatalog.DefaultExportFileName,
                    AchievementsRemoteConfigKeys.RemoteConfigFileExtension.TrimStart('.'),
                    string.Empty,
                    string.IsNullOrEmpty(startFolder) ? "Assets" : startFolder);

                if (string.IsNullOrEmpty(chosen)) return;

                Undo.RecordObject(catalog, "Set achievement export path");
                catalog.ExportAssetPath = chosen;
                EditorUtility.SetDirty(catalog);
                path.SetValueWithoutNotify(catalog.ExportAssetPath);
            })
            { text = "Browse..." };

            row.Add(path);
            row.Add(browse);
            return row;
        }

        private static void Export(AchievementDefinitionCatalog catalog)
        {
            // Save first: exporting what is on disk rather than what is in the inspector would
            // write a file that does not match the asset it came from.
            AssetDatabase.SaveAssetIfDirty(catalog);

            if (AchievementDefinitionExporter.TryExport(catalog, out string assetPath, out string error))
            {
                Debug.Log($"Achievements exported to '{assetPath}'.", catalog);
                return;
            }

            EditorUtility.DisplayDialog("Achievement export failed", error, "OK");
        }

        private static void Import(AchievementDefinitionCatalog catalog)
        {
            string chosen = EditorUtility.OpenFilePanel("Import achievement definitions", "Assets", "rc,ach,json");
            if (string.IsNullOrEmpty(chosen)) return;

            if (!AchievementDefinitionExporter.TryImportJson(chosen, out List<AchievementDefinition> definitions, out string error))
            {
                EditorUtility.DisplayDialog("Import failed", error, "OK");
                return;
            }

            Undo.RecordObject(catalog, "Import achievement definitions");
            catalog.SetAchievements(definitions);
            AssetDatabase.SaveAssetIfDirty(catalog);

            Debug.Log($"Imported {definitions.Count} achievement definitions from '{Path.GetFileName(chosen)}'.", catalog);
        }

        private static void RefreshValidation(AchievementDefinitionCatalog catalog, HelpBox box)
        {
            bool valid = catalog.TryValidate(out string error);
            box.text = valid ? string.Empty : error;
            box.style.display = valid ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static bool IsUnderEditorFolder(Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            return !string.IsNullOrEmpty(assetPath) &&
                   assetPath.Replace('\\', '/').Contains(EditorFolderSegment);
        }

        private static VisualElement Spacer() => new VisualElement { style = { height = 8f } };
    }
}
