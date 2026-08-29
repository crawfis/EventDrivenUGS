# CrawfisSoftware - Unity Gaming Services

A UGS integration expressed entirely as events. The game publishes what happened; this package
turns that into service calls and publishes what came back. Neither side references the other's
types.

```
  game events   <->   GameSignals   <->   UGS events
      (your glue)      (contract)      (this package)
```

The point is **replaceability**. A game that talks only through `GameSignals` can run with this
package absent - the sibling project ships a build profile that does exactly that - and this
package can run against a dummy game with random scores. Removing a domain means not loading its
scenes, not editing the other side.

## What is in it

| Area | What it does |
|------|--------------|
| `Events/` | `UGS_EventsEnum`, the auto-event flow, and `GameSignalsUGSBridge` - the only place UGS events and `GameSignals` are named together. |
| `Initialization/` | `PlayerAuthenticationManager`, `UGS_State`, connectivity handling. |
| `Authentication/` | `PlayerSignIn` (the modal element) and `PlayerSignInController`. Anonymous, Unity Player Account, or username/password. |
| `RemoteConfig/` | Config fetch plus typed views over it: game balance, feature flags, campaign events, difficulty. |
| `Leaderboard/` | `LeaderboardQuery` (reads), `LeaderboardPanel` (the display), `LeaderboardPlayerController` (score submission). |
| `Achievements/` | Model, service and UI. Two interchangeable backends - see below. |
| `UI/` | The runtime theme, the panel settings, and the achievement icons. |
| `Editor/` | `AchievementCatalog` and its exporter - authoring only, excluded from player builds. |

## Install

Four git URLs. **UPM does not resolve git dependencies declared inside a package**, so this
package cannot pull its three siblings in for you - list all four yourself:

```jsonc
// Packages/manifest.json
"com.crawfissoftware.eventspublisher": "https://github.com/crawfis/EventsPublisher.git",
"com.crawfissoftware.contracts": "https://github.com/crawfis/CrawfisSoftware.UnityPackages.git?path=/Packages/com.crawfissoftware.contracts",
"com.crawfissoftware.common":    "https://github.com/crawfis/CrawfisSoftware.UnityPackages.git?path=/Packages/com.crawfissoftware.common",
"com.crawfissoftware.ugs":       "https://github.com/crawfis/CrawfisSoftware.UnityPackages.git?path=/Packages/com.crawfissoftware.ugs"
```

Everything else - Core, Authentication, Cloud Save, Cloud Code, Leaderboards, Remote Config,
Newtonsoft - is a registry dependency and resolves on its own.

Then, in the Unity Services dashboard: link the project, and deploy the achievement definitions to
Remote Config under the key `achievements` (see **Achievements** below). Nothing else is required
to compile or enter Play mode.

## Achievements: two backends, one interface

`IAchievementBackend` has two implementations, chosen by the `UseTrustedClient` checkbox on the
achievements prefab:

- **`CloudSaveAchievementBackend`** (default) reads definitions from Remote Config and keeps
  records in the player's own Cloud Save. The device decides what was earned - fine for
  single-player progression, wrong for anything competitive.
- **`CloudCodeAchievementBackend`** routes every read and write through a Cloud Code module, so the
  server decides. Endpoint names come from `CloudCodeAchievementEndpoints`, which is data rather
  than constants - point it at whatever module you actually deployed.

This package deliberately does **not** use generated Cloud Code bindings. They are emitted into
your project's own `Assets/` folder under a fixed assembly name, and a package assembly cannot
reference an assembly that lives there - a package that depended on them could not compile at all.
The trusted backend calls `CallModuleEndpointAsync` with DTOs declared here instead.

### Achievement definitions

Definitions live in Remote Config under the key `achievements`, as a JSON array:

```json
[
  { "Id": "first_achievement", "Icon": "thumbnail_green", "Title": "First Steps",
    "Description": "Travel your first 100 metres.", "IsHidden": false, "ProgressTarget": 1 }
]
```

Two things there are contracts that fail **silently** if broken:

- The six field names are the JSON keys. Rename one and that field arrives empty - an achievement
  loses its title, and nothing is logged.
- `Icon` is matched against `Texture2D.name`, not a path or a GUID. The package ships
  `thumbnail`, `thumbnail_blue`, `thumbnail_green`, `thumbnail_red` and `thumbnail_black`
  (the fallback). Supply your own through the `m_Icons` list on the achievements prefab; they merge
  with the built-in set rather than replacing it.

### Authoring them

Create an **AchievementCatalog** (`Assets > Create > CrawfisSoftware > UGS > Achievement Catalog`)
in your own project, ideally under an `Editor/` folder so it stays out of player builds. Edit the
definitions in the Inspector, then press **Export to Remote Config (.rc)**. Point the Deployment
window at the written file and deploy it.

The catalog is authoring data only - nothing loads it at runtime, and it never ships. Definitions
reach the game exclusively from Remote Config, which is why the export step is the one that
matters.

Two things the exporter does deliberately:

- It writes **`.rc`**, the extension `com.unity.remote-config` already claims, so the Deployment
  window lists the file without this package registering an importer - and therefore without an
  extension for two packages to fight over. The envelope is identical to the one the vendored
  stack wrote under its own `.ach` extension, so **Import from JSON...** reads an existing `.ach`
  unchanged.
- It **fails loudly**. Blank, whitespace-bearing and duplicate ids are reported before anything is
  written, and a failed write reports the failure rather than clearing the dirty flag and claiming
  success.

## Scenes

The scenes ship as a **sample**, not in `Runtime/` - Package Manager copies a sample into your
`Assets/` with its `.meta` files, so GUIDs survive and the scenes stay editable. Import "UGS Boot
Scenes" from the package's page in Package Manager.

**The scenes load one another by name, so those names are public contract**, and the imported
scenes must be added to your build profile - a scene loaded by name that is not in the build list
fails at runtime, not at import.

`LeaderboardPanel` and the achievement panels are components, not prefabs: their serialized
`PanelRenderer` and `StyleSheet[]` have to be wired in a scene or prefab, which is what the sample
scenes carry.

`0_BootStrap` is deliberately not included. A bootstrap composes an *application* - it owns game
state, quit handling and scene teardown - and that is the project's job, not a package's.

## Licence

CC0-1.0. See [LICENSE.txt](../../LICENSE.txt). This package contains no third-party content.
