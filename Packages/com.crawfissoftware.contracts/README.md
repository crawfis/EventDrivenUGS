# CrawfisSoftware - Game/Service Contracts

`GameSignals` is the vocabulary a game and a backing services layer use to talk to each other,
in terms **neither of them owns**.

Without it, a services layer ends up naming the game's events - a UGS layer subscribing to
`UGS_CoinUpdated`, an event that exists only because *this* game has coins - and the game ends
up naming the service's. Either way, one side cannot be replaced without editing the other.

Both sides map into this enum instead. A game publishes `ScoreUpdated` however it computes
score; the services layer consumes it without knowing whether that score is metres run, puzzles
solved, or laps completed.

## What is in it

Two types, and deliberately nothing else:

| Type | Purpose |
|------|---------|
| `GameSignals` | The enum. Game -> services: `ScoreUpdated`, `CurrencyTotalChanged`, `SessionEnding`, `SessionEnded`. Services -> game: `ServicesReady`, `ServicesUnavailable`, `ServicesStatusChanged`, `RemoteConfigApplied`, `DifficultySettingsAvailable`. |
| `ServicesStatus` | `Connecting` / `Ready` / `Unavailable`. Three values rather than a bool, because "not ready" is two different situations to a player: still trying, and gave up. |

Every member is a crossing someone maintains forever, so the enum carries only what a service
genuinely needs. Anything specific to one game belongs in that game's own domain, translated
into these signals by per-game glue.

`ServicesStatusChanged` is `Sticky` on purpose: the glue that translates it into a host's
lifecycle lives in an additively-loaded scene and may subscribe long after services came up. A
transient edge would already be gone, and the boot would stall with no menu and no error.

## Install

This package assumes [EventsPublisher](https://github.com/crawfis/EventsPublisher), which is
distributed by git URL. **UPM does not resolve git dependencies declared inside a package**, so
it cannot be listed in this package's `dependencies` - you must add both lines yourself:

```jsonc
// Packages/manifest.json
"com.crawfissoftware.eventspublisher": "https://github.com/crawfis/EventsPublisher.git",
"com.crawfissoftware.contracts": "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.contracts"
```

Assembly: `CrawfisSoftware.Contracts` (`autoReferenced: true`, so code with no asmdef of its own
can see it).

## Licence

CC0-1.0. See [LICENSE.txt](../../LICENSE.txt).
