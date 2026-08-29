using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

// Reports what Unity itself thinks is broken in the staged sample assets: components whose script
// no longer resolves, and object references that point at an asset this project does not have.
// Runs before and after the fixer so the difference is measurable rather than asserted.
public static class SamplesAudit
{
    private const string SceneFolder = "Assets/UGS-Scenes/Scenes";
    private const string PrefabFolder = "Assets/UGS-Scenes/Prefabs";

    public static void Report()
    {
        int missingScripts = 0;
        int missingRefs = 0;

        foreach (string path in ScenePaths())
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Debug.Log($"AUDIT_SCENE {scene.name}");

            // The property throws when unset rather than returning null, so it cannot be read
            // directly on a scene whose lighting asset does not resolve - which is exactly the
            // case being measured.
            string lighting = Lightmapping.TryGetLightingSettings(out LightingSettings settings) && settings != null
                ? settings.name
                : "none";
            Debug.Log($"AUDIT_LIGHTING {scene.name} -> {lighting}");

            foreach (GameObject root in scene.GetRootGameObjects())
                Walk(root, scene.name, ref missingScripts, ref missingRefs);
        }

        foreach (string path in PrefabPaths())
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
            {
                Debug.Log($"AUDIT_PREFAB_UNLOADABLE {path}");
                continue;
            }
            Debug.Log($"AUDIT_PREFAB {go.name}");
            Walk(go, go.name, ref missingScripts, ref missingRefs);
        }

        Debug.Log($"AUDIT_TOTAL missingScripts={missingScripts} missingRefs={missingRefs}");
        EditorApplication.Exit(0);
    }

    private static void Walk(GameObject root, string owner, ref int missingScripts, ref int missingRefs)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            Component[] components = t.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c == null)
                {
                    missingScripts++;
                    Debug.Log($"AUDIT_MISSING_SCRIPT {owner} :: {Path(t)} [component {i}]");
                    continue;
                }

                var so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;

                    // A reference whose target is gone keeps its id but resolves to null.
                    if (p.objectReferenceValue == null && HasDanglingId(p))
                    {
                        missingRefs++;
                        Debug.Log($"AUDIT_MISSING_REF {owner} :: {Path(t)} :: {c.GetType().Name}.{p.propertyPath}");
                    }
                }
            }
        }
    }

    // objectReferenceInstanceIDValue became an ERROR-level obsolete in Unity 6 and the replacement
    // is objectReferenceEntityIdValue, whose type differs across versions. Reflection keeps this
    // working on either without a compile-time dependency on which one exists.
    private static readonly System.Reflection.PropertyInfo IdProperty =
        typeof(SerializedProperty).GetProperty("objectReferenceEntityIdValue")
        ?? typeof(SerializedProperty).GetProperty("objectReferenceInstanceIDValue");

    private static bool HasDanglingId(SerializedProperty p)
    {
        if (IdProperty == null) return false;

        object value = IdProperty.GetValue(p);
        if (value == null) return false;

        // A default-valued id means "no reference was ever set"; anything else is a reference that
        // was set and then failed to resolve.
        object none = Activator.CreateInstance(value.GetType());
        return !value.Equals(none);
    }

    private static string Path(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Add(t.name); t = t.parent; }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static IEnumerable<string> ScenePaths() =>
        AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder })
                     .Select(AssetDatabase.GUIDToAssetPath)
                     .OrderBy(p => p);

    private static IEnumerable<string> PrefabPaths() =>
        AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder })
                     .Select(AssetDatabase.GUIDToAssetPath)
                     .OrderBy(p => p);
}
