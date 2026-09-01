# CrawfisSoftware - Unity Gaming Services

A UGS integration expressed entirely as events. The game publishes what happened; this package
turns that into service calls and publishes what came back. Neither side references the other's
types.

```
  game events   <->   GameServiceEvents   <->   UGS events
  (your glue)             (contract)          (this package)
```

The point is **replaceability**. A game that talks only through `GameServiceEvents` can run with
this package absent - the sibling project ships a build profile that does exactly that - and this
package can run against a dummy game with random scores. Removing a domain means not loading its
scenes, not editing the other side.

## What is in it

| Area | What it does |
|------|--------------|
| `Events/` | `UGS_EventsEnum`, the auto-event flow, and `GameServiceEventsUGSBridge` - the only place UGS events and `GameServiceEvents` are named together. |
| `Initialization/` | `PlayerAuthenticationManager`, `UGS_State`, connectivity handling. |
| `Authentication/` | `PlayerSignIn` (the modal element) and `PlayerSignInController`. Anonymous, Unity Player Account, or username/password. |
| `RemoteConfig/` | The one config fetch, and the difficulty table it publishes to the contract. |
| `Leaderboard/` | `LeaderboardQuery` (reads), `LeaderboardPanel` (the display), `LeaderboardPlayerController` (score submission). |
| `Achievements/` | Model, service and UI. Two interchangeable backends - see below. |
| `Economy/` | `PlayerCurrencyManager` - the player's lifetime soft-currency balance. Two interchangeable backends, as achievements has. |
| `UI/` | The runtime theme, the panel settings, and the achievement icons. |
| `Editor/` | `AchievementDefinitionCatalog` and its exporter - authoring only, excluded from player builds. |

## Install

Four git URLs. **UPM does not resolve git dependencies declared inside a package**, so this
package cannot pull its three siblings in for you - list all four yourself:

