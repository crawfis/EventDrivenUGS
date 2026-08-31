# EventDrivenUGS

A Unity Gaming Services integration a game can talk to without ever naming it - and, because the
coupling runs only through events, one it can also run *without*.

Three UPM packages in one repository, consumed by git URL with `?path=`.

| Package | What it is |
|---------|------------|
| [`com.crawfissoftware.contracts`](Packages/com.crawfissoftware.contracts) | `GameSignals` - the vocabulary a game and a services layer share, in terms neither owns. |
| [`com.crawfissoftware.common`](Packages/com.crawfissoftware.common) | Game-neutral pieces: the event-chain dispatcher behind every auto-flow and bridge, additive scene plumbing, small utilities. |
| [`com.crawfissoftware.ugs`](Packages/com.crawfissoftware.ugs) | A Unity Gaming Services integration expressed entirely as events. |

## Install

**UPM does not resolve git dependencies declared inside a package.** These three cannot pull each
other in, so every one you use must be listed in your own manifest, along with EventsPublisher:

```jsonc
// Packages/manifest.json
"com.crawfissoftware.eventspublisher": "https://github.com/crawfis/EventsPublisher.git",
"com.crawfissoftware.contracts": "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.contracts",
"com.crawfissoftware.common":    "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.common",
"com.crawfissoftware.ugs":       "https://github.com/crawfis/EventDrivenUGS.git?path=/Packages/com.crawfissoftware.ugs"
```

The URL grammar is `protocol://host/path.git?path=/subfolder#revision` - `?path=` always precedes
`#revision`. Pin a revision by appending a tag, e.g. `#v0.2.0`, once one is cut - the packages are
at **0.2.0** and `main` is what an unpinned URL tracks.

```
  game events   <->   GameSignals   <->   UGS events
   (your glue)        (contracts)        (ugs package)
```

## Why one repository

The three change together. A monorepo makes a change spanning `Common` and `UGS` one atomic commit
instead of two commits in two repos that can never be atomic. The cost is a shared tag stream: all
three version together.

## Third-party content

**None.** That is deliberate and load-bearing - see
[docs/extraction-status.html](docs/extraction-status.html) for why a package containing Asset Store
content could not be distributed at all.

## Licence

CC0-1.0. See [LICENSE.txt](LICENSE.txt).
