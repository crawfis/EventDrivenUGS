# UGS Boot Scenes

The additive scenes that drive the UGS integration, plus the three panel prefabs they instantiate.

Everything here resolves against the three packages alone. No reference points back at the project
these scenes came from — that was checked by opening every scene and prefab in a project containing
nothing but the packages, and requiring Unity to report zero unresolved references.

## Add them to your build profile

**The scenes load one another by name.** Those names are a contract, and a scene loaded by name that
is not in the build list fails at runtime, not at import — which is the worst place to find out.

```
UGS_Boot_0_Initialization  ─┬─ "UGS_Boot_1_RemoteConfig"
                            ├─ "UGS_Boot_2_Authentication"
                            ├─ "UGS_Boot_3_Achievements"
                            └─ "UGS_Boot_4_Leaderboards"
UGS_Boot_3_Achievements    ─┬─ "Achievements"
                            ├─ "AchievementNotifications"
                            └─ closes "Leaderboards"
UGS_Boot_4_Leaderboards    ─── "Leaderboards"
```

Renaming an imported scene breaks the load that names it. Rename both ends together, or neither.

## What each scene is

| Scene | Role |
|---|---|
| `UGS_Boot_0_Initialization` | Entry point. Initializes services, then loads the other four. Also hosts `PlayerCurrency` - see below. |
| `UGS_Boot_1_RemoteConfig` | Fetches Remote Config. |
| `UGS_Boot_2_Authentication` | Hosts the sign-in modal. |
| `UGS_Boot_3_Achievements` | Loads the achievements panel and the unlock toast. |
| `UGS_Boot_4_Leaderboards` | Owns the open/close flow for the leaderboard scene. |
| `Achievements` | The achievements grid. |
| `AchievementNotifications` | The unlock toast. |
| `Leaderboards` | The leaderboard panel. |

There is deliberately **no `0_BootStrap`**. A bootstrap composes an *application* — it owns game
state, quit handling and scene teardown — and that is your project's job, not a package's. These
scenes expect something to load `UGS_Boot_0_Initialization` additively.

## The persistent coin balance

`UGS_Boot_0_Initialization` carries a `PlayerCurrency` object holding a `PlayerCurrencyController`.
It banks each run's coins into the player's lifetime Economy balance when the run ends, and that
lifetime balance is what `CoinBasedAchievements` reads.

| Field | Value |
|---|---|
| `_currencyId` | `COIN` - **must match a currency you have created** in the Unity Dashboard under Economy > Currencies. Nothing checks this at compile time. |
| `_useTrustedClient` | off - the client writes its own balance. Appropriate while coins buy only single-player progression. |
| `_cloudCodeModuleName` | empty - only read when the trusted client is on. See `CloudCode~/CurrencyModule` in the repository. |

Coins are banked once per run, not once per pickup, so a run abandoned by killing the application
loses that run's coins. Crediting more often trades network calls for that.

## The panel prefabs

Each is a `PanelRenderer` plus one component. The serialized field names are the contract between
the prefab and the C#, so re-wiring them by hand is fine but renaming them is not.

`LeaderboardPanel.prefab` ships configured for the board these scenes were authored against:

| Field | Value |
|---|---|
| `_leaderboardId` | `DailyDistance` |
| `_tierId` | empty — reads the global board. Set it only if you actually created tiers on this board; a tier id that does not exist fails the read rather than falling back. |
| `_numberToDisplay` | `10` |
| `_styleSheets` | `UgsCore.uss`, then `UgsComponents.uss` — order matters, Components uses tokens Core declares |

Point `_leaderboardId` at your own leaderboard, and clear `_tierId` unless yours is tiered.

## The achievement icons

The two achievement prefabs carry the package's own placeholder icons — flat 128×128 badges named
`thumbnail`, `thumbnail_black`, `thumbnail_blue`, `thumbnail_green` and `thumbnail_red`.

Those **names are a deployed contract**: `AchievementIconLibrary` matches them against
`Texture2D.name` using the `Icon` field of definitions that live server-side in Remote Config. To
use your own art, keep the names and replace the textures, or add yours to the `m_Icons` array — the
library merges rather than replaces, so both sets coexist.

An empty `m_Icons` renders every card and toast iconless, with nothing in the console.
