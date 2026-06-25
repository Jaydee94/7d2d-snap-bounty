# Snap Bounty

Server-side **C#/Harmony mod** for *7 Days to Die* **V2.6**.

Every player has **1–3 active bounties** they can complete whenever they like. They can list them
in chat (`/bounty`) and reroll any of them (`/skip <n>`). On completion a **physical loot bag
drops** nearby, filled with good gear (weapons, medkits, magazines, mods, ammo) — scaled by the
bounty's tier.

> **Anti-cheat:** This is a **server-side-only** mod — clients install nothing. `SkipWithAntiCheat`
> makes EAC clients skip the DLL, while a dedicated server still loads server-side mods, so **EAC
> can stay enabled on the server**. (Crossplay clients connect normally.)

## Chat commands

| Command | Effect |
|---|---|
| `/bounty` | Show your active bounties and progress |
| `/bounty help` | Short help |
| `/skip <n>` | Reroll bounty number `n` |

## Bounty types (all tracked server-side)

| Type | Server source | Examples |
|---|---|---|
| **Kill** | `ModEvents.EntityKilled` | any zombies/animals; cops, demolishers, vultures, spiders, lumberjacks, screamers, mutated, wights; predators, bears, dogs, snakes, boars |
| **Mine** | Harmony patch `GameManager.SetBlocksRPC` (block → air) | mine 60 / 250 / 500 blocks |
| **Build** | Harmony patch `GameManager.SetBlocksRPC` (block placed) | place 50 / 150 / 300 blocks |
| **Craft** | Harmony patch `TileEntityWorkstation.AddCraftComplete` | craft any 20/50; forge iron/steel; mix concrete |
| **Explore** | Server polling of `EntityPlayer.biomeStandingOn` | enter desert / burnt forest / snow / wasteland |

## Bounty catalog

33 bounties (defined in [`src/Bounties.cs`](src/Bounties.cs)). The **tier** decides the loot-bag
quality (T1→T3); every bag yields a random **2–5** items.

**Kill — any**

| Tier | Goal |
|---|---|
| 1 | Kill 10 zombies |
| 2 | Kill 50 zombies |
| 3 | Kill 100 zombies |
| 1 | Kill 5 animals |
| 2 | Kill 20 animals |

**Kill — specific zombies**

| Tier | Goal | Targets |
|---|---|---|
| 1 | Kill 20 vultures | zombie vultures (+ radiated) |
| 2 | Kill 5 cops | fat cop (+ feral/radiated/infernal) |
| 2 | Kill 10 spider zombies | spider (+ feral/radiated/charged/infernal) |
| 2 | Kill 10 lumberjacks | lumberjack (+ feral/radiated/infernal) |
| 2 | Kill 5 screamers | screamer (+ feral/radiated) |
| 2 | Kill 10 mutated | mutated (+ feral/radiated/charged/infernal) |
| 3 | Kill 5 wights | wight (feral/radiated/charged/infernal) |
| 3 | Kill 3 demolishers | demolition |

**Kill — animals / hunting**

| Tier | Goal | Targets |
|---|---|---|
| 1 | Kill 10 snakes | snake |
| 1 | Kill 10 boars | boar (+ zombie boar) |
| 2 | Kill 10 predators | wolf, direwolf, bear, mountain lion |
| 2 | Kill 3 bears | bear, small bear, zombie bear |
| 2 | Kill 15 zombie dogs | zombie dog |

**Mine blocks**

| Tier | Goal |
|---|---|
| 1 | Mine 60 blocks |
| 2 | Mine 250 blocks |
| 3 | Mine 500 blocks |

**Build (place blocks)**

| Tier | Goal |
|---|---|
| 1 | Place 50 blocks |
| 2 | Place 150 blocks |
| 3 | Place 300 blocks |

**Craft (at workstations)**

| Tier | Goal |
|---|---|
| 2 | Craft any 20 items |
| 3 | Craft any 50 items |
| 2 | Forge 50 forged iron |
| 2 | Mix 100 concrete mix |
| 3 | Forge 100 forged steel |

**Explore (enter biome)**

