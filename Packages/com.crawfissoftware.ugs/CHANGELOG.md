# Changelog

All notable changes to this package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

### Changed

- `CoinBasedAchievements` now reads the lifetime balance (`CurrencyBalanceChanged`) instead of
  the per-run session count (`UGS_CoinUpdated`). The session count is reset by the game at the
  end of every run, so a "collect 500 coins" achievement written against it could only ever
  mean 500 coins in a single run. The first balance of a session primes the thresholds without
  announcing them, so a returning player is not re-congratulated at every launch.
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
