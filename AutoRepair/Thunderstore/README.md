# AutoRepair

**Open a crafting station and get back to adventuring with repaired gear. No repeated repair-button clicks.**

## Supported repair stations

- **Workbench** - repair eligible early-game tools, weapons and armor.
- **Forge** - repair eligible metal equipment.
- **Black Forge** - repair eligible advanced weapons, armor and tools.
- **Galdr Table** - repair eligible magical weapons and mage armor.

Compatible modded crafting stations are also supported when their equipment is repairable there under the game's normal rules.

AutoRepair repairs eligible damaged equipment in your inventory when you interact with its station, including unequipped items. Use the station your gear normally requires.

By Hoose.

## Vanilla rules still apply

- Equipment must be repairable at the current station and meet its normal requirements.
- Forge-only gear is not repaired at a workbench.
- Undamaged items are left alone, and repair costs are unchanged.
- Buildings are not repaired.

The mod uses Valheim's own repair methods. Automatic repairs happen on station interaction, not continuously while its panel is open.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**, which the mod manager installs for you.

For manual installation, copy `plugins/AutoRepair.dll` into `BepInEx/plugins/AutoRepair/`.

AutoRepair runs on each player's client; a server installation is not required.

## Configuration

None needed. AutoRepair works as soon as it is installed.

## Stack with other mods

Works well with [CraftFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CraftFromStorage) (craft and upgrade from nearby chests), [CookFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/CookFromStorage) (cooking and brewing from storage) and [SupplyFromStorage](https://github.com/dhamhuis/ValheimMods/tree/main/SupplyFromStorage) (fuel and machine inputs from storage). Walk up to and use the Forge: your gear is repaired, and the upgrade materials come straight from your chests.

[Project and support](https://github.com/dhamhuis/ValheimMods)