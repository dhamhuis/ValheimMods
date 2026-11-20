# SupplyFromStorage

**Feed your fires and production machines without hauling fuel and materials out of chests. Walk up and press E; nearby storage supplies the input.**

Want charcoal without sacrificing your good building wood? The **F8 menu** lets you choose what goes into supported stations, or tell Auto what to use first and what to leave alone.

## Supported stations and machines

- **Campfires, bonfires, hearths, fireplaces, braziers and torches** - add their accepted fuel.
- **Stone Oven and other fuel-enabled cooking stations** - add fuel, not cooking ingredients.
- **Charcoal Kiln** - supply Wood, Fine Wood or Core Wood for charcoal.
- **Smelter and Blast Furnace** - supply fuel and accepted ores.
- **Windmill** - supply barley.
- **Spinning Wheel** - supply flax.
- **Eitr Refinery** - supply Sap to its fuel slot or Soft Tissue to its separate processing-input slot.
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
- Fuel, other processors and disabled-selection paths use accepted items in your inventory first through the normal E interaction, then nearby accessible storage. Enabled selection chooses a discovered type by priority first, then prefers inventory within that type.
- Successful fireplace and fuel-switch transfers show the added-fuel message without a misleading missing-fuel warning.
- Full fires report that no more fuel can be added when nearby accessible storage has fuel; no fuel is consumed.
- A storage fallback consumes from the container, not your inventory.
- Fuel and processing-input automation have independent settings, both enabled by default.
- Rechecks normal container access, ward/busy rules, ownership and station capacity before consuming. Containers and machines owned by another player are skipped.

Cooking ingredients and Fermenter bases belong to **CookFromStorage**, not this mod.

## Choose what goes in: F8 menu

Use the menu at the **Charcoal Kiln, Smelter, Blast Furnace or Battering Ram engine**. Aim at the material input (the ram's rear loading input), not a separate fuel input, and press **F8**.

1. **Pick an item:** Highlight an available item and choose **Use highlighted**. Nothing moves yet; press **E** to add that exact type. It won't substitute a different item if you run out.
2. **Set your preferences:** On each item, toggle **Auto** to **Never auto-add** to keep it out of automatic choices. Use **Up / Down** to change priority: lower numbers are used first.
3. **Let Auto choose:** Select **Use automatic choice**. When you press E, Auto chooses the first available allowed type, checking inventory first and then nearby storage for that type.

For example, set Finewood and Corewood to **Never auto-add** at the kiln and let Auto turn ordinary Wood into charcoal. Need charcoal in a hurry? You can still explicitly select an excluded type or use an item manually.

The menu shows your discovered inputs and their inventory and storage counts, including discovered items you don't currently have. Undiscovered items stay hidden and aren't chosen by Auto. Preferences save immediately, even if you close the menu, and are shared by stations of the same type for that character. An exact choice is temporary; leaving the input without holding E or switching stations clears it. **Close** leaves your current choice unchanged.

Where vanilla repeats held E, you can briefly aim away and back without stopping. Auto moves to the next available allowed type when one runs out; an exact choice stops instead. A full station, lost access or opening another menu stops the hold until you release E.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, which the mod manager installs for you.

For manual installation, copy `plugins/SupplyFromStorage.dll` and `plugins/ValheimMods.Common.dll` into `BepInEx/plugins/SupplyFromStorage/`. Keep the shared library beside the plugin and only one installed copy of each DLL.

Install on each player's client; a server-only install does not enable it.

## Configuration

Settings are in `BepInEx/config/hoose.valheim.supplyfromstorage.cfg`; each setting's description lists the machines and items it controls.

- `Automation/Automatic fuel` - fuel for fires, cooking stations, the Stone Oven, Smelter, Blast Furnace, Eitr Refinery, Hot Tub, Frost Kiln and Frost Foundry. Enabled by default.
- `Automation/Automatic processing inputs` - ore, charcoal-kiln wood, barley, flax, Soft Tissue for the Eitr Refinery and Battering Ram wood. Enabled by default.
- `Input selection/Enabled` - discovered-type selection and preferences for the supported multi-input processors. Enabled by default for new configs while Automatic processing inputs is on; saved disabled values remain disabled.
- `Storage/Storage radius` - how far around you storage is searched, from 1 to 100 meters. Default 20.

The F8 menu requires both `Input selection/Enabled` and `Automation/Automatic processing inputs`. New configurations enable both; a previously saved `Input selection/Enabled = false` remains disabled. The menu key is configurable under `Input selection preview/Open key`. Disabling selection restores legacy inventory-first behavior, without the automatic exclusions.

## Stack with other mods

Works well with [CookFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CookFromStorage) (cooking ingredients and Fermenter bases), [CraftFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CraftFromStorage) (craft and upgrade from nearby chests) and [AutoRepair](https://github.com/dhamhuis/ValheimMods/tree/main/AutoRepair) (repairs your gear when you use a station). SupplyFromStorage feeds the fire; CookFromStorage loads the food. Every storage mod uses the same 20 m default radius, and each can be set separately.

[Project and support](https://github.com/dhamhuis/ValheimMods)