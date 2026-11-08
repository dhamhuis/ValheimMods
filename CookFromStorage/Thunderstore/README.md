# CookFromStorage

**Cook meals and brew mead straight from nearby chests. Leave the ingredients in storage, not in your pockets.**

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
- Your inventory is used first. Ingredients can stay in storage until needed.
- Collect cooked food and finished mead normally. Collection does not also load another ingredient.
- Normal recipes, station requirements, fire, shelter, capacity and storage permissions still apply. Busy or inaccessible storage is skipped.
- Compatible modded cooking stations and Fermenter conversions use their current accepted inputs.
- Nothing is filled automatically when the world loads or while stations run.

This mod supplies ingredients, not fuel. **Frost Foundry casts stay manual**; CookFromStorage does not load them from storage.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, which the mod manager installs for you.

For manual installation, place `CookFromStorage.dll` and the included `ValheimMods.Common.dll` together in `BepInEx/plugins/CookFromStorage/`. Keep only one installed copy of each DLL.

Install on each player's client; no server plugin is required.

## Configuration

Settings are in `BepInEx/config/hoose.valheim.cookfromstorage.cfg`; each setting's description lists what it controls.

- `Automation/Automatic ingredients` - use storage ingredients for food recipes, cooking stations and Fermenters. Enabled by default.
- `Storage/Storage radius` - how far your storage is searched, from 1 to 100 meters. Default 20.

## Stack with other mods

Works well with [SupplyFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/SupplyFromStorage) (keeps your cooking fires, oven and machines fueled from storage), [CraftFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CraftFromStorage) (non-food recipes and equipment upgrades) and [AutoRepair](https://github.com/dhamhuis/ValheimMods/tree/main/AutoRepair) (repairs your gear when you use a station). CookFromStorage and CraftFromStorage share one recipe engine, so ingredients are never counted or consumed twice. Every storage mod uses the same 20 m default radius, and each can be set separately.

[Project and support](https://github.com/dhamhuis/ValheimMods)