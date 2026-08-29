using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using CrawfisSoftware.UGS.Achievements;

using UnityEditor;

using UnityEngine;

namespace CrawfisSoftware.UGS.Editor.Achievements
{
    /// <summary>
    /// Writes an <see cref="AchievementCatalog"/> to a Remote Config <c>.rc</c> deployment file,
    /// and reads one back.
    ///    Dependencies: AchievementCatalog, AchievementsRemoteConfigKeys, UnityEditor.FileUtil
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para>Exports <c>.rc</c>, not the <c>.ach</c> extension the vendored stack invented. Only
    /// that stack's own ScriptedImporter ever made <c>.ach</c> a Deployment item, so exporting one
    /// now would produce a file the Deployment window cannot see. <c>.rc</c> is claimed by
    /// com.unity.remote-config itself and the envelope is byte-shape identical, which also means
    /// this package registers no importer and has no extension to collide over.</para>
    /// <para>Two defects in the code this replaces are fixed by construction. It resolved paths as
    /// <c>Application.dataPath.Replace("Assets", "")</c>, which strips every occurrence of that
    /// substring from the project path and cannot address a package at all; here
    /// <see cref="ResolvePhysicalPath"/> asks <see cref="FileUtil.GetPhysicalPath"/>. And its save
    /// caught the write exception and returned success anyway, so the inspector cleared its dirty
    /// flag and reported a write that never happened; <see cref="TryExport"/> returns false on any
    /// caught exception and hands back the message.</para>
    /// <para>Serialisation is <see cref="JsonUtility"/> for reading and a hand-built string for
    /// writing. Writing by hand is what gives exact control over key order and indentation, so a
    /// re-export of an unchanged catalog produces no diff.</para>
    /// </remarks>
    public static class AchievementCatalogExporter
    {
        /// <summary>Menu path of the export command.</summary>
        public const string ExportMenuPath = "CrawfisSoftware/UGS/Achievements/Export Catalog to Remote Config";

        private const string Indent = "  ";

        /// <summary>
        /// Validate the catalog, write it to <see cref="AchievementCatalog.ExportAssetPath"/>,
        /// import it and ping it in the Project window.
        /// </summary>
        /// <returns>
        /// False on a blank, whitespace-bearing or duplicate id, or on any IO failure - never true
        /// after a caught exception.
        /// </returns>
        public static bool TryExport(AchievementCatalog catalog, out string exportedAssetPath, out string error)
        {
            exportedAssetPath = null;

            if (catalog == null)
            {
                error = "No catalog to export.";
                return false;
            }

            if (!TryValidateIds(catalog.Achievements, out error)) return false;

            string assetPath = catalog.ExportAssetPath;
            if (string.IsNullOrEmpty(assetPath))
            {
                error = "No export path is set on the catalog.";
                return false;
            }

            try
            {
                string physicalPath = ResolvePhysicalPath(assetPath);
                string directory = Path.GetDirectoryName(physicalPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(physicalPath, BuildRemoteConfigJson(catalog.Achievements), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                error = $"Could not write '{assetPath}'. {e.Message}";
                return false;
            }

            // Only inside the project can the AssetDatabase see the file. An export deliberately
            // aimed outside it still succeeded; it just has nothing to import or ping.
            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                var imported = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (imported != null) EditorGUIUtility.PingObject(imported);
            }

            exportedAssetPath = assetPath;
            error = null;
            return true;
        }

        /// <summary>
        /// Build the <c>{$schema, entries, types}</c> envelope. Pure - no IO, no AssetDatabase.
        /// </summary>
        /// <param name="definitions">The definitions to write, in order.</param>
        /// <param name="emitJsonType">
        /// Whether to declare the entry's type as JSON. Remote Config's own writer omits the
        /// declaration for JSON entries, but the file this replaces carried it and deployed, so it
        /// stays on by default and the flag is the escape hatch.
        /// </param>
        public static string BuildRemoteConfigJson(IReadOnlyList<AchievementDefinition> definitions, bool emitJsonType = true)
        {
            string key = AchievementsRemoteConfigKeys.AchievementsKey;
            var json = new StringBuilder();

            json.Append("{\n");
            json.Append($"{Indent}\"$schema\": \"{AchievementsRemoteConfigKeys.RemoteConfigSchemaUrl}\",\n");
            json.Append($"{Indent}\"entries\": {{\n");
            json.Append($"{Indent}{Indent}\"{Escape(key)}\": [");

            int count = definitions?.Count ?? 0;
            int written = 0;
            for (int i = 0; i < count; i++)
            {
                AchievementDefinition definition = definitions[i];
                if (definition == null) continue;

                json.Append(written == 0 ? "\n" : ",\n");
                AppendDefinition(json, definition);
                written++;
            }

            json.Append(written == 0 ? "]\n" : $"\n{Indent}{Indent}]\n");
            json.Append($"{Indent}}}");

            if (emitJsonType)
            {
                json.Append(",\n");
                json.Append($"{Indent}\"types\": {{\n");
                json.Append($"{Indent}{Indent}\"{Escape(key)}\": \"{AchievementsRemoteConfigKeys.JsonValueType}\"\n");
                json.Append($"{Indent}}}\n");
            }
            else
            {
                json.Append('\n');
            }

            json.Append("}\n");
            return json.ToString();
        }

        /// <summary>
        /// Parse any file carrying the envelope - a legacy <c>.ach</c>, a <c>.rc</c>, or a Remote
        /// Config export - into definitions, for migrating an existing authored file.
        /// </summary>
        public static bool TryImportJson(string absolutePath, out List<AchievementDefinition> definitions, out string error)
        {
            definitions = null;

            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                error = $"No file at '{absolutePath}'.";
                return false;
            }

            try
            {
                string json = File.ReadAllText(absolutePath);
                var envelope = JsonUtility.FromJson<Envelope>(json);

                if (envelope?.entries?.achievements == null)
                {
                    error = $"'{Path.GetFileName(absolutePath)}' has no entries.{AchievementsRemoteConfigKeys.AchievementsKey} array.";
                    return false;
                }

                definitions = envelope.entries.achievements;
                error = null;
                return true;
            }
            catch (Exception e)
            {
                error = $"Could not read '{Path.GetFileName(absolutePath)}'. {e.Message}";
                return false;
            }
        }

        /// <summary>
        /// Whether every id is present, whitespace-free and unique. Every offender is listed in one
        /// message so a catalog can be fixed in a single pass.
        /// </summary>
        public static bool TryValidateIds(IReadOnlyList<AchievementDefinition> definitions, out string error)
        {
            var problems = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            int count = definitions?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                AchievementDefinition definition = definitions[i];

                if (definition == null)
                {
                    problems.Add($"Entry {i} is empty.");
                    continue;
                }

                string id = definition.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    problems.Add($"Entry {i} has no Id.");
                    continue;
                }

                // The id is a Cloud Save record key as well as a lookup key, so whitespace in it
                // would survive as far as the save and then fail there instead of here.
                if (id.Trim() != id || id.IndexOf(' ') >= 0)
                    problems.Add($"Entry {i} ('{id}') has whitespace in its Id.");

                if (!seen.Add(id))
                    problems.Add($"Entry {i} repeats the Id '{id}'.");
            }

            error = problems.Count == 0 ? null : string.Join("\n", problems);
            return problems.Count == 0;
        }

        /// <summary>
        /// Map a project-relative asset path to an absolute on-disk path.
        /// </summary>
        /// <remarks>
        /// <see cref="FileUtil.GetPhysicalPath"/> handles both <c>Assets/</c> and
        /// <c>Packages/&lt;name&gt;/</c>, including a package resolved into the package cache -
        /// which string surgery on <see cref="Application.dataPath"/> cannot do.
        /// </remarks>
        public static string ResolvePhysicalPath(string projectRelativeAssetPath)
        {
            if (string.IsNullOrEmpty(projectRelativeAssetPath)) return null;

            string physical = FileUtil.GetPhysicalPath(projectRelativeAssetPath);
            if (!string.IsNullOrEmpty(physical)) return Path.GetFullPath(physical);

            // GetPhysicalPath yields nothing for a path that is not yet an asset, which is the
            // ordinary case on a first export.
            return Path.GetFullPath(projectRelativeAssetPath);
        }

        [MenuItem(ExportMenuPath)]
        private static void ExportSelected()
        {
            var catalog = Selection.activeObject as AchievementCatalog;
            if (catalog == null) return;

            if (TryExport(catalog, out string assetPath, out string error))
                Debug.Log($"Achievements exported to '{assetPath}'.", catalog);
            else
                Debug.LogError($"Achievement export failed.\n{error}", catalog);
        }

        [MenuItem(ExportMenuPath, isValidateFunction: true)]
        private static bool ValidateExportSelected() => Selection.activeObject is AchievementCatalog;

        private static void AppendDefinition(StringBuilder json, AchievementDefinition definition)
        {
            string pad = Indent + Indent + Indent;
            string field = pad + Indent;

            json.Append(pad).Append("{\n");
            json.Append(field).Append("\"Id\": ").Append(Quote(definition.Id)).Append(",\n");
            json.Append(field).Append("\"Icon\": ").Append(Quote(definition.Icon)).Append(",\n");
            json.Append(field).Append("\"Title\": ").Append(Quote(definition.Title)).Append(",\n");
            json.Append(field).Append("\"Description\": ").Append(Quote(definition.Description)).Append(",\n");
            json.Append(field).Append("\"IsHidden\": ").Append(definition.IsHidden ? "true" : "false").Append(",\n");
            json.Append(field).Append("\"ProgressTarget\": ")
                .Append(definition.ProgressTarget.ToString(CultureInfo.InvariantCulture)).Append('\n');
            json.Append(pad).Append('}');
        }

        private static string Quote(string value) => $"\"{Escape(value)}\"";

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var escaped = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': escaped.Append("\\\""); break;
                    case '\\': escaped.Append("\\\\"); break;
                    case '\b': escaped.Append("\\b"); break;
                    case '\f': escaped.Append("\\f"); break;
                    case '\n': escaped.Append("\\n"); break;
                    case '\r': escaped.Append("\\r"); break;
                    case '\t': escaped.Append("\\t"); break;
                    default:
                        if (c < 0x20) escaped.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else escaped.Append(c);
                        break;
                }
            }
            return escaped.ToString();
        }

        // JsonUtility binds by field name, so these mirror the envelope's shape exactly. The field
        // name 'achievements' has to equal AchievementsRemoteConfigKeys.AchievementsKey - a C#
        // field name cannot be a const, so this is the one place the two can drift apart.
        [Serializable]
        private sealed class Envelope
        {
            public EntriesBlock entries;
        }

        [Serializable]
        private sealed class EntriesBlock
        {
            public List<AchievementDefinition> achievements;
        }
    }
}
