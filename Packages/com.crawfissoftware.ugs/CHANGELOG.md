# Changelog

All notable changes to this package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
