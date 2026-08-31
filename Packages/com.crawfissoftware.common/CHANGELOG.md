# Changelog

All notable changes to this package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-08-31

No code changes. The version is aligned with the `v0.4.0` repository tag, because the three
packages share one tag stream and version together.

## [0.3.0] - 2026-08-31

No code changes. The version is aligned with the `v0.3.0` repository tag, because the three
packages share one tag stream and version together. Nothing in this package references the
`GameSignals` enum renamed in contracts 0.3.0, so it is source-compatible with 0.1.0.

## [0.1.0] - 2026-08-29

Initial extraction. The code was lifted from the `Assets/_Common` and
`Assets/GameFlow/Scripts/SceneManagement` trees of the RunnerUGSTemplate project and made
installable on its own, under the `CrawfisSoftware.Common` assembly.

### Added

- `EventChainDispatcher<TSource, TDest>` and `AutoEventFlowBase<TSource, TDest>` - the one
  dispatch implementation behind every auto-event flow and cross-domain bridge, taking chains
  as a flat list of pairs so a single source event may declare several consequences.
- Event-driven additive scene management: `LoadSceneAdditively`,
  `LoadSceneAfterGameControlEvent`, `CloseSceneOnEvent`, `FireEventWhenSceneCloses`.
- `DifficultyConfig`, `EventHistory`, `Logger`, `DebugEventFileLogger`, `TimedEvent`,
  `TextureExtensions`, and the `Test_AutoFireEvent` / `Test_AutoFireEventOnStart` helpers.
- `com.unity.modules.imageconversion` as a declared dependency. `TextureExtensions` calls
  `Texture2D.LoadImage` and `Texture2D.EncodeToPNG`, which that removable built-in module
  owns, and nothing else the package references pulls it in.

### Notes

- `com.crawfissoftware.eventspublisher` is required but is distributed by git URL, which UPM
  cannot resolve from inside a package. Consumers list it in their own project manifest.
