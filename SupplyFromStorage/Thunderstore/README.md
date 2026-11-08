# SupplyFromStorage

**Feed your fires and production machines without hauling fuel and materials out of chests. Walk up and press E; nearby storage supplies the input.**

## Supported stations and machines

- **Campfires, bonfires, hearths, fireplaces, braziers and torches** - add their accepted fuel.
- **Stone Oven and other fuel-enabled cooking stations** - add fuel, not cooking ingredients.
- **Charcoal Kiln** - supply wood for charcoal.
- **Smelter and Blast Furnace** - supply fuel and accepted ores.
- **Windmill** - supply barley.
- **Spinning Wheel** - supply flax.
- **Eitr Refinery** - supply its accepted processing inputs.
- **Hot Tub** - add wood.
- **Battering Ram** - feed its engine Wood, Fine Wood, Core Wood or Ashwood.
- **Frost Kiln** - add ice.
- **Frost Foundry** - add liquid frost. Casts stay manual.

Machine input support follows each machine's own accepted-item list; fuel support follows its fuel slot. Compatible modded machines using the same components can also use their current accepted inputs.

By Hoose.

## Features

- Searches storage in a sphere centered on the local player: 20 meters by default, configurable from 1 to 100 meters.
- Supplies **one accepted fuel or processing item per valid E action**.
- Holding E repeats one-item transfers at the game's interaction repeat interval; stations are not filled on load or during updates.
- Uses accepted items in your inventory first through the normal E interaction. If none are available, supplies one from nearby accessible storage instead.
- Successful fuel-switch transfers show the added-fuel message.
- A storage fallback consumes from the container, not your inventory.
- Fuel and processing-input automation have independent settings, both enabled by default.
- Rechecks normal container access, ward/busy rules, ownership and station capacity before consuming. Containers and machines owned by another player are skipped.

Cooking ingredients and Fermenter bases belong to **CookFromStorage**, not this mod.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, which the mod manager installs for you.

For manual installation, copy `plugins/SupplyFromStorage.dll` and `plugins/ValheimMods.Common.dll` into `BepInEx/plugins/SupplyFromStorage/`. Keep the shared library beside the plugin and only one installed copy of each DLL.

Install on each player's client; a server-only install does not enable it.

## Configuration

Settings are in `BepInEx/config/hoose.valheim.supplyfromstorage.cfg`; each setting's description lists the machines and items it controls.

- `Automation/Automatic fuel` - fuel for fires, cooking stations, the Stone Oven, Smelter, Blast Furnace, Hot Tub, Frost Kiln and Frost Foundry. Enabled by default.
- `Automation/Automatic processing inputs` - ore, wood, barley, flax, Eitr Refinery inputs and Battering Ram wood. Enabled by default.
- `Storage/Storage radius` - how far around you storage is searched, from 1 to 100 meters. Default 20.

## Stack with other mods

Works well with [CookFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CookFromStorage) (cooking ingredients and Fermenter bases), [CraftFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CraftFromStorage) (craft and upgrade from nearby chests) and [AutoRepair](https://github.com/dhamhuis/ValheimMods/tree/main/AutoRepair) (repairs your gear when you use a station). SupplyFromStorage feeds the fire; CookFromStorage loads the food. Every storage mod uses the same 20 m default radius, and each can be set separately.

[Project and support](https://github.com/dhamhuis/ValheimMods)