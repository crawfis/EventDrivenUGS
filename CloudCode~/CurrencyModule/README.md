# CurrencyModule — the trusted path for `PlayerCurrencyManager`

Two Cloud Code endpoints backing `CloudCodeCurrencyBackend`, so a player's balance is written with
the module's service token rather than by the player's own device.

**What it guarantees, and what it does not.** It moves the *write* off the device, which is what
keeps crediting working under an access policy denying players direct writes to
`urn:ugs:economy:*`. It does **not** validate that the coins were earned: the amount comes from the
client and this module holds no run state to check it against, so it clamps the per-call amount and
nothing more. Making it genuinely authoritative means an endpoint built on the game's own rules -
one that takes a run token and recomputes the credit, the way `AdRewardsService` validates an ad
reward. That is a game-design decision, not a porting one.

This folder is `CloudCode~`, a tilde folder: Unity never imports it, and it is not part of the
package. `com.crawfissoftware.ugs` still ships **no deployed module** — `CloudCodeCurrencyEndpoints.Default`
carries no `ModuleName`, and the trusted backend refuses to construct without one. You deploy this,
then name it.

## What it is

| File | |
|---|---|
| `PlayerCurrencyService.cs` | `GetCurrencyBalance` and `AddCurrency`, harvested from the currency half of `TempleRunUGSCloud`'s `PlayerEconomyService` |
| `ModuleSetup.cs` | `ICloudCodeSetup` registering `GameApiClient`, same shape as the existing Blocks modules |

The two function names must match `CloudCodeCurrencyEndpoints.Default` exactly, and the
`CurrencyBalance` property names must match `CurrencyBalanceDto` exactly. Renaming any of
them fails at runtime with nothing to say so.

### What was left behind

The source it came from is 831 lines; roughly 100 were currency. Inventory CRUD, the five booster
item ids, the `INFINITE_HEART` expiry system and the `COIN_PACK_FREE` store pack are Match3 sample
content and were not carried over.

### Defects fixed in the harvest

- **A new player's first credit failed.** The original threw
  `InvalidOperationException("Currency {id} not found in player balances")` when the player had no
  balance row — which is every player before their first coin. Nothing reads a row before crediting
  now, so there is no row to be missing.
- **The 403 was destroyed.** The original caught and rethrew `new Exception($"...{e.Message}")`,
  flattening the chain. `CurrencyBackendException.IsAccessDenied` on the client looks for a 403 in
  the message chain, so an access-policy denial reported as a generic failure is the one failure a
  developer could have fixed without touching code. The original exception is now the inner.
- **The write lock made a commutative operation lossy.** The original read the balance to obtain a
  `WriteLock` and passed it to Increment. But Increment is arithmetic the service applies to the
  stored value - it is already safe against a concurrent writer - so the lock only added a round
  trip and a way to *fail* on concurrency instead of composing with it. No lock is passed now.
- **The player's token was used for a write meant to escape a player-write denial.** The original
  passed `context.AccessToken`, which carries the player as the request's principal. A policy
  denying `Player` writes to Economy would deny that too, making the trusted path pointless
  precisely when it is needed. It now uses `context.ServiceToken`, as the sibling services that
  reach beyond the caller already do.

## No project file — read this before deploying

**No Cloud Code module in `RunnerUGSTemplate` has a working `.csproj`/`.sln` yet**, this one
included. `*.csproj` and `*.sln` were ignored wholesale, so a clone got a `.ccmr` pointing at a
solution nobody had. Cloud Code modules are now excepted from that ignore rule, so once a solution
is generated it stays.

Generating one is `Generate Solution` on the `.ccmr` asset's inspector. If it fails with
`IOException: ... already exists`, a leftover file is blocking the template copy — clear it and run
again, and check the result is named after the module: the generator copies the template *then*
renames, so an aborted run leaves `Solution.sln` and `Project/Project.csproj` behind. That matters
because `GetMainEntryProjectName` takes the deployed module's name from the solution's main
project, so deploying an un-renamed one publishes a module called `Project`.

The Cloud Code package's own template is the authority on the project shape, and running
**Generate Solution** on a `.ccmr` lays it down. As of `com.unity.services.cloudcode@4d38bd1f7a5b`
that template is:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <Configurations>Debug;Release</Configurations>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Com.Unity.Services.CloudCode.Apis" Version="0.0.26" />
    <PackageReference Include="Com.Unity.Services.CloudCode.Core" Version="0.0.5" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="7.0.1" />
  </ItemGroup>
</Project>
```

`<Nullable>enable</Nullable>` matters: `PlayerCurrencyService` uses `context.PlayerId!`.

Generate the solution rather than copying that by hand - the template is versioned with the package
and this snapshot will drift.

## Deploying

1. Create the class library and add both files.
2. Put the `.sln` where a sibling `.ccmr` can point at it, matching the existing pattern:
   `{ "modulePath": "CurrencyModule~/CurrencyModule.sln" }`.
3. `Services > Deployment`, select the module, Deploy. **Do not select any `.ac` file** — see below.
4. Set the module name on the client, either in the `PlayerCurrencyController` inspector field or
   in code before anything touches the backend:

   ```csharp
   var endpoints = PlayerCurrencyManager.Instance.CloudCodeEndpoints;
   endpoints.ModuleName = "CurrencyModule";
   PlayerCurrencyManager.Instance.CloudCodeEndpoints = endpoints;
   PlayerCurrencyManager.Instance.UseTrustedClient = true;
   ```

## When you actually need this

The client-authoritative `EconomyCurrencyBackend` is the default and is the right choice while coins
buy only single-player progression: a player cheating themselves costs nobody else anything, and it
needs no module and no deploy.

Switch to this one when you deploy an access policy denying player writes to
`urn:ugs:economy:*` — at which point the client-authoritative path 403s on every credit and this
becomes the only one that works.

Switching because "the balance buys something a competitor can see" is **not** enough on its own:
as above, the amount still comes from the client. That case needs an endpoint that recomputes the
credit from server-held state, not just a different backend.

There is precedent for that denial in this project: `AchievementsAccessControl.ac` denies player
writes to `urn:ugs:cloud-save:*`, which is exactly what the default achievements backend does.
