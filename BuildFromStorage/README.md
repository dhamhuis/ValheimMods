# BuildFromStorage

Build with the Hammer using materials from nearby storage.

BuildFromStorage adds eligible nearby storage to vanilla build-cost checks and consumes materials from your inventory first, then the nearest storage that supplies the remaining amount. Vanilla placement rules, building-station requirements, permissions and no-cost modes remain in effect.

**Tip:** The build menu considers materials in your inventory and eligible nearby storage when showing which pieces are available to build. Normal building requirements still apply.

## Storage rules

- The default radius is **20 meters**, configurable from 1 to 100 meters.
- The radius is a sphere centered on your character.
- Storage must be accessible, closed and currently under your client's network ownership. This is separate from permission to access it, and ownership can change.
- If the mod isn't using an accessible chest, try opening it yourself and closing it before building. A successful open can transfer network ownership to your client, but ownership may change later. You do not need to open a chest that your client already owns; the mod does not request ownership automatically.
- Personal chest and ward permissions remain in effect.
- Only Hammer construction costs are handled. Repairs, dismantling, crafting-station recipes, fueling and processing-machine inputs are not changed.
- Materials are removed from storage when placement succeeds; output is not deposited into storage.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, which the mod manager installs for you.

For manual installation, copy `plugins/BuildFromStorage/` into `BepInEx/plugins/BuildFromStorage/`. Keep `BuildFromStorage.dll` and `ValheimMods.Common.dll` together in that folder.

BuildFromStorage runs on each player's client; a server installation is not required.

## Configuration

`BepInEx/config/hoose.valheim.buildfromstorage.cfg`:

- `Storage/Storage radius` (default `20`, range `1`-`100` meters): how far around the player storage is searched.

## Validation

BuildFromStorage is an original implementation following the repository's shared BepInEx build and storage conventions. The user reported single-player testing complete on 2026-11-24; see the checklist for the recorded steps. Multiplayer ownership and concurrency remain untested.

See the [in-game test checklist](TESTING.md) for step-by-step validation.

[Project and support](https://github.com/dhamhuis/ValheimMods)