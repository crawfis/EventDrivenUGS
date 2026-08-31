# Changelog

All notable changes to this package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this
package adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-08-31

### Added

- `GameServiceEvents.CurrencyBalanceChanged` - the player's banked lifetime soft-currency
  balance as the backing service reports it. Payload `long`, delivery Sticky.

  It is not the mirror of `CurrencyTotalChanged`: that runs game to services and carries one
  session's running count, while this is the stored total the service is authoritative for. A
  game showing a balance needs this one; the session count resets every run.

  Sticky for the same reason `ServicesStatusChanged` is - the balance is read once at sign-in,
  long before a HUD in an additively-loaded scene exists to hear it, and as an edge it would
  leave that HUD blank until a run ended.

  Additive; existing members keep their values.

## [0.3.0] - 2026-08-31

### Changed

- **Breaking.** `GameSignals` is now `GameServiceEvents`, and `GameSignals.cs` is
  `GameServiceEvents.cs`. Every member name, numeric value, payload and delivery attribute is
  unchanged - only the enum's own name moved. A compile error naming `GameSignals` is this
  rename and nothing else; update the type name and any
  `EventsFor<GameSignals>` alias.
- The old name said "Signals" where every sibling enum in both projects says "Events", implying
  a different mechanism rather than the same one at a boundary. Worse, the `Game` prefix read as
  possessive - as though the game owned a vocabulary whose entire purpose is that neither side
  does. `GameServiceEvents` names the boundary the enum spans instead.

### Notes

- `ServicesStatus` is unchanged in name, namespace and package. It is a payload type, not part
  of the renamed vocabulary.
- The package id `com.crawfissoftware.contracts`, the assembly `CrawfisSoftware.Contracts` and
  the namespace `CrawfisSoftware.Contracts` are all unchanged, so a manifest entry pointing at
  this package keeps resolving untouched.
- The version jumps 0.1.0 -> 0.3.0 rather than to 0.2.0, so that it matches the `v0.3.0`
  repository tag consumers pin to. Nothing was released as 0.2.0.

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
