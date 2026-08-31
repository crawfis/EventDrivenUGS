using CrawfisSoftware.Events;

namespace CrawfisSoftware.Contracts
{
    /// <summary>
    /// The vocabulary a game and a backing services layer use to talk to each other, in terms
    /// neither of them owns.
    /// </summary>
    /// <remarks>
    /// <para>Without this, a services layer ends up naming the game's events - UGS subscribed to
    /// <c>UGS_CoinUpdated</c>, which existed only because this game has coins - and a game ends up
    /// naming the service's. Either way one side cannot be swapped without editing the other.</para>
    /// <para>Both sides map into this enum instead. A game publishes <see cref="ScoreUpdated"/>
    /// however it computes score; the services layer consumes it without knowing whether the score
    /// is metres run, puzzles solved, or laps completed. Neither references the other's types.</para>
    /// <para><b>Deliberately small.</b> Every member here is a crossing someone must maintain
    /// forever, so it carries only what a service genuinely needs. Anything specific to one game
    /// belongs in that game's own domain, translated into these events by per-game glue.</para>
    /// <para><b>Payloads.</b> Declared only where the type is primitive and unambiguous. The
    /// difficulty payload is deliberately undeclared: it currently carries a game-side config type,
    /// and a contract that references the game's types is not a contract.</para>
    /// </remarks>
    [EventEnum]
    public enum GameServiceEvents
    {
        // ---------- Game -> Services ----------

        /// <summary>The run's score/progress metric changed. Data: float.</summary>
        [EventPayload(typeof(float))]
        ScoreUpdated = 0,

        /// <summary>
        /// The player's soft-currency total for this session changed. Data: int, the running
        /// TOTAL rather than a delta - named for what it carries, not what it sounds like.
        /// </summary>
        [EventPayload(typeof(int))]
        CurrencyTotalChanged = 1,

        /// <summary>A session is finishing and its result is final. Data: float score.</summary>
        [EventPayload(typeof(float))]
        SessionEnding = 10,

        /// <summary>The session is over and the game has returned to a neutral state.</summary>
        SessionEnded = 11,

        // ---------- Services -> Game ----------

        /// <summary>
        /// Edge: services just became available. Transient, for the moment itself - a sound, a
        /// transition. Anything that needs to know the current state reads
        /// <see cref="ServicesStatusChanged"/> instead.
        /// </summary>
        ServicesReady = 20,

        /// <summary>Edge: services just became unavailable. Transient, as above.</summary>
        ServicesUnavailable = 21,

        /// <summary>
        /// The level: what the services state currently IS. Data: <see cref="ServicesStatus"/>.
        /// </summary>
        /// <remarks>
        /// <para>Sticky, and that is the whole point. The glue that translates this into the host's
        /// lifecycle lives in an additively-loaded scene, so it may subscribe long after services
        /// came up. A transient edge would already be gone and the boot would stall with no menu
        /// and no error - which is exactly what happened before this existed.</para>
        /// <para>Sticky is only safe here because this is one event carrying a value, not two
        /// opposing edges. Marking ServicesReady and ServicesUnavailable Sticky would replay both
        /// to a late subscriber, independently, in registration order.</para>
        /// </remarks>
        [EventPayload(typeof(ServicesStatus))]
        [EventDelivery(EventDelivery.Sticky)]
        ServicesStatusChanged = 22,

        /// <summary>Remote configuration has arrived and been applied.</summary>
        RemoteConfigApplied = 30,

        /// <summary>
        /// Difficulty settings fetched from a remote source are available. Payload undeclared:
        /// see the note above.
        /// </summary>
        DifficultySettingsAvailable = 31,
    }
}
