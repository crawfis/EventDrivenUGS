# Changelog

All notable changes to this package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-29

Initial extraction. The vocabulary a game and a backing services layer use to talk to each
other was pulled out of the RunnerUGSTemplate project into terms neither side owns, under the
`CrawfisSoftware.Contracts` assembly.

### Added

- `GameSignals` - the contract enum both sides bridge to, replacing the direct
  TempleRun-to-UGS and UGS-to-GameFlow event references the original project carried.
- `ServicesStatus` - the payload describing what a services layer has come up with.

### Notes

- The package references nothing but `com.crawfissoftware.eventspublisher`, and no engine
  module, on purpose: a game and a services layer must both be able to reference it without
  either referencing the other.
- `com.crawfissoftware.eventspublisher` is required but is distributed by git URL, which UPM
  cannot resolve from inside a package. Consumers list it in their own project manifest.
