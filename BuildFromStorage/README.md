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
- The build tooltip shows each material as the required amount with `inventory:storage` on the line below, for example `5` with `10:42` below (the `5` moves up a line so the counts fit where it normally sits, and long material names stay on one line): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
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

### Shared placement engine

The current development build shares one tool-routed placement engine with [FarmFromStorage](../FarmFromStorage/README.md). Hammer and Cultivator costs are handled independently, including separate radius settings. The prior single-player report predates this migration; repeat the applicable Hammer checklist before packaging.

If using both mods, install the current development BuildFromStorage DLL, not its old prepared ZIP, and keep every installed `ValheimMods.Common.dll` copy identical and current. FarmFromStorage rejects the older standalone BuildFromStorage patch set to prevent duplicate consumption.

[Project and support](https://github.com/dhamhuis/ValheimMods)