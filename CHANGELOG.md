# 1.0.0 (2026-06-25)


### Features

* add config file (counts/tiers) and skip cooldown ([b034746](https://github.com/Jaydee94/7d2d-snap-bounty/commit/b03474602153134b437cf108e22b1b656415123b))
* add Snap Bounty server-side mod with bounties, loot drops and CI/release ([8f88214](https://github.com/Jaydee94/7d2d-snap-bounty/commit/8f882146e41aaf6ec51c7155bb092b20d107f1c5))
* english chat messages and bounty titles ([3d2eba4](https://github.com/Jaydee94/7d2d-snap-bounty/commit/3d2eba41eb4b1e0f20acc477809610cf676332f2))
* expand bounty catalog to 33 bounties ([616ba8c](https://github.com/Jaydee94/7d2d-snap-bounty/commit/616ba8c8e501a7f6e172b476416bce35271ffa67))


### Installation
1. Download `SnapBounty-v1.0.0.zip` and unzip it.
2. Copy the `SnapBounty/` folder into your server's `Mods/` directory.
3. Restart the server. (Server-side only — clients don't need to install anything; EAC can stay enabled on a dedicated server.)

### Commands
- `/bounty` — show your active bounties and progress
- `/skip <n>` — reroll bounty number n

### Contents
```
SnapBounty/
├── SnapBounty.dll
├── ModInfo.xml
├── snapbounty.xml          # config: maxActive, skip cooldown, per-bounty count/tier
└── Config/
    ├── loot.xml
    ├── entityclasses.xml
    └── gameevents.xml
```

Built for **7 Days to Die V2.6**.
