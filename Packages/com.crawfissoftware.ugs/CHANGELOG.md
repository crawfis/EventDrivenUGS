# Changelog

All notable changes to this package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2026-08-31

### Changed

- **Breaking.** `RemoteConfigManager` now publishes `UGS_EventsEnum.DifficultySettingsFetched`
  itself, from the response it already fetched, so the difficulty table finally reaches a game.
  Nothing published that event before: it came only from `DifficultyObserver`, which no code in
  this package ever constructed. A missing `difficulty_settings` key is not an error - the manager
  logs and stays silent, leaving the game's own configs standing.

- `color` no longer sits in the universal selector in `UgsCore.uss`. It is declared once on
  `:root` and reaches text by inheritance, so a colour set on a container now flows into the text
  inside it - a universal rule matched that text directly and won every time. The six elements that
  were living on the universal rule (achievement card title, leaderboard title and its three row
  labels, sign-in header) were given the identical colour explicitly, and `UgsSignIn.uss` gained a
  container-level default for the case where `PlayerAccountLogin.uxml` links `UgsCore.uss` on its
  own, below the root, where `:root` is not in scope. No colour on screen changes.

### Removed

- **Breaking.** `DifficultyObserver` and its `ServiceObserverHelpers`. The observer waited for
  authentication and then made a *second* Remote Config round trip for a payload the manager's
  fetch had already downloaded. Folding the one useful line into `RemoteConfigManager` removes the
  duplicate fetch along with the type. A host that constructed it by hand - none could, it was
  internal to the package's own flow - now needs nothing.
- **Breaking.** `GameBalance`, `GameBalanceManager`, `FeatureFlags`, `FeatureFlagsManager`,
  `CampaignEventConfig` and `CampaignEventConfigManager`. All six were public and complete, and all
  six were constructed only from commented-out code. They were also unreachable by design: a game
  cannot reference this assembly, so a typed view held on a manager in here has no way of crossing
  to the game - only an event on the contract does. Reaching them would have meant inventing
  contract vocabulary for daily rewards, video ads, multiplayer and campaign events, none of which
  the reference game has. A host that wants those keys reads them from
  `RemoteConfigService.Instance.appConfig` after `RemoteConfigUpdated`, in its own types.

## [0.4.0] - 2026-08-31

### Added

- `GameServiceEventsUGSBridge` now forwards the banked balance to the contract, publishing
  `GameServiceEvents.CurrencyBalanceChanged` whenever `UGS_EventsEnum.CurrencyBalanceChanged`
  fires. The number is unwrapped by hand rather than declared as a dispatcher pair: the
  dispatcher forwards a payload untouched, and this one is a `CurrencyBalanceUpdate` - a type in
  this package - so forwarding it would hand a game a services type to cast.

  Until now the balance was reachable only by `CoinBasedAchievements` inside this package, which
  meant a host had no supported way to display it.

## [0.3.0] - 2026-08-31

### Changed

- **Breaking.** Follows the contracts rename of `GameSignals` to `GameServiceEvents`.
  `GameSignalsUGSBridge` is now `GameServiceEventsUGSBridge`, in the same namespace and file
  location. Behaviour is identical - the same mappings, the same hand-published
  `ServicesStatus` level, the same members on both enums.
- Requires `com.crawfissoftware.contracts` 0.3.0 or later. The two move together: this bridge is
  the one place that names the contract enum, so an older contracts package will not compile
  against this release.

### Notes

- The bridge's `.meta` GUID is unchanged, so a `GameSignalsUGSBridge` component already placed
  in a scene - including the one in the `UGS Boot Scenes` sample - rebinds to the renamed class
  with no scene edit. Unity resolves MonoBehaviours by script GUID, not by type name.
- The sample scene's GameObject is still *named* `GameSignalsUGSBridge` and its
  `m_EditorClassIdentifier` still reads `Assembly-CSharp::...GameSignalsUGSBridge`. Both are
  cosmetic and deliberately left: that identifier was already stale before this change - the
  class has lived in the `CrawfisSoftware.UGS` assembly since extraction, not `Assembly-CSharp`
  - which is the evidence that Unity does not read it.

## [0.2.0] - 2026-08-30

### Added

