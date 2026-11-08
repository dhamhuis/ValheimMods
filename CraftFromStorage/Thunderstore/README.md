# CraftFromStorage

**Craft and upgrade without the chest-to-workbench shuffle. Use materials straight from nearby storage and keep your inventory free.**

## Supported stations

- **Workbench** - craft and upgrade its equipment and other recipes.
- **Forge** - make and upgrade metal weapons, armor and tools.
- **Stonecutter** - craft its station recipes, such as sharpening stones.
- **Artisan Table**, **Black Forge** and **Galdr Table** - use nearby materials for their station recipes and eligible upgrades.
- **Other non-food crafting stations**, including compatible modded stations.

Available recipes and upgrades still depend on your progression and the station's normal requirements. **Cauldron, Food Preparation Table and Mead Ketill** recipes belong to **CookFromStorage**, not this mod.

By Hoose.

## Behavior

- Searches a **20 meter sphere around the player** by default, not around the station. Configurable from 1 to 100 meters.
- Uses your inventory first, then nearby accessible chests, carts and ship storage, nearest first.
- Includes non-food station recipes and equipment upgrades; ingredients may be split across several containers.
- Respects chest ownership, wards and normal recipe/station rules. Busy storage is skipped.
- Crafting requirement colors include available storage materials. Crafted output stays in your inventory.
- Storage owned by another player may add a short delay while ownership is requested. Missing, denied or unresponsive storage cancels crafting without spending ingredients.

Does **not** add hand crafting from storage, hammer building, auto-repair, fueling, automatic cooking/processing, sorting, or stashing. No hotkeys are needed.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, which the mod manager installs for you.

For manual installation, copy `plugins/CraftFromStorage.dll` and `plugins/ValheimMods.Common.dll` into `BepInEx/plugins/CraftFromStorage/`. Keep the shared library beside the plugin and only one installed copy of each DLL.

Install on each player's client; a server-only install does not enable it. Do not install another craft-from-storage mod alongside it.

## Configuration

Settings are in `BepInEx/config/hoose.valheim.craftfromstorage.cfg`; each setting's description explains what it controls.

- `Storage/Storage radius` - how far around you storage is searched, from 1 to 100 meters. Default 20. Food-station recipes use CookFromStorage's own radius.

## Stack with other mods

Works well with [CookFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CookFromStorage) (food recipes, cooking stations and Fermenters), [SupplyFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/SupplyFromStorage) (fuel and machine inputs) and [AutoRepair](https://github.com/dhamhuis/ValheimMods/tree/main/AutoRepair) (repairs your gear when you use a station). CraftFromStorage and CookFromStorage share one recipe engine, so ingredients are never counted or consumed twice. Every storage mod uses the same 20 m default radius, and each can be set separately.

[Project and support](https://github.com/dhamhuis/ValheimMods)