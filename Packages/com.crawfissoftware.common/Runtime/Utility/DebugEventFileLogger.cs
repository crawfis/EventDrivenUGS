using System.IO;

using CrawfisSoftware.Events;

using UnityEngine;

namespace CrawfisSoftware.Utility.Testing
{
    /// <summary>
    /// Debug helper: subscribes to ALL published events (every domain) and writes each one to a
    /// plain-text file (debug_event_log.txt) for offline analysis. Auto-boots via
    /// RuntimeInitializeOnLoadMethod, so no scene wiring is needed - just enter Play mode.
    /// Writes with AutoFlush so the log survives an abrupt Play-mode stop.
    ///    Dependencies: EventsPublisher (global, all-domain)
    /// </summary>
    /// <remarks>
    /// <para><b>Editor and development builds only.</b> The auto-boot is compiled out of release
    /// players. This type ships inside a package, so it runs in every project that merely
    /// references the package - an unconditional auto-boot would mean a released game silently
    /// opening a writer next to its own executable.</para>
    /// <para><b>The log location differs by target</b> for the same reason. In the editor the
    /// project root is convenient and always writable. In a player, <c>dataPath/..</c> is the
    /// install directory, which under Program Files is read-only; a throwing constructor there
    /// would take down the first frame. Players write to <see cref="Application.persistentDataPath"/>,
    /// and a failure to open the file is reported and swallowed rather than propagated - a
    /// diagnostic that breaks the game it is diagnosing is worse than no diagnostic.</para>
    /// </remarks>
    internal class DebugEventFileLogger : MonoBehaviour
    {
        private const string FileName = "debug_event_log.txt";

        private StreamWriter _writer;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("[DebugEventFileLogger]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.DontSave;
            go.AddComponent<DebugEventFileLogger>();
        }
#endif

        private static string ResolveLogPath()
        {
#if UNITY_EDITOR
            // Application.dataPath is <project>/Assets in the editor -> write to the project root.
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileName));
#else
            return Path.Combine(Application.persistentDataPath, FileName);
#endif
        }

        private void Awake()
        {
            string path = ResolveLogPath();
            try
            {
                _writer = new StreamWriter(path, append: false) { AutoFlush = true };
            }
            catch (IOException e)
            {
                Debug.LogWarning($"{nameof(DebugEventFileLogger)}: could not open '{path}' ({e.Message}). Event file logging is disabled for this run.");
                return;
            }
            catch (System.UnauthorizedAccessException e)
            {
                Debug.LogWarning($"{nameof(DebugEventFileLogger)}: no write access to '{path}' ({e.Message}). Event file logging is disabled for this run.");
                return;
            }

            _writer.WriteLine($"# Event log started  realtime={Time.realtimeSinceStartup:F3}  frame={Time.frameCount}");
            _writer.WriteLine($"# columns: realtime | frame | event | sender | data");

            if (EventsPublisher.Instance == null)
            {
                _writer.WriteLine("# ERROR: EventsPublisher.Instance was null at AfterSceneLoad - no events captured.");
                _writer.Flush();
                return;
            }
            EventsPublisher.Instance.SubscribeToAllEvents(LogEvent);
        }

        private void OnDestroy()
        {
            if (EventsPublisher.Instance != null)
                EventsPublisher.Instance.UnsubscribeToAllEvents(LogEvent);

            if (_writer != null)
            {
                _writer.WriteLine($"# Event log stopped  realtime={Time.realtimeSinceStartup:F3}  frame={Time.frameCount}");
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
        }

        private void LogEvent(string eventName, object sender, object data)
        {
            string senderName = sender?.GetType().Name ?? "null";
            string dataStr = data?.ToString() ?? "null";
            _writer?.WriteLine($"{Time.realtimeSinceStartup,10:F3} | f{Time.frameCount,-7} | {eventName,-52} | {senderName,-28} | {dataStr}");
        }
    }
}
