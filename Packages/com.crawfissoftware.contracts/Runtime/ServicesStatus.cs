namespace CrawfisSoftware.Contracts
{
    /// <summary>
    /// Whether the backing services are usable. Carried by
    /// <see cref="GameServiceEvents.ServicesStatusChanged"/>.
    /// </summary>
    /// <remarks>
    /// Three values rather than a bool, because "not ready" is two different situations to a
    /// player: still trying, and gave up. A loading screen that cannot tell them apart shows the
    /// same spinner for a slow connection and a dead one.
    /// </remarks>
    public enum ServicesStatus
    {
        /// <summary>Initialising or authenticating. Show progress; do not offer a fallback yet.</summary>
        Connecting = 0,

        /// <summary>Authenticated and usable. Gameplay may proceed.</summary>
        Ready = 1,

        /// <summary>
        /// Initialisation or sign-in failed, or the session ended. The host decides what to do -
        /// offer a retry, or fall back to an offline mode.
        /// </summary>
        Unavailable = 2,
    }
}