- `Economy/` - `PlayerCurrencyManager`, a lifetime soft-currency balance with the same
  two-backend shape as achievements: `EconomyCurrencyBackend` (client-authoritative, via the
  Economy service) and `CloudCodeCurrencyBackend` (server-authoritative), chosen by a
  `UseTrustedClient` flag. `PlayerCurrencyController` configures it from a scene and banks each
  run's coins into the balance when the run ends.
- `UGS_EventsEnum.CurrencySyncRequested`, `CurrencyBalanceChanged` (payload:
  `CurrencyBalanceUpdate`, carrying the balance and whether it came from a read or a write) and
  `CurrencySyncFailed`.
- `com.unity.services.economy` is now a declared dependency, and `Unity.Services.Economy` an
  assembly reference.
- The `UGS Boot Scenes` sample gains a `PlayerCurrency` object in `UGS_Boot_0_Initialization`.

### Fixed

- The sign-in modal now renders. `PlayerAccountLogin.uxml` was not well-formed XML - a comment
  wrote a custom-property prefix literally, and a double hyphen is illegal inside an XML
  comment - so Unity imported it to an EMPTY `VisualTreeAsset`, reporting nothing at import or
  at run time. The panel was built full-size and unhidden with zero children in it, and
  `root.Q<PlayerSignIn>()` returned null. It is the only UXML file in the package; every other
  panel builds its tree in C#, which is why nothing else showed the fault.

### Changed

- `CoinBasedAchievements` now reads the lifetime balance (`CurrencyBalanceChanged`) instead of
  the per-run session count (`UGS_CoinUpdated`). The session count is reset by the game at the
  end of every run, so a "collect 500 coins" achievement written against it could only ever
  mean 500 coins in a single run. The first balance of a session primes the thresholds without
  announcing them, so a returning player is not re-congratulated at every launch.
- `AchievementsService` no longer loads the catalogue before a player is signed in. The
  achievements UI asks for a load from its constructor, which runs at scene `Awake` - so the
  load fetched Remote Config with no player, drawing a
  `Auth Service not initialized` warning from the SDK and then reporting the empty response as
  `Remote Config has no 'achievements' key`, which blamed a deployment that was not at fault.
- `GameSignalsUGSBridge` maps `GameSignals.SessionEnding` to two targets now - the existing
  `ScoreUpdating` and the new `CurrencySyncRequested`. The chain list has always been a list of
  pairs rather than a dictionary precisely so one signal can declare several consequences.

## [0.1.0] - 2026-08-29

Initial extraction. The `Assets/UGS` tree of the RunnerUGSTemplate project became a standalone
package, and the parts of it that were vendored Asset Store content were replaced with original
code so the package installs into any project without a prior import step.

### Added

- `Events/` - `UGS_EventsEnum`, the UGS auto-event flow, and `GameSignalsUGSBridge`, the only
  place UGS events and `GameSignals` are named together.
- `Initialization/`, `Authentication/`, `RemoteConfig/`, `Leaderboard/`, `Achievements/` and
  the runtime UI theme, all reached through events rather than direct calls.
- `Editor/` - `AchievementDefinitionCatalog` and `AchievementDefinitionExporter`, which write
  achievement definitions as a Remote Config `.rc` deployment file.
- A `UGS Boot Scenes` sample entry for the additive boot scenes.

### Changed

- The vendored Building Blocks stack is gone. Achievements, the sign-in modal and the
  leaderboard panel are original implementations, and the theme is original styling rather
  than the Blocks USS tree with its relative `@import` paths.
- Cross-domain wiring goes through `GameSignals` instead of naming the game's own event enums,
  so the package no longer needs the game to exist.

### Fixed

- `com.unity.remote-config-runtime` is now declared alongside `com.unity.remote-config`. The
  runtime assembly this package references, `Unity.Services.RemoteConfig`, is owned by
  `com.unity.remote-config-runtime`; `com.unity.remote-config` is the editor authoring package
  and only pulled that assembly in transitively. Both are declared because the achievement
  exporter's `.rc` output is only meaningful to the Deployment window that the authoring
  package provides.

### Notes

- `com.crawfissoftware.eventspublisher`, `.common` and `.contracts` are required but are
  distributed by git URL, which UPM cannot resolve from inside a package. Consumers list all
  four in their own project manifest.
