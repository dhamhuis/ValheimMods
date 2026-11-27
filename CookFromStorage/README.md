# CookFromStorage

Cook meals and brew mead straight from nearby chests. CookFromStorage by Hoose supports food-station recipes and adds one accepted ingredient per cooking-rack/oven or Fermenter interaction. Multi-input cooking stations and Fermenters share the F8 input picker with SupplyFromStorage: choose an exact type or configure automatic priorities/exclusions. Inventory is preferred within the chosen type; repeated E interactions can fill additional cooking slots.

## Scope

- Active cooking stations using Valheim's `CookingStation` component and its current conversion list, including compatible modded stations. The Frost Foundry is explicitly excluded: its casts remain manual player choices.
- Cauldron, Food Preparation Table and Mead Ketill recipe availability, ingredient display and actual crafting use accessible storage, without requiring CraftFromStorage.
- Recipe requirement counts are shown as the required amount with `inventory:storage` on the line below, for example `5` with `10:42` below (the `5` moves up a line so the counts fit): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
- Fermenters load one accepted mead base when you press E on an empty, sheltered Fermenter. The current conversion list determines accepted inputs, including modded conversions.
- One ingredient per valid rack/oven or Fermenter interaction, taken from locally owned accessible storage in an inclusive sphere centered on the local player. The radius is configurable from 1 to 100 meters (default **20 meters**). Stations themselves must be within 20 meters regardless of the storage radius.
- Vanilla cooked-item pickup, player-inventory ingredients, explicitly selected ingredients, slot capacity, required-fire checks, station acceptance and recipe/conversion configuration remain in force.
- Storage access, ward checks, busy state, cart use and ownership are checked before mutation. Racks/ovens and Fermenters skip remote-owned stations and containers; food recipes use the shared ownership-request workflow before consuming remote-owned storage.
- Shared container tracking and access checks come from `ValheimMods.Common.dll`, included in the prepared package and build/deploy output.

Recipe storage uses the shared `ValheimMods.Common` planning/counting/ownership engine. The food category is identified by verified station keys `$piece_cauldron`, `$piece_preptable` and `$piece_meadcauldron`; all other recipe stations remain vanilla unless CraftFromStorage is installed. Both plugins can coexist without duplicate recipe hooks. Food recipes preserve station levels, fire/shelter, recipe knowledge, output capacity, upgrades, quality, alternatives and multicraft rules. Unknown modded recipe-station keys are not automatically classified as food.

No automatic fueling, Smelter processing inputs, station updates or world-load filling. SupplyFromStorage continues to handle fuel and Smelter inputs separately.

Fermenter roof, exposure, ward access and empty-state checks remain in force. Normal fermentation duration and finished-mead collection are unchanged; collecting a batch does not also load another base. Explicit item use remains vanilla. Storage insertion preserves the base's cheat flag and confirms the Fermenter's recorded input before reporting success.

Finished output is always collected before considering a new input. With selection disabled, the legacy inventory-first behavior remains: storage is used only when no accepted player-inventory ingredient exists. With selection enabled, automatic type priority spans both sources, and the player's exact chosen type is consumed before that type is searched in storage. If insertion is rejected, the source stack is restored when possible and the failure is logged.

When storage has an accepted ingredient but the station cannot detect its required lit fire or has no free cooking slot, the mod shows the game's fire/room message instead of claiming there are no cookable items. No ingredient is consumed. Cooking racks require a burning campfire/hearth positioned where the rack can detect it. Failed storage requests log the station name and the blocking fire/capacity, station eligibility, or accessible-container/accepted-item information in the profile's `BepInEx/LogOutput.log`.

## Installation

Requires BepInExPack_Valheim. Install on each client that wants this behavior; no server plugin is required.

For manual installation, place `CookFromStorage.dll` and `ValheimMods.Common.dll` together in `BepInEx/plugins/CookFromStorage/`. Keep only one copy of each DLL.

Configuration is in `BepInEx/config/hoose.valheim.cookfromstorage.cfg`:

