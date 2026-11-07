# CraftFromStorage

By Hoose.

Craft and upgrade at Valheim crafting stations using ingredients in nearby storage, without carrying them in your inventory.

## Behavior

- Fixed **20 meter sphere around the player**, not around the station.
- Uses your inventory first, then nearby accessible chests, carts and ship storage, nearest first.
- Includes cauldron recipes and equipment upgrades; ingredients may be split across several containers.
- Respects chest ownership, wards and normal recipe/station rules. Busy storage is skipped.
- Crafting requirement colors include available storage materials. Crafted output stays in your inventory.

Does **not** add hand crafting from storage, hammer building, auto-repair, fueling, automatic cooking/processing, sorting, or stashing. No hotkeys or configuration are needed.

## Installation

Requires BepInExPack_Valheim. Install on your client; a server-only install does not enable it.

For manual installation, copy `plugins/CraftFromStorage.dll` into `BepInEx/plugins/CraftFromStorage/`. Keep only one installed copy. Do not install another craft-from-storage mod alongside it.

Remote-owned storage may add a short delay while ownership is requested. Missing, denied or unresponsive storage cancels crafting without spending ingredients; retry after resolving access. Normal inventory capacity requirements still apply.

## Validation

Tested locally in game. Multiplayer ownership, simultaneous access and inventory persistence still require dedicated validation; test with your setup before relying on it in a shared world. Packaging does not publish the mod to Thunderstore.

[Source and development notes](https://github.com/dhamhuis/ValheimMods)