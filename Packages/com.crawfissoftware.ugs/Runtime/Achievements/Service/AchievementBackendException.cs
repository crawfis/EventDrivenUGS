using System;

namespace CrawfisSoftware.UGS.Achievements
{
    /// <summary>
    /// An achievement operation failed in the backing service.
    ///    Dependencies: none
    ///    Subscribes: none
    ///    Publishes: none
    /// </summary>
    /// <remarks>
    /// Exists so a failure can be reported with a reason rather than swallowed into a log line. The
    /// most common cause is worth distinguishing: a project whose access policy denies player
    /// writes to saved data will fail every unlock with a 403, and the fix is a project
    /// configuration change, not a code change - see <see cref="IsAccessDenied"/>.
    /// </remarks>
    public sealed class AchievementBackendException : Exception
    {
        public AchievementBackendException(string message, Exception inner = null)
            : base(message, inner)
        {
        }

        /// <summary>
        /// True when the underlying failure looks like an access-policy denial. Distinguishing this
        /// matters because it is the one failure a developer can fix without touching the game.
        /// </summary>
        public bool IsAccessDenied
        {
            get
            {
                for (Exception e = this; e != null; e = e.InnerException)
                {
                    string message = e.Message;
                    if (message != null && (message.Contains("403") || message.Contains("Forbidden")))
                        return true;
                }
                return false;
            }
        }
    }
}