- `Automation/Automatic ingredients` (default `true`): enable food-recipe storage crafting and one accepted storage ingredient per valid rack/oven or Fermenter interaction.
- `Storage/Storage radius` (default `20`, range `1`-`100` meters): how far around the player CookFromStorage searches for storage. Cauldron, Food Preparation Table and Mead Ketill recipes use this setting even when CraftFromStorage is installed.
- `Input selection/Enabled` (default `true`): enable the shared input picker, exact-type selection and automatic priorities/exclusions for multi-input cooking stations and Fermenters. Requires `Automatic ingredients`; disable for legacy behavior.
- `Input selection/Open key` (default `F8`): open the picker while aiming at the cooking ingredient input or Fermenter, not a separate fuel switch.

Each setting's description in the config file lists the stations and items it controls.

## Input selection

The shared picker uses the same compact layout, counts, tooltips and controls as SupplyFromStorage. It works with CookFromStorage alone or both mods installed; there is one picker and one common input-patch set.

- Aim at a rack itself, an oven's ingredient switch, or a Fermenter and press F8. Single-input stations, recipe stations and Frost Foundry casts do not expose this picker.
- Only inputs discovered by the current character appear, including discovered inputs with zero availability. No hidden tiles, counts or placeholders reveal future inputs. New discoveries appear on refresh; knowing only one input does not disable a multi-input station's picker.
- Choose a tile and **Use highlighted** (Enter) to arm that exact type; no item moves until E. **Use automatic choice** clears the manual choice. Escape/B/F8 closes without changing it.
- **Auto / Never auto-add** toggles automatic eligibility. **Up / Down** changes type priority across both inventory and storage; inventory wins within the selected type. Explicit picker selection and ordinary explicit item use can override exclusions. With nothing accepted available, the game's normal no-items message appears; if available items are all excluded, `Items set to Never auto-add (F8)` appears.
- New preferences rank the station's distinct current conversions in their existing order, with no exclusions. Modded inputs remain visible and editable; additional types fall after saved ranks in ordinal prefab-ID order. Larger lists use pages of eight.
- Preferences save per character and station type in `Input preferences <station-name token>` sections, as `Player <ID> = priority|exclusions`. Different placed stations of the same type share settings; wooden/iron cooking stations, ovens and Fermenters have independent settings. Invalid or unsavable preferences block controlled insertion and log/notify rather than falling back.
- Auto skips undiscovered inputs in inventory and storage, even if they have saved ranks. Unknown-only availability blocks insertion rather than falling through to vanilla. Hidden priorities/exclusions are preserved; displayed ranks and Up/Down operate on discovered inputs only. Normal explicit item-use remains vanilla. Disabling selection restores legacy behavior without this discovery restriction.
- Leaving the target while not holding E, aiming at another station or player lifecycle changes clears manual selection. Where vanilla repeats held E (oven ingredient switches), held input continues like vanilla: briefly aiming off the input and back resumes without releasing, another station restarts with its Auto choice, and Auto moves to the next-priority available type when the current one runs out. An explicit selection never substitutes and stops on exhaustion. Missing fire, fullness, lost access or opening another UI stops that hold until release. This does not add held-repeat support to racks or Fermenters where vanilla rejects holding E.
- Cooked-food pickup, running batches and finished-mead collection remain vanilla; collection never also loads a new input. Fuel and food-recipe storage crafting are unaffected by selection.

## Development

```powershell
.\CookFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CookFromStorage
dotnet run --project .\CookFromStorage\tests\CookFromStorage.Tests.csproj --configuration Release
.\CookFromStorage\scripts\build.ps1 -Deploy
.\scripts\Render-ModIcon.ps1 -ModDirectory .\CookFromStorage
```

See the [repository development guide](../docs/Development.md), [testing checklist](TESTING.md) and [processing API reference](../docs/reference/Processing.md) for build steps and verification records.

If you enjoy these mods and would ever like to support my work, [I'm on Ko-fi](https://ko-fi.com/hoose_builds). Thanks for using them!

[Project and support](https://github.com/dhamhuis/ValheimMods)