# CrawfisSoftware - Common

Game-neutral pieces shared across CrawfisSoftware Unity projects. Everything here is usable by a
project that has never heard of an endless runner; anything that named a specific game's types
was deliberately left behind.

## What is in it

| Area | Types |
|------|-------|
| `Events/` | `EventChainDispatcher<TSource,TDest>` and `AutoEventFlowBase<TSource,TDest>` - the single dispatch implementation behind every auto-event flow and cross-domain bridge. `EventHistory`. |
| `SceneManagement/` | `LoadSceneAdditively`, `CloseSceneOnEvent`, `FireEventWhenSceneCloses`, `LoadSceneAfterGameControlEvent` - additive scene plumbing driven entirely by events. |
| `Config/` | `DifficultyConfig`. |
| `Utility/` | `Logger`, `TimedEvent`, `TextureExtensions`, `DebugEventFileLogger`. |
| `Test/` | `Test_AutoFireEvent`, `Test_AutoFireEventOnStart`. |

### Chains are a flat list of pairs, not a dictionary

`EventChainDispatcher` maps a source event to **any number** of consequences. A dictionary
allowed exactly one successor each, and that ceiling never produced bugs directly - it produced
workarounds, where a developer finding a source event's slot already taken published the second
consequence by hand inside a controller. Targets fire in declaration order, synchronously.

### DebugEventFileLogger auto-boots, but only where that is safe

It subscribes to every published event and writes them to `debug_event_log.txt`. The auto-boot
is compiled out of release players (`UNITY_EDITOR || DEVELOPMENT_BUILD`), because this type
ships inside a package and would otherwise run in every project that merely references it. In
the editor it writes to the project root; in a development player it writes to
`Application.persistentDataPath`, since the install directory may be read-only. A failure to
open the file is warned about and swallowed - a diagnostic that breaks the game it is
diagnosing is worse than no diagnostic.

## Install

Requires [EventsPublisher](https://github.com/crawfis/EventsPublisher). **UPM does not resolve
git dependencies declared inside a package**, so it cannot be listed in `dependencies` here -
add both lines yourself:

```jsonc
// Packages/manifest.json
"com.crawfissoftware.eventspublisher": "https://github.com/crawfis/EventsPublisher.git",
"com.crawfissoftware.common": "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.common"
```

Assembly: `CrawfisSoftware.Common` (`autoReferenced: true`).

## Licence

CC0-1.0. See [LICENSE.txt](../../LICENSE.txt).
