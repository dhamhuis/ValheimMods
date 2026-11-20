# CookFromStorage

CookFromStorage by Hoose cooks, prepares food and brews drinks using nearby storage. It supports food-station recipes and adds one accepted ingredient per cooking-rack/oven or Fermenter interaction. Multi-input cooking stations and Fermenters share the F8 input picker with SupplyFromStorage: choose an exact type or configure automatic priorities/exclusions. Inventory is preferred within the chosen type; repeated E interactions can fill additional cooking slots.

## Scope

- Active cooking stations using Valheim's `CookingStation` component and its current conversion list, including compatible modded stations. The Frost Foundry is explicitly excluded: its casts remain manual player choices.
- Cauldron, Food Preparation Table and Mead Ketill recipe availability, ingredient display and actual crafting use accessible storage, without requiring CraftFromStorage.
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

This is a local preview. Stub tests and a successful build do not prove live prefab setup, Harmony startup, multiplayer behavior or persistence.

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

### Input-selection checkpoint

Status: Passed by user report on 2026-11-16. All 17 steps below were checked in this file when the user confirmed, "Testing passed." This records the local checkpoint, not independent observation or captured per-step logs. Multiplayer, controller-specific behavior, other resolutions and abnormal failures remain unverified. The tested build was deployed on 2026-11-13; no new assembly inspection, rebuild or deployment was performed to record these results.

Use a backed-up world/character and small stacks. Keep both CookFromStorage and SupplyFromStorage enabled for the shared-provider checks.

- [x] Restart through the test profile; confirm both mods load and Cook reports its shared cooking/Fermenter picker registration without errors.
- [x] At a wooden Cooking Station, open F8 with multiple accepted raw foods split between inventory and an accessible chest. Check localized names, exact counts, all accepted tiles and no consumption while editing.
- [x] Select one available type, close, and press E. Verify exactly one of that type is consumed, using inventory before storage for that same type.
- [x] In Auto, move a stored type above a different carried type. Verify E uses the preferred stored type, not the carried alternative.
- [x] Exclude every available type. Verify Auto consumes neither source. Explicitly select an excluded type and verify E can add exactly that type.
- [x] Exhaust a selected type while another type is available. Verify no substitution and a localized unavailable-type message; release before retrying.
- [x] Check missing-fire/full messages consume nothing. Restore readiness, release/repress and verify insertion works.
- [x] Collect cooked food with ingredients available and exclusions set. Verify collection takes precedence and does not also consume an ingredient.
- [x] Repeat targeting and exact insertion at the Iron Cooking Station and Stone Oven. Oven fuel must not open the picker or change fuel behavior.
- [x] At an empty sheltered Fermenter, verify the accepted base list, counts, paging and priority editing. Select a base and verify one exact inventory/storage base starts a batch.
- [x] Verify missing-roof/exposure checks consume nothing, a running batch accepts no new base, and finished-batch collection does not reload.
- [x] Verify ordinary explicit item use remains vanilla and does not replace the picker's manual choice.
- [x] Alternate F8 between cooking/Fermenter inputs and Supply's kiln/furnaces/ram. Confirm the correct list/settings open each time, only one dialog appears, and closing restores controls without accidental insertion.
- [x] Move between station types while holding use; verify the old type/hold does not carry over. Check Escape/F8 close and release/repress behavior.
- [x] Restart with the same character and verify priorities/exclusions persist separately by station type; another character should receive fresh conversion-order defaults.
- [x] Disable Cook's input selection and verify legacy inventory-first cooking/brewing. Disable Automatic ingredients and verify selection/storage crafting stop while vanilla inventory actions remain available.
- [x] Check the Frost Foundry still has no ingredient picker and no stored casts are automatically loaded; food recipes and Supply fuel handling remain unchanged.

The discovery filter and 2026-11-18 continuous held-input change postdate this checkpoint (step 6's "no substitution" now applies to explicit selections only, and step 14 now expects a hold to restart on the new station). On 2026-11-20 the user reported that in-game testing was complete; individual results for the later [discovery](../SupplyFromStorage/TESTING.md#discovery-filter-checkpoint---pending) and [held-input](../SupplyFromStorage/TESTING.md#continuous-held-input-checkpoint---pending) checklists were not recorded.

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