| Tier | Goal |
|---|---|
| 1 | Enter the desert |
| 2 | Enter the burnt forest |
| 2 | Enter the snow |
| 3 | Enter the wasteland |

## How it works

- **Tracking (C#):** kills via `ModEvents.EntityKilled`; block/craft events via Harmony postfix
  patches on the server-authoritative methods; biomes via throttled polling (`ModEvents.GameUpdate`).
  Per-player state (`PlatformId.CombinedString`) is persisted to the save folder
  (`<save>/SnapBounty/bounties.txt`).
- **Assignment:** on login a player is filled up to 3 random, non-duplicate bounties from the
  catalog; completed/skipped ones are refilled.
- **Reward (C# → XML):** on completion the mod calls
  `GameEventManager.Current.HandleAction("snapBountyReward_tX", player, …)`, which triggers the
  `SpawnContainer` action in `Config/gameevents.xml`. That spawns the loot-bag entity
  (`Config/entityclasses.xml`) filled from the loot group in `Config/loot.xml` (a random **2–5**
  items per bag) right next to the player.

## Layout

```
src/                 C# source (+ SnapBounty.csproj)
  ModApi.cs          IModApi entry, event/Harmony wiring, chat-command parsing
  Bounties.cs        bounty definitions + catalog
  BountyManager.cs   assignment, tracking, /skip, reward trigger, biome polling
  HarmonyPatches.cs  server hooks for block (SetBlocksRPC) and craft (AddCraftComplete) events
  Persistence.cs     save/load in the save folder
  ChatUtil.cs        chat message to a single player
SnapBounty/          mod assets (committed; the .dll is built, not committed)
  ModInfo.xml
  Config/{loot,entityclasses,gameevents}.xml
scripts/             CI helpers (server install, release staging, notes footer)
.github/workflows/   CI (compile on PR) + Release (semantic-release)
```

## Build

Requires the .NET SDK. References are resolved against a 7DTD installation.

**Local (uses your local Steam install on macOS by default):**

```bash
./build.sh
# -> dist/SnapBounty/ (folder ready to copy into Mods/)
```

Override the game location or deploy straight into a server:

```bash
GAME_ROOT="/path/to/7 Days To Die Dedicated Server" ./build.sh
DEPLOY_DIR="/path/to/7 Days To Die/Mods" ./build.sh
```

Or build the raw DLL only:

```bash
dotnet build src/SnapBounty.csproj -c Release            # -> bin/Release/SnapBounty.dll
dotnet build src/SnapBounty.csproj -c Release -p:GameRoot="$SERVER_ROOT"
```

## CI / Release

- **CI** (`.github/workflows/ci.yml`): on every PR (and manual dispatch) installs the 7DTD
  Dedicated Server via SteamCMD (cached) to provide the build DLLs, then compiles the mod.
- **Release** (`.github/workflows/release.yml`): manual dispatch. Builds, then runs
  **semantic-release** (Conventional Commits) to compute the version, update `CHANGELOG.md`, stamp
  `SnapBounty/ModInfo.xml`, package `dist/SnapBounty-v<version>.zip`, and publish a GitHub Release.
  Use the `dry_run` input to preview without publishing.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`,
`fix:`, `chore:` …) so the version bump is derived automatically.

## Installation (server)

1. Build the package (`./build.sh`) or download `SnapBounty-v<version>.zip` from a release.
2. Copy the `SnapBounty/` folder into your server's `Mods/` directory.
3. Restart the server. (Server-side only; clients don't need it; EAC can stay enabled.)

## Limitations / roadmap

- **Crafting** is only counted at **workstations** (forge/workbench/chem station/campfire), since
  only those run server-side via `AddCraftComplete`. Pure hand-crafting isn't counted.
- **Mine/build** counts block-value changes; upgrades may count as "place" (approximation).
  "Harvest X wood" by item amount is approximated as "mine blocks".
- Not yet verified in-game (no game launch during development): that the loot bag spawns at the
  player via `HandleAction`, and that `SetBlocksRPC`/`AddCraftComplete` fire as expected on a
  dedicated server — check these first on a real server.
- Planned: skip cooldown, configurable counts/tiers, localized chat text.
