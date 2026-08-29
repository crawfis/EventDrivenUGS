# Samples~/UGS-Scenes — the scene and prefab handoff

Everything in this file is scene and prefab YAML, which is deliberately not done here. This is the
worked list: what to copy, what each copy must be rewired to, and what must stay behind.

Every target GUID below was read from the package's own `.meta` files and checked. Every source
line number was read from the file it names in `RunnerUGSTemplate`. Nothing here is inferred.

---

## 1. What the sample contains

Eleven assets. Copy each **with its `.meta`** — the GUIDs are cited from other scenes, and losing
them means rewiring 26 more lines of YAML by hand.

**Scenes (8)**, from `RunnerUGSTemplate/Assets/UGS/Scenes/`:

| Source | Why |
|---|---|
| `Boot/UGS_Boot_0_Initialization.unity` | Loads the other four boot scenes by name |
| `Boot/UGS_Boot_1_RemoteConfig.unity` | |
| `Boot/UGS_Boot_2_Authentication.unity` | Hosts the sign-in modal |
| `Boot/UGS_Boot_3_Achievements.unity` | Loads `Achievements` and `AchievementNotifications` |
| `Boot/UGS_Boot_4_Leaderboards.unity` | `_sceneToLoad: Leaderboards` |
| `UGS/Achievements.unity` | |
| `UGS/AchievementNotifications.unity` | |
| `UGS/Leaderboards.unity` | |

**Prefabs (3)**, from `Assets/UGS/Prefabs/UGS/`:

| Prefab | GUID | Instantiated by |
|---|---|---|
| `AchievementsPrefab.prefab` | `180634fb92d23974dbcdc4eaa7bca05e` | Achievements, AchievementNotifications, UGS_Boot_3_Achievements |
| `AchievementsNotificationPrefab.prefab` | `72accb302318b52449d598c7fbff1ad1` | AchievementNotifications |
| `LeaderboardPanel.prefab` | *(new — see §5)* | Leaderboards |

Both existing prefab `.meta` files are clean: a plain `PrefabImporter:` with no `AssetOrigin:`
block, unlike the Blocks leaderboard prefab they sit beside.

### The scene-name contract

The scenes load one another **by name**, as plain strings — so every imported scene must be added
to the consuming project's build profile. A scene loaded by name that is not in the build list
fails at runtime, not at import, which is the worst place to find out.

```
UGS_Boot_0_Initialization  ─┬─ "UGS_Boot_1_RemoteConfig"      (:443)
                            ├─ "UGS_Boot_2_Authentication"    (:415)
                            ├─ "UGS_Boot_3_Achievements"      (:241)
                            └─ "UGS_Boot_4_Leaderboards"      (:550)
UGS_Boot_3_Achievements    ─┬─ "Achievements"                 (:213)
                            ├─ "AchievementNotifications"     (:246)
                            └─ closes "Leaderboards"          (:167)
UGS_Boot_4_Leaderboards    ─── _sceneToLoad: "Leaderboards"   (:171)
```

Put these four names in `Samples~/UGS-Scenes/README.md` as public contract.

---

## 2. GUID rewires

Thirteen references dangle once Blocks is gone. Every other reference in these files resolves
either into one of the three packages (GUIDs were preserved on extraction — verified, zero
mismatches) or into a declared SDK dependency.

### Panel settings — 2 files

`7a38f2dd4a52f3c43802f4f88af54bfe` (Blocks `Common/BlocksPanelSettings.asset`)
→ **`557bff3f5a41b6b3aaffb4f32758faac`** (`Runtime/UI/UgsPanelSettings.asset`)

- `AchievementsPrefab.prefab:85`
- `AchievementsNotificationPrefab.prefab:100`

Also at `UGS_Boot_2_Authentication.unity:218`, which currently points at
`097332d7a39dc7848947505b553aa666` (`Assets/UGS/UI/PS_Login.asset`) — repoint it to the same
`557bff3f…`. `PS_Login.asset` and `UGS Panel Settings.asset` are byte-identical apart from
`m_Name`, and both point at the deleted Blocks theme, so the one package asset replaces both.