```jsonc
// Packages/manifest.json
"com.crawfissoftware.eventspublisher": "https://github.com/crawfis/EventsPublisher.git",
"com.crawfissoftware.contracts": "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.contracts",
"com.crawfissoftware.common":    "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.common",
"com.crawfissoftware.ugs":       "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.ugs"
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

#### The module contract

The package calls your module by name, so it cannot check the shape at compile time. A conforming
module publishes four functions - the names are whatever you put in
`AchievementsService.Instance.CloudCodeEndpoints`, and these are the defaults:

| Function | Arguments | Returns |
|---|---|---|
| `GetAchievements` | `playerId` (string) | array of `{ definition, record }` |
| `UnlockAchievement` | `achievementId` (string) | one record |
| `UpdateAchievementProgress` | `achievementId` (string), `progressCount` (int) | one record |
| `ResetAllAchievements` | none | nothing |

A *record* is `{ "Id": string, "Unlocked": bool, "ProgressCount": int }`; a *definition* is the same
six fields as the Remote Config entry below.

Two things are easy to get wrong and fail silently:

- **`UpdateAchievementProgress` must assign, not accumulate.** `IAchievementBackend.SetProgressAsync`
  is specified as absolute progress and the Cloud Save backend implements it that way. A module that
  adds instead makes progress grow quadratically when a caller reports a running total, which is the
  natural reading of "set progress".
- `UnlockAchievement` and `UpdateAchievementProgress` take the player from the execution context;
  only `GetAchievements` is passed a player id.

There is **no default module name**, on purpose: this package ships no module, and a placeholder
would fail per call at runtime against something that does not exist. Setting `UseTrustedClient`
without configuring `CloudCodeEndpoints` throws at construction with a message naming what to set.

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

Create an **AchievementDefinitionCatalog** (`Assets > Create > CrawfisSoftware > UGS > Achievement Definitions`)
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

The scenes ship as a Package Manager **sample** - a sample is copied into your `Assets/` with its
`.meta` files, so GUIDs survive and the scenes stay editable. Import "UGS Boot Scenes" from the
package's page in Package Manager, then read the README that comes with it.

Every reference in those eight scenes and three prefabs resolves against these packages alone -
verified by opening all eleven in a project containing nothing else and requiring Unity to report
zero unresolved references. How they were rebuilt off the vendored assets they used to depend on is
[docs/samples-handoff.md](../../docs/samples-handoff.md).

**The scenes load one another by name, so those names are public contract**, and the imported
scenes must be added to your build profile - a scene loaded by name that is not in the build list
fails at runtime, not at import.

`LeaderboardPanel` and the achievement panels are components, not prefabs: their serialized
`PanelRenderer` and `StyleSheet[]` have to be wired in a scene or prefab, which is what the sample
scenes carry.

`0_BootStrap` is deliberately not included. A bootstrap composes an *application* - it owns game
state, quit handling and scene teardown - and that is the project's job, not a package's.

## Currency: two backends, one interface

`ICurrencyBackend` has two implementations, chosen by the `Use Trusted Client` checkbox on the
`PlayerCurrencyController` component:

- **`EconomyCurrencyBackend`** (default) credits and debits the player's own balance through the
  Economy service. Needs no module and no deploy. The device decides - fine while coins buy only
  single-player progression.
- **`CloudCodeCurrencyBackend`** routes the move through a Cloud Code module, which performs the
  write with the module's *service* token. That is what keeps it working under an access policy
  denying players direct writes to Economy. A reference module is in `CloudCode~/CurrencyModule`;
  as with achievements there is **no default module name**, and the backend throws at construction
  rather than failing per call against a module that does not exist.

Note what the trusted path does **not** buy you: the amount still comes from the client. A module
can only check it against state the server holds on its own, and this one holds none - it bounds
the amount per call and no more. "Goes through Cloud Code" is not the same as "cannot be forged".

| Function | Arguments | Returns |
|---|---|---|
| `GetCurrencyBalance` | `currencyId` (string) | `{ "CurrencyId": string, "Balance": long }` |
| `AddCurrency` | `currencyId` (string), `amount` (int, negative to spend) | the same shape |

`PlayerCurrencyManager.DefaultCurrencyId` is `"COIN"`. The name **must match exactly** a
currency you created in the Unity Dashboard under Economy > Currencies, and nothing checks it at
compile time. A wrong id surfaces as `CurrencyBackendException.IsCurrencyNotFound` on the first
call, rather than as a balance that silently stays at zero.

### Two things must be true before a single coin can bank

Neither is a compile error, and neither shows up in a survey of this package on its own.

1. **`PlayerCurrencyController` must be in a loaded scene.** It is the *only* subscriber to
   `UGS_EventsEnum.CurrencySyncRequested`. Leave it out and every hop up to that signal still fires,
   so an event log looks healthy right up to the point where the sync is received by nobody and
   nothing is written. Put it beside `GameServiceEventsUGSBridge`, as the `UGS Boot Scenes` sample does.
2. **The currency must exist in the environment you sign in to.** Economy configuration is
   per-environment, so one published to `production` does not exist in `initial-development`.
   Create it in the Dashboard under Economy > Currencies, or commit a deployment resource and push
   it from the Deployment window - a file named `COIN.ecc` whose id comes from the filename:

   ```json
   {
     "$schema": "https://ugs-config-schemas.unity3d.com/v1/economy/economy-currency.schema.json",
     "name": "Coin",
     "initial": 0
   }
   ```

   A missing currency is reported as `CurrencyBackendException.IsCurrencyNotFound`, which
   `PlayerCurrencyManager` turns into the warning "the currency id does not exist in this project's
   Economy configuration" - deliberately, so it cannot be mistaken for an access-policy problem.

### How a coin becomes a lifetime balance

The game publishes `GameServiceEvents.CurrencyTotalChanged` carrying its **running session
total**, not a delta, and zeroes it at the end of each run. `PlayerCurrencyController` remembers
that number and banks it once, when `GameServiceEvents.SessionEnding` arrives - so a run costs one
balance write rather than one per coin. The resulting lifetime balance is published as
`UGS_EventsEnum.CurrencyBalanceChanged`, carrying a `CurrencyBalanceUpdate`, which is what
`CoinBasedAchievements` reads. That payload says whether the balance came from reading the store
or from a write, because only a read may be used as a baseline - see the class remarks.

A run abandoned by killing the application loses that run's coins. That is the cost of banking
per run rather than per pickup.

## Known gaps

- Remote Config carries only the difficulty table. `RemoteConfigManager` publishes
  `UGS_EventsEnum.DifficultySettingsFetched` - and so `GameServiceEvents.DifficultySettingsAvailable` -
  when the environment defines a `difficulty_settings` key, and stays silent when it does not, so a
  game keeps its own configs. Any other key is yours to read: subscribe to `RemoteConfigUpdated` and
  take it off `RemoteConfigService.Instance.appConfig`.
- `CoinBasedAchievements` is not placed in any sample scene, because none of the achievement
  definitions this project ships is coin-based. Add it beside `DistanceBasedAchievements` in
  `AchievementNotifications` once you have coin achievements to bind its threshold list to.
- Most of this package has now been exercised against live Unity Gaming Services. Boot,
  initialization, authentication, leaderboard submission, Cloud Save achievements and the currency
  path end to end all run: a play session banked a run's coins into an Economy `COIN` balance and
  the dashboard agreed with the count. **Still not run:** the two Cloud Code backends, which need a
  deployed module. Compiling was never the hard part - the first play session found three defects
  a build could not have shown, all silent, and the session that closed the currency path found a
  fourth thing a build cannot show at all: a component that compiles, ships, and is simply never
  placed in a scene.

## Licence

CC0-1.0. See [LICENSE.txt](../../LICENSE.txt). This package contains no third-party content.
