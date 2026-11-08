# CookFromStorage

CookFromStorage by Hoose cooks, prepares food and brews drinks using nearby storage. It supports food-station recipes and adds one accepted ingredient per cooking-rack/oven or Fermenter interaction. The player's inventory remains first; repeated E interactions can fill additional cooking slots.

## Scope

- Active cooking stations using Valheim's `CookingStation` component and its current conversion list, including compatible modded stations. The Frost Foundry is explicitly excluded: its casts remain manual player choices.
- Cauldron, Food Preparation Table and Mead Ketill recipe availability, ingredient display and actual crafting use accessible storage, without requiring CraftFromStorage.
- Fermenters load one accepted mead base when you press E on an empty, sheltered Fermenter and have no accepted base in inventory. The current conversion list determines accepted inputs, including modded conversions.
- One ingredient per valid rack/oven or Fermenter interaction, taken from locally owned accessible storage in an inclusive sphere centered on the local player. The radius is configurable from 1 to 100 meters (default **20 meters**). Stations themselves must be within 20 meters regardless of the storage radius.
- Vanilla cooked-item pickup, player-inventory ingredients, explicitly selected ingredients, slot capacity, required-fire checks, station acceptance and recipe/conversion configuration remain in force.
- Storage access, ward checks, busy state, cart use and ownership are checked before mutation. Racks/ovens and Fermenters skip remote-owned stations and containers; food recipes use the shared ownership-request workflow before consuming remote-owned storage.
- Shared container tracking and access checks come from `ValheimMods.Common.dll`, included in the prepared package and build/deploy output.

Recipe storage uses the shared `ValheimMods.Common` planning/counting/ownership engine. The food category is identified by verified station keys `$piece_cauldron`, `$piece_preptable` and `$piece_meadcauldron`; all other recipe stations remain vanilla unless CraftFromStorage is installed. Both plugins can coexist without duplicate recipe hooks. Food recipes preserve station levels, fire/shelter, recipe knowledge, output capacity, upgrades, quality, alternatives and multicraft rules. Unknown modded recipe-station keys are not automatically classified as food.

No automatic fueling, Smelter processing inputs, station updates, world-load filling or new hotkeys. SupplyFromStorage continues to handle fuel and Smelter inputs separately.

Fermenter roof, exposure, ward access and empty-state checks remain in force. Normal fermentation duration and finished-mead collection are unchanged; collecting a batch does not also load another base. Explicit item use remains vanilla. Storage insertion preserves the base's cheat flag and confirms the Fermenter's recorded input before reporting success.

The implementation adds one item only after vanilla has no finished item to collect and no accepted player-inventory ingredient. This avoids automating over an explicit vanilla interaction. If storage insertion is rejected, the source stack is restored when possible and the failure is logged.

When storage has an accepted ingredient but the station cannot detect its required lit fire or has no free cooking slot, the mod shows the game's fire/room message instead of claiming there are no cookable items. No ingredient is consumed. Cooking racks require a burning campfire/hearth positioned where the rack can detect it. Failed storage requests log the station name and the blocking fire/capacity, station eligibility, or accessible-container/accepted-item information in the profile's `BepInEx/LogOutput.log`.

## Installation

Requires BepInExPack_Valheim. Install on each client that wants this behavior; no server plugin is required.

For manual installation, place `CookFromStorage.dll` and `ValheimMods.Common.dll` together in `BepInEx/plugins/CookFromStorage/`. Keep only one copy of each DLL.

Configuration is in `BepInEx/config/hoose.valheim.cookfromstorage.cfg`:

- `Automation/Automatic ingredients` (default `true`): enable food-recipe storage crafting and one accepted storage ingredient per valid rack/oven or Fermenter interaction.
- `Storage/Storage radius` (default `20`, range `1`-`100` meters): how far around the player CookFromStorage searches for storage. Cauldron, Food Preparation Table and Mead Ketill recipes use this setting even when CraftFromStorage is installed.

Each setting's description in the config file lists the stations and items it controls.

This is a local preview. Stub tests and a successful build do not prove live prefab setup, Harmony startup, multiplayer behavior or persistence.

## Development

```powershell
.\CookFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CookFromStorage
dotnet run --project .\CookFromStorage\tests\CookFromStorage.Tests.csproj --configuration Release
.\CookFromStorage\scripts\build.ps1 -Deploy
.\scripts\Render-ModIcon.ps1 -ModDirectory .\CookFromStorage
```

See the [repository development guide](../docs/Development.md) and [processing API reference](../docs/reference/Processing.md) for build steps, API evidence and validation limits. Test the supported station prefabs, input order, multiplayer ownership and saved item state in game before release. For Fermenters, check an accepted stored base with empty inventory, inventory-first input, missing roof/exposure, a running batch, finished-mead collection without reloading, and save/reload persistence.

[Source and support](https://github.com/dhamhuis/ValheimMods)