# FarmFromStorage

**Plant your fields straight from nearby chests. Leave the seeds in storage, not in your pockets.**

Grab your Cultivator, choose a plant and get growing. FarmFromStorage draws the planting materials from nearby storage, so you can tend your fields without hauling stacks of seeds back and forth.

## What you can plant

- **Crops from seeds** - sow your next harvest using stored seeds.
- **Seed-producing plants** - replant crops from storage to grow your next supply of seeds.
- **Tree saplings** - start a grove using stored tree seeds and cones.
- **Other Cultivator planting entries** - use their normal materials, without a fixed seed whitelist.

From planting a vegetable patch to replenishing your seed supply and growing a grove, **FarmFromStorage keeps planting materials in your chests and your inventory free for the adventure**.

By Hoose.

## How it works

- Keep planting materials in accessible storage within **20 meters of your player** (configurable from 1 to 100 meters).
- Use the Cultivator normally. Choose an unlocked plant and place it as usual.
- Materials come from your inventory first, then the nearest eligible storage supplies the remainder.
- A successful placement consumes exactly the normal planting cost. Rejected placements do not spend storage materials.
- The Cultivator tooltip shows each planting material as the required amount with `inventory:storage` on the line below, for example `5` with `10:42` below (the `5` moves up a line so the counts fit where it normally sits, and long material names stay on one line): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
- Normal unlocks, growing conditions, placement restrictions, stamina and tool durability still apply.
- Storage must be accessible, closed and currently owned by your client's network peer. Ward and personal-chest permissions are respected.
- If an accessible chest is not being used, try opening and closing it yourself. This can transfer ownership, but ownership may change; the mod does not request ownership automatically.

This mod supplies planting materials, not automatic harvesting. Cultivating soil, restoring grass, harvesting and plant growth are unchanged.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, installed automatically by the manager.

For manual installation, copy `plugins/FarmFromStorage.dll` and `plugins/ValheimMods.Common.dll` from the archive into `BepInEx/plugins/FarmFromStorage/`. Keep both DLLs together. Do not install duplicate copies of the plugin.

Install on each player's client; no server installation is required. When using companion mods, keep all copies of `ValheimMods.Common.dll` identical and current. Older standalone-placement BuildFromStorage builds must be updated before using both mods.

## Configuration

`BepInEx/config/hoose.valheim.farmfromstorage.cfg`:

- `Storage/Storage radius`: default `20`, allowed range `1`-`100` meters.

## Stack with other mods

- [BuildFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/BuildFromStorage) builds with the Hammer from nearby storage, with an independent radius.
- [CraftFromStorage](https://thunderstore.io/c/valheim/p/Hoose/CraftFromStorage/) crafts non-food recipes from storage.
- [CookFromStorage](https://thunderstore.io/c/valheim/p/Hoose/CookFromStorage/) prepares food and supplies cooking ingredients.
- [SupplyFromStorage](https://thunderstore.io/c/valheim/p/Hoose/SupplyFromStorage/) supplies fuel and machine processing inputs.
- [AutoRepair](https://thunderstore.io/c/valheim/p/Hoose/AutoRepair/) repairs equipment at crafting stations.

If you enjoy these mods and would ever like to support my work, [I'm on Ko-fi](https://ko-fi.com/hoose_builds). Thanks for using them!

[Project and support](https://github.com/dhamhuis/ValheimMods)