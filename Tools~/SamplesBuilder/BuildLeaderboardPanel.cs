using System.Linq;

using CrawfisSoftware.UGS.Leaderboard;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Builds LeaderboardPanel.prefab and swaps it in for the vendored prefab instance that
// Leaderboards.unity still points at. Done through the editor API rather than by editing YAML, so
// Unity writes the serialisation and the fileIDs.
public static class BuildLeaderboardPanel
{
    private const string PrefabPath = "Assets/UGS-Scenes/Prefabs/LeaderboardPanel.prefab";
    private const string ScenePath = "Assets/UGS-Scenes/Scenes/Leaderboards.unity";
    private const string OldInstanceName = "LeaderboardPrefab";

    private const string PanelSettingsPath = "Packages/com.crawfissoftware.ugs/Runtime/UI/UgsPanelSettings.asset";
    private const string CoreUssPath = "Packages/com.crawfissoftware.ugs/Runtime/UI/Theme/UgsCore.uss";
    private const string ComponentsUssPath = "Packages/com.crawfissoftware.ugs/Runtime/UI/Theme/UgsComponents.uss";

    // Carried off the LeaderboardController fields the extraction deleted: they were authored in
    // UGS_Boot_4_Leaderboards.unity and read by nothing, and the panel's own defaults (global,
    // top 25) would silently show a different board.
    private const string LeaderboardId = "DailyDistance";
    private const string TierId = "weekly_distance_tier_1";
    private const int NumberToDisplay = 10;

    public static void Run()
    {
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        var core = AssetDatabase.LoadAssetAtPath<StyleSheet>(CoreUssPath);
        var components = AssetDatabase.LoadAssetAtPath<StyleSheet>(ComponentsUssPath);

        if (panelSettings == null || core == null || components == null)
        {
            Debug.LogError($"BUILD_FAIL missing package asset: panelSettings={panelSettings != null} " +
                           $"core={core != null} components={components != null}");
            EditorApplication.Exit(1);
            return;
        }

        // ---- the prefab ----
        var go = new GameObject("LeaderboardPanel");
        var renderer = go.AddComponent<PanelRenderer>();
        renderer.panelSettings = panelSettings;

        var panel = go.AddComponent<LeaderboardPanel>();

        var so = new SerializedObject(panel);
        so.FindProperty("_panel").objectReferenceValue = renderer;

        SerializedProperty sheets = so.FindProperty("_styleSheets");
        sheets.arraySize = 2;
        // Core first: UgsComponents.uss is written against the --ugs-* tokens Core declares.
        sheets.GetArrayElementAtIndex(0).objectReferenceValue = core;
        sheets.GetArrayElementAtIndex(1).objectReferenceValue = components;

        so.FindProperty("_leaderboardId").stringValue = LeaderboardId;
        so.FindProperty("_tierId").stringValue = TierId;
        so.FindProperty("_numberToDisplay").intValue = NumberToDisplay;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out bool saved);
        Object.DestroyImmediate(go);

        if (!saved || prefab == null)
        {
            Debug.LogError("BUILD_FAIL could not save LeaderboardPanel.prefab");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log($"BUILD_OK prefab saved: {PrefabPath}");

        // ---- swap it into the scene ----
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Unity renames a broken instance to "<name> (Missing Prefab with guid: ...)", so match on
        // the condition rather than the name - that is also what makes this safe to re-run.
        GameObject old = scene.GetRootGameObjects()
            .FirstOrDefault(g => PrefabUtility.IsPrefabAssetMissing(g)
                                 && g.name.StartsWith(OldInstanceName, System.StringComparison.Ordinal));

        if (old == null)
        {
            bool alreadyDone = scene.GetRootGameObjects().Any(g => g.name == "LeaderboardPanel");
            if (alreadyDone)
            {
                Debug.Log($"BUILD_OK {scene.name} already carries LeaderboardPanel; nothing to swap");
                EditorApplication.Exit(0);
                return;
            }

            string roots = string.Join(", ", scene.GetRootGameObjects().Select(g => g.name));
            Debug.LogError($"BUILD_FAIL no missing-prefab root starting '{OldInstanceName}' in {scene.name}. Roots: {roots}");
            EditorApplication.Exit(1);
            return;
        }

        Transform t = old.transform;
        Vector3 position = t.localPosition;
        Quaternion rotation = t.localRotation;
        int siblingIndex = t.GetSiblingIndex();
        Debug.Log($"BUILD_OK removing '{old.name}' (missing prefab: {PrefabUtility.IsPrefabAssetMissing(old)})");
        Object.DestroyImmediate(old);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "LeaderboardPanel";
        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        instance.transform.SetSiblingIndex(siblingIndex);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"BUILD_OK {scene.name} now instantiates LeaderboardPanel.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }
}
