# How the sample was rebuilt

`Samples~/UGS-Scenes` is **done**. Eight scenes and three prefabs, every reference resolving against
the three packages alone — verified by opening all eleven in a project containing nothing but the
packages and requiring Unity to report zero unresolved references and zero missing scripts.

This was originally a list of YAML edits to do by hand. It is now a record of the pipeline that did
them, because the pipeline is what matters when the source scenes change again.

---

## Why a script rather than hand edits

Two constraints shaped it:

- **`Samples~` is invisible to Unity.** A tilde folder is excluded from the asset database, so Unity
  cannot open, fix or validate anything inside it. The work has to happen in a folder Unity can see
  and be copied out afterwards.
- **A GUID substitution alone proves nothing.** Text surgery on `.unity` files is only defensible if
  something afterwards checks that every reference now resolves. Unity is that something.

So: stage the assets in a throwaway project that has the packages and nothing else, let Unity report
what dangles, fix it, and make Unity re-report until the count is zero.

## The pipeline

The three scripts are in [`Tools~/SamplesBuilder/`](../Tools~/SamplesBuilder) — a tilde folder, so
they ship in git but never reach a consumer's Unity.

| Step | Tool | What it does |
|---|---|---|
| 1 | *(copy)* | Stage the 8 scenes and 2 prefabs, **with their `.meta` files**, into `Assets/UGS-Scenes/` of a scratch project whose manifest holds only the three packages |
| 2 | `SamplesAudit.cs` | Open every scene and prefab; report each component whose script is gone and each object reference that no longer resolves |
| 3 | `remap_guids.py` | Rewrite the dangling GUIDs to their package equivalents, and clear the lighting reference |
| 4 | `BuildLeaderboardPanel.cs` | Build `LeaderboardPanel.prefab` and swap it in for the vendored prefab instance |
| 5 | `SamplesAudit.cs` | Re-run. **Required result: zero.** |
| 6 | *(copy)* | Copy the fixed tree into `Samples~/UGS-Scenes/`, add the sample README, re-declare `samples[]` |

Run steps 2, 4 and 5 with
`Unity.exe -batchmode -nographics -quit -projectPath <scratch> -logFile <log> -executeMethod <Class>.<Method>`.

### What the audit measured

| | Before | After |
|---|---|---|
| Missing scripts | 0 | 0 |
| Unresolved references | **30** | **0** |

Thirty, not the thirteen a by-hand GUID count suggests — because a prefab *instance* in a scene
carries its own overrides, and each dangling override is a separate site. Fixing the prefab asset
alone would have left the scene instances broken, and that is exactly the kind of thing a manual
list gets wrong.

---

## The GUID map

Kept because it is the reference if any of this is ever redone by hand. Every target was read from
the package's own `.meta` file.

| What | From | To |
|---|---|---|
| Panel settings (Blocks) | `7a38f2dd4a52f3c43802f4f88af54bfe` | `557bff3f5a41b6b3aaffb4f32758faac` |
| Panel settings (`PS_Login`) | `097332d7a39dc7848947505b553aa666` | ↑ same |
| Panel settings (`UGS Panel Settings`) | `4c8011be1283eb042ad4a0e7a6f30d4a` | ↑ same |
| Sign-in UXML | `bb35356fde6fea2479e1bfe9dfd8371f` | `a602e73cdd00535a0b297b19c7698d87` |
| `thumbnail` | `9d64ab3d9efdd44d3b35a50db335af8c` | `270632bccfacba130a6de980c6677a57` |
| `thumbnail_black` | `b44d5d04e5bc047dd9c197975b3c2b18` | `3123ed13657d7e53cf3c776941acb64c` |
| `thumbnail_blue` | `6842d3cc8e24d4f03b10428facaeac46` | `325c5cc90d62af5e4e7841cbe5d3f612` |
| `thumbnail_green` | `3429dc3b80e8141d0b696b17d76131c7` | `6d0ae46d398e6a05b47797b23ec10b53` |
| `thumbnail_red` | `ec0cde348bd904ba6bf1d22ca12d9348` | `0c6a4d05039ce6c32525f04b73cb9042` |
| Runtime theme | `2e74663645b63fe43ad48957a2398ce5` | `85a690dbfb5f6df3cd8b3970d3155447` |

The three panel-settings GUIDs collapse onto one asset: `PS_Login.asset` and
`UGS Panel Settings.asset` were byte-identical apart from `m_Name`, and both pointed at the deleted
Blocks theme.

`fileID`s were not touched. `2800000` (Texture2D), `11400000` (ScriptableObject) and
`9197481963319205126` (VisualTreeAsset) are main-object ids, constant across assets of a type.

## The lighting reference

All five boot scenes carried `m_LightingSettings` pointing at
`Assets/TempleRun/Scenes/Gameplay/TempleRun Lighting Settings.lighting` — a *game* asset, in a
domain the package has nothing to do with. In a consumer project it does not exist, and a broken
lighting reference errors on **scene open**, not at play time.

Now `{fileID: 0}`. These are camera-less additive service scenes that own no lighting, so Unity's
default is correct and the package-to-game edge is gone.

Nothing to do with Blocks. It simply survived the extraction unnoticed.

## The leaderboard prefab

`LeaderboardPanel.prefab` is built by script, not by hand, and ships configured for the board these
scenes were authored against:

| Field | Value | Why |
|---|---|---|
| `_leaderboardId` | `DailyDistance` | from the old prefab instance override |
| `_tierId` | `weekly_distance_tier_1` | from `LeaderboardController._tier` |
| `_numberToDisplay` | `10` | from `LeaderboardController._numberToDisplay` |
| `_styleSheets` | `UgsCore.uss`, `UgsComponents.uss` | order matters — Components uses tokens Core declares |

Those last two came off fields the extraction deleted from `LeaderboardController`, where nothing
read them. Carrying them over is the point: leaving them unset renders the global top-25 instead of
the authored weekly tier, and nothing says so.

---

## What is deliberately not in the sample

**The four `Scenes/Test/` scenes.** Each depends on assets that stay in the game repo:
`0_BootStrap_UGS_Only.unity` references five GameFlow scripts, and
`DummyGame_Boot_0_Initialization.unity` and `Test_SubmitScoreAndEnd.unity` both reference
`Assets/UGSGlue/Test_SubmitLeaderboardScore.cs`. A UGS-only harness needs a game-side dummy and a
bootstrap — exactly the composition the package declines to own.

**`0_BootStrap.unity`**, by policy. A bootstrap composes an application.

---

## Still yours

**The achievement art changes.** The package ships original 128×128 placeholder badges; the vendored
originals were 778×778 renders. Nothing is derived from them — that was the point. If the old look
matters, replace the textures keeping the names `thumbnail`, `thumbnail_black`, `thumbnail_blue`,
`thumbnail_green`, `thumbnail_red`, because those names are matched against `Texture2D.name` from
definitions that live server-side in Remote Config.

**A missing script in `RunnerUGSTemplate`, unrelated to any of this.**
`Assets/UGS/Scenes/Test/UGS_Boot_0_Test_Init_UGS_Only.unity:164` carries a MonoBehaviour whose
script GUID `3ede6e1f19f81d145a572506d7e6e298` resolves to nothing anywhere, for a class
`DEBUG_UGSOnlyEventFlow` with no source anywhere under `Assets/`. It predates the extraction. Either
delete the block at lines 155-166 and its entry in the owning GameObject's `m_Component` list, or
restore the script if it went by accident.

**The cutover.** `RunnerUGSTemplate` is still untouched on `main`; pointing it at these packages
instead of `Assets/UGS` is a separate PR.
