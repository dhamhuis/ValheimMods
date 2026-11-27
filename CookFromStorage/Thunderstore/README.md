# CookFromStorage

**Cook meals and brew mead straight from nearby chests. Leave the ingredients in storage, not in your pockets.**

Want to save an ingredient for another recipe? The **F8 menu** puts you in charge of what goes on the rack, in the oven or into the Fermenter.

## Supported stations

- **Cooking Station** - add raw meat from storage.
- **Iron Cooking Station** - load its accepted cooking ingredients from storage.
- **Stone Oven** - bake using stored ingredients.
- **Cauldron** - see available ingredients and craft food using storage.
- **Food Preparation Table** - prepare recipes using stored ingredients.
- **Mead Ketill** - craft mead bases using storage.
- **Fermenter** - load a stored mead base into an empty, sheltered Fermenter.

From preparing ingredients to cooking dinner and brewing your next batch, **CookFromStorage handles the whole food-and-mead workflow**.

By Hoose.

## How it works

- Keep ingredients in accessible storage within **20 meters of your player** (configurable from 1 to 100 meters).
- Use stations normally. Recipe ingredient counts include nearby storage; press **E** to add one accepted ingredient to a rack, oven or empty Fermenter.
- Recipe requirement counts are shown as the required amount with `inventory:storage` on the line below, for example `5` with `10:42` below (the `5` moves up a line so the counts fit): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
- Multi-input cooking stations and Fermenters support **F8** selection, automatic priorities and **Never auto-add** exclusions. Priorities choose a type across inventory and storage; inventory is used first within that type.
- Collect cooked food and finished mead normally. Collection does not also load another ingredient.
- Normal recipes, station requirements, fire, shelter, capacity and storage permissions still apply. Busy or inaccessible storage is skipped.
- Compatible modded cooking stations and Fermenter conversions use their current accepted inputs.
- Nothing is filled automatically when the world loads or while stations run.

This mod supplies ingredients, not fuel. **Frost Foundry casts stay manual**; CookFromStorage does not load them from storage.

## Choose what's on the menu: F8

At a multi-input cooking rack, oven ingredient switch or Fermenter, aim at the ingredient input and press **F8**. The menu isn't used for fuel, food recipes, single-input stations or Frost Foundry casts.

1. **Pick an ingredient:** Highlight an available item and choose **Use highlighted**. Nothing moves yet; press **E** to add that exact type. It won't substitute another ingredient if you run out.
2. **Set your preferences:** On each item, toggle **Auto** to **Never auto-add** to save it from automatic use. Use **Up / Down** to change priority: lower numbers are used first.
3. **Let Auto choose:** Select **Use automatic choice**. When you press E, Auto chooses the first available allowed type, checking inventory first and then nearby storage for that type.

For example, save your Neck Tails for a recipe by setting them to **Never auto-add**, while leaving other ingredients available for Auto. Change your mind? You can still explicitly select an excluded ingredient or use an item manually.

The menu shows your discovered ingredients and their inventory and storage counts, including discovered items you don't currently have. Undiscovered items stay hidden and aren't chosen by Auto. Preferences save immediately, even if you close the menu, and are shared by stations of the same type for that character. An exact choice is temporary; leaving the input without holding E or switching stations clears it. **Close** leaves your current choice unchanged.

Where vanilla repeats held E, such as an oven ingredient switch, you can briefly aim away and back without stopping. Auto moves to the next available allowed type when one runs out; an exact choice stops instead. This doesn't add held-E support to racks or Fermenters where vanilla doesn't repeat it. Collecting finished food or mead still takes priority over adding an ingredient.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, which the mod manager installs for you.

For manual installation, place `CookFromStorage.dll` and the included `ValheimMods.Common.dll` together in `BepInEx/plugins/CookFromStorage/`. Keep only one installed copy of each DLL.

Install on each player's client; no server plugin is required.

## Configuration

Settings are in `BepInEx/config/hoose.valheim.cookfromstorage.cfg`; each setting's description lists what it controls.

- `Automation/Automatic ingredients` - use storage ingredients for food recipes, cooking stations and Fermenters. Enabled by default.
- `Storage/Storage radius` - how far your storage is searched, from 1 to 100 meters. Default 20.
- `Input selection/Enabled` - shared F8 type selection and per-character/per-station-type priorities/exclusions. Enabled by default while Automatic ingredients is on; disable for legacy inventory-first behavior.
- `Input selection/Open key` - picker key; default F8. Aim at ingredients or a Fermenter, not fuel.

The picker also works without SupplyFromStorage. Disabling input selection restores legacy inventory-first behavior, without the automatic exclusions.

## Stack with other mods

Works well with [SupplyFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/SupplyFromStorage) (keeps your cooking fires, oven and machines fueled from storage), [CraftFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CraftFromStorage) (non-food recipes and equipment upgrades) and [AutoRepair](https://github.com/dhamhuis/ValheimMods/tree/main/AutoRepair) (repairs your gear when you use a station). CookFromStorage and CraftFromStorage share one recipe engine, so ingredients are never counted or consumed twice. Every storage mod uses the same 20 m default radius, and each can be set separately.

If you enjoy these mods and would ever like to support my work, [I'm on Ko-fi](https://ko-fi.com/hoose_builds). Thanks for using them!

[Project and support](https://github.com/dhamhuis/ValheimMods)