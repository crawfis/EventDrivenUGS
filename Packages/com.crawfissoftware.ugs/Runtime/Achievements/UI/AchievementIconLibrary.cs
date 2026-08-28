using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.UGS.Achievements.UI
{
    /// <summary>
    /// Resolves an achievement's <see cref="AchievementDefinition.Icon"/> name to a texture.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// <para><b>Registration merges; it does not overwrite.</b> Two different components supply
    /// icons - the achievements panel supplies the full set, the unlock toast usually supplies one.
    /// A single shared list assigned by each in turn is last-writer-wins, so whichever woke second
    /// silently emptied the other's icons. Merging by name removes that ordering dependency
    /// entirely.</para>
    /// <para><b>The names are a server-side contract.</b> Lookup is by <c>Texture2D.name</c>
    /// against the <c>Icon</c> string in the achievement definition, and those definitions live in
    /// Remote Config. Renaming a texture asset therefore breaks artwork in a deployed game without
    /// touching any code.</para>
    /// </remarks>
    public static class AchievementIconLibrary
    {
        /// <summary>Shown when a definition names an icon that was never registered.</summary>
        public const string FallbackIconName = "thumbnail_black";

        private static readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>();

        /// <summary>Every icon name currently registered. Useful when diagnosing a missing icon.</summary>
        public static IEnumerable<string> RegisteredNames => _icons.Keys;

        /// <summary>
        /// Add icons to the shared set. Null entries are skipped; a repeated name keeps the first
        /// texture registered, so a late component cannot displace the panel's artwork.
        /// </summary>
        public static void Register(IEnumerable<Texture2D> icons)
        {
            if (icons == null) return;
            foreach (var icon in icons)
            {
                if (icon == null || string.IsNullOrEmpty(icon.name)) continue;
                if (_icons.ContainsKey(icon.name)) continue;
                _icons[icon.name] = icon;
            }
        }

        /// <summary>
        /// The texture for this icon name, the fallback when it is unknown, or null when even the
        /// fallback was never registered.
        /// </summary>
        public static Texture2D Get(string iconName)
        {
            if (!string.IsNullOrEmpty(iconName) && _icons.TryGetValue(iconName, out var icon))
                return icon;

            if (_icons.TryGetValue(FallbackIconName, out var fallback))
            {
                if (!string.IsNullOrEmpty(iconName))
                    Debug.LogWarning($"{nameof(AchievementIconLibrary)}: no icon named '{iconName}'; using '{FallbackIconName}'.");
                return fallback;
            }
            return null;
        }

        /// <summary>Drop every registered icon. Intended for tests and domain reload.</summary>
        public static void Clear() => _icons.Clear();
    }
}