### Achievement icons — 6 references

Keep `fileID: 2800000` (the Texture2D main object) unchanged; swap only the GUID.

| Icon | From (Blocks) | To (package) |
|---|---|---|
| `thumbnail` | `9d64ab3d9efdd44d3b35a50db335af8c` | **`270632bccfacba130a6de980c6677a57`** |
| `thumbnail_black` | `b44d5d04e5bc047dd9c197975b3c2b18` | **`3123ed13657d7e53cf3c776941acb64c`** |
| `thumbnail_blue` | `6842d3cc8e24d4f03b10428facaeac46` | **`325c5cc90d62af5e4e7841cbe5d3f612`** |
| `thumbnail_green` | `3429dc3b80e8141d0b696b17d76131c7` | **`6d0ae46d398e6a05b47797b23ec10b53`** |
| `thumbnail_red` | `ec0cde348bd904ba6bf1d22ca12d9348` | **`0c6a4d05039ce6c32525f04b73cb9042`** |

- `AchievementsPrefab.prefab:110-114` — the `m_Icons` array, all five
- `AchievementNotifications.unity:249` — the `m_Icons.Array.data[0]` override (`size` is set to 1
  at :242)

**The art visibly changes.** The package icons are original 128×128 placeholders (~2 KB each); the
Blocks originals are 778×778 renders (247–884 KB). Nothing is derived from them. If the old look
matters, drop in replacement PNGs named **exactly** `thumbnail`, `thumbnail_black`,
`thumbnail_blue`, `thumbnail_green`, `thumbnail_red` — those names are matched against
`Texture2D.name` from definitions that live server-side in Remote Config, so the file name is a
deployed contract, not a local detail.

The icons are **not** auto-loaded. `AchievementIconLibrary` is fed only from the serialized arrays,
so an un-rewired `m_Icons` renders every card and toast iconless with nothing in the console.

### Sign-in UXML — 1 reference

`bb35356fde6fea2479e1bfe9dfd8371f` (`Assets/UGS/UI/PlayerAccountLogin.uxml`)
→ **`a602e73cdd00535a0b297b19c7698d87`** (`Runtime/UI/PlayerAccountLogin.uxml`)

- `UGS_Boot_2_Authentication.unity:219` — keep `fileID: 9197481963319205126`, the constant
  VisualTreeAsset main-object id used by all eight `sourceAsset` references in the repo.

### The runtime theme

`2e74663645b63fe43ad48957a2398ce5` (Blocks `BlocksRuntimeTheme.tss`)
→ **`85a690dbfb5f6df3cd8b3970d3155447`** (`Runtime/UI/Theme/UgsRuntimeTheme.tss`)

The `themeUss` `fileID: -4733365628477956816` is a constant of the `.tss` ScriptedImporter and does
not change. This only matters if you keep a copy of the old panel settings asset; the package's own
`UgsPanelSettings.asset` already points at the right theme.

---

## 3. The lighting reference — 5 scenes

All five shipping boot scenes carry, at **line 97**:

```yaml
  m_LightingSettings: {fileID: 4890085278179872738, guid: 514fa5d4c9ef8e144abb009fe3ef8b55,
    type: 2}
```

That GUID is `Assets/TempleRun/Scenes/Gameplay/TempleRun Lighting Settings.lighting` — a *game*
asset, in the TempleRun domain. In a consumer project it does not exist, so all five imported
scenes open with a broken lighting reference.

Replace both lines with:

```yaml
  m_LightingSettings: {fileID: 0}
```

These are additively-loaded, camera-less service scenes — none of them owns the lighting, so
falling back to Unity's default costs nothing and removes a package-to-game edge. Do it before the
first import: a broken lighting reference is one of the few things that errors on *scene open*
rather than at play time.

This has nothing to do with Blocks. It is a cross-domain edge that survived the extraction
unnoticed.

---

## 4. `Leaderboards.unity` — replace the prefab instance

Lines 184–262 are a `PrefabInstance` of the deleted Blocks
`LeaderboardPrefab.prefab` (`c89b90c1f63b2489789ade666212fb7b`, referenced on 14 lines).

Replace the whole block with an instance of the new `LeaderboardPanel.prefab`, and:

- **Keep the anchor `&1707870260`**, so `SceneRoots` at line 267 needs no edit.
- **Drop** the `m_LeaderboardId` override (:192-196) and the `m_PanelSettings` override (:197-202).
  The new prefab carries both itself.

---

## 5. The new `LeaderboardPanel.prefab`

`LeaderboardPanel` is code now, but it is a *component* — these serialized fields have to be wired
by hand, and their **names are the contract** between this prefab and the C#:

| Field | Wire to |
|---|---|
| `_panel` | the `PanelRenderer` on the same GameObject |
| `_styleSheets` | `[0]` `Runtime/UI/Theme/UgsCore.uss` (the design tokens), `[1]` `Runtime/UI/Theme/UgsComponents.uss` (the leaderboard rules) |
| `_leaderboardId` | `DailyDistance` |
| `_tierId` | empty — empty means the global board |
| `_title` | `LEADERBOARD` |
| `_numberToDisplay` | `25` |
| `_playerRangeLimit` | `5` |
| `_showPlayerTab` | true |
| `_refreshOnStart` | true |
| `_scoreFormat` | `N0` |

Order matters for `_styleSheets`: `UgsComponents.uss` uses the `--ugs-*` custom properties that
`UgsCore.uss` declares.

The same applies to the two achievement prefabs: their `PanelRenderer` and icon arrays are
serialized, which is why they are in the sample rather than in `Runtime/`.

---

## 6. What stays behind

**The four `Scenes/Test/` scenes.** They look like a natural addition — the README's replaceability
story leans on the UGS-only build profile — but each depends on assets that stay in the game repo:

- `0_BootStrap_UGS_Only.unity` references five GameFlow scripts, none of them in any package:
  `GameState.cs` (:194), `EventLoggerDump.cs` (:323), `UnloadNonActiveScenes.cs` (:550),
  `QuitController.cs` (:759), `GameFlowAutoEventFlow.cs` (:881).
- `DummyGame_Boot_0_Initialization.unity:149` and `Test_SubmitScoreAndEnd.unity:148` both reference
  `Assets/UGSGlue/Test_SubmitLeaderboardScore.cs` — the game-side glue that deliberately stayed
  behind.

A UGS-only harness needs a game-side dummy and a bootstrap, which is exactly the composition the
package declines to own. Say so in the sample README so nobody re-adds them.

**`0_BootStrap.unity`**, by policy. A bootstrap composes an *application* — it owns game state,
quit handling and scene teardown. That is the project's job.

**`Assets/UGS/UI/PS_Login.asset`, `UGS Panel Settings.asset`, `PlayerAccountLogin.uxml`/`.uss`** —
all superseded by the package copies, per §2.

---

## 7. Unrelated: a missing script already in the game repo

`Assets/UGS/Scenes/Test/UGS_Boot_0_Test_Init_UGS_Only.unity:164` carries

```yaml
  m_Script: {fileID: 11500000, guid: 3ede6e1f19f81d145a572506d7e6e298, type: 3}
  m_EditorClassIdentifier: Assembly-CSharp::CrawfisSoftware.UGS.Events.DEBUG_UGSOnlyEventFlow
```

That GUID resolves to nothing — it appears in no `.meta` anywhere, and `DEBUG_UGSOnlyEventFlow`
has no source anywhere under `Assets/`. The script was deleted and the scene still carries the
component.

This predates the extraction and has nothing to do with packaging. Either delete the block at
lines 155-166 and its entry in the owning GameObject's `m_Component` list, or restore the script if
it went by accident.
