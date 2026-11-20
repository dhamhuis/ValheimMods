# SupplyFromStorage

SupplyFromStorage by Hoose adds one accepted fuel or processing input from nearby accessible storage when you use a machine's default E interaction. It is a separate BepInEx plugin from CraftFromStorage.

## Supported machines

- **Fire and light fuel:** campfires, bonfires, hearths, fireplaces, braziers and torches using Valheim's `Fireplace` component.
- **Cooking-station fuel:** cooking stations, hanging and iron cooking stations, and the stone oven when its `CookingStation` fuel slot is enabled. Cooking ingredients are never transferred.
- **Smelter fuel:** smelters and blast furnaces using Valheim's `Smelter` fuel slot, the Eitr Refinery (sap), the Hot Tub (wood) and Frost Kiln (ice), which use the same component. The Frost Foundry's liquid-frost slot uses the `CookingStation` fuel path; its casts stay manual.
- **Processing inputs:** the Charcoal Kiln accepts Wood, Fine Wood and Core Wood; smelters and blast furnaces accept their configured ores/scrap; the Windmill accepts barley; the Spinning Wheel accepts flax; and the Eitr Refinery accepts Soft Tissue. Each prefab's current accepted conversion entries determine which items can be supplied. The Battering Ram's engine uses the same input path and accepts Wood, Fine Wood, Core Wood and Ashwood.

With input selection disabled, the normal E interaction uses an accepted item from the player's inventory first, and vanilla consumes that item. If the player has no accepted item in inventory, SupplyFromStorage falls back to an eligible item in nearby accessible storage; that fallback consumes the item from the container, not the player's inventory. The selection mode below, now enabled by default for new configs, overrides this type-selection behavior for the Charcoal Kiln, Smelter, Blast Furnace and Battering Ram engine inputs only. Existing saved settings are respected. The mod does not load cooking ingredients, fermenter bases, outputs or unrelated crafting ingredients. Fuel-from-storage and processing-input-from-storage have separate BepInEx configuration switches, both enabled by default.

## Access and multiplayer behavior

Storage must be within an inclusive sphere centered on the local player whose radius is configurable from 1 to 100 meters (default **20 meters**). Machines must be within 20 meters regardless of the storage radius. Storage access, ward checks, busy state and cart use follow CraftFromStorage's container rules. Transfers recheck range, access, ownership, station capacity and item quantity immediately before removing a storage item. A station's vanilla acceptance/conversion data is checked before insertion. Each valid E action transfers at most one item; holding E repeats according to the game's interaction repeat interval. No items are supplied during station updates or when the world loads. Failed, full, unavailable, out-of-range and no-eligible-item attempts are briefly rate-limited; successful transfers are not throttled.

For Smelter inputs, the game normally reports “You don't have any processable items” before a postfix can supply from storage. SupplyFromStorage checks storage before vanilla takes that branch, suppresses the message when it successfully adds an item, and preserves vanilla inventory handling and the full-station message.

Fireplace and fuel-switch requests check storage before vanilla reports missing fuel. Successful storage fueling shows the normal added-fuel message without first claiming you have no fuel, including empty and fueled fires; inventory-first input, fire toggles, manual items and full-station feedback remain unchanged. This includes the Frost Kiln's ice and Frost Foundry's liquid frost.

A full fire reports that no more fuel can be added when accessible storage has its fuel, even if your inventory is empty. No storage fuel is consumed; if neither inventory nor accessible storage has fuel, vanilla's missing-fuel message remains.

For safety, SupplyFromStorage transfers only when the local client owns both the source container and the machine. It does not request ownership, open containers, or modify remote-owned inventories. As a result, storage owned by another player is skipped until normal gameplay gives the local client ownership. Multiplayer behavior, including ownership changes and simultaneous access, still requires in-game testing.

CraftFromStorage and SupplyFromStorage have separate plugins and do not share transfer hooks. CraftFromStorage patches recipe checks and string-based inventory consumption; SupplyFromStorage patches only the machine's E-action callbacks, and supplies from storage only if vanilla did not already handle that action from player inventory. It removes a specific item from its source container and invokes the machine's vanilla fuel/input RPC only when the local client owns it. These distinct hooks avoid double insertion through CraftFromStorage's crafting hooks. Disable other mods that automate the same machine inputs.

## Installation

Requires BepInExPack_Valheim. Install on the client; a server-only install does not enable the behavior.

For manual installation, copy `plugins/SupplyFromStorage.dll` and `plugins/ValheimMods.Common.dll` into `BepInEx/plugins/SupplyFromStorage/`. Keep the shared library beside the plugin and only one installed copy of each DLL.

Configuration is in `BepInEx/config/hoose.valheim.supplyfromstorage.cfg`:

- `Automation/Automatic fuel` (default `true`): enable one fuel item from storage per valid machine E action.
- `Automation/Automatic processing inputs` (default `true`): enable one processing input from storage per valid machine E action.
- `Input selection/Enabled` (default `true`): enable discovered-input selection and preferences for the supported multi-input processors while Automatic processing inputs is enabled. Existing saved `false` values remain disabled; enable explicitly when upgrading from the earlier opt-in milestone.
- `Storage/Storage radius` (default `20`, range `1`-`100` meters): how far around the player SupplyFromStorage searches for storage.

Each setting's description in the config file lists the machines and items it controls.

## Experimental processing-input selection

Selection is **enabled by default for new configs** and covers the **Charcoal Kiln, Smelter, Blast Furnace and Battering Ram engine**. Saved disabled settings are not overridden; upgrading from the earlier opt-in milestone may require setting `Input selection/Enabled = true`. Separate fuel callbacks remain unchanged. The kiln core/regression, furnace behavior and BR.1-BR.82 ram scenarios passed by user report. The post-checklist localized item-name message correction was confirmed by the user. Multiplayer, controllers and broader edge cases remain unverified. Keep `Automation/Automatic processing inputs` enabled. The old preview setting alone still cannot change E behavior.

- Aim at a supported station's material input (not coal/fuel), or the ram's engine-loading input at the back (not its operating interaction), and press **F8** (`Input selection preview/Open key`). Click a tile or use Left/Right / D-pad, then **Use highlighted**, Enter or controller Use to arm that exact type. Choosing consumes nothing; subsequent E actions insert one item each.
- F8 shows only inputs discovered by the current character, including discovered types with zero available items. Unknown types have no tiles, placeholders or hidden-item count. New discoveries appear on refresh. Auto also skips undiscovered inputs in inventory and storage; unknown-only availability blocks insertion instead of falling back to vanilla. Normal explicit item-use remains unchanged.
- **Auto** clears the explicit selection. Escape/B/F8/Cancel dismisses without changing that selection. Preference edits, unlike highlighting, save immediately even if you cancel.
- Each type's **Auto / Never auto-add** button toggles its automatic exclusion. The priority number appears between **Up / Down**, which move it one priority position; repeated moves can create any precedence order. Empty types remain editable. With nothing accepted available, the game's normal no-items message appears; if available items are all excluded, `Items set to Never auto-add (F8)` appears. Additional modded inputs use pages; new unconfigured types are ordered deterministically after ranked types.
- In Auto mode, the highest-priority available allowed **type** wins across inventory and storage. Inventory is preferred only within that type. Thus Wood in a chest can beat Fine Wood in inventory. Exclusions protect both sources; explicit picker selection or normal manual item-use can intentionally override them.
- Holding E continues like vanilla. Briefly aiming off the input (at nothing or a non-station object) pauses insertion and keeps the hold, its current type and any explicit selection; aiming back resumes without a message or release. Aiming at a different supported station while holding restarts there with that station's Auto choice. In Auto, the held type continues from inventory into storage and, when exhausted, moves to the next-priority available, allowed and discovered type; exclusions still apply. An explicit selection never substitutes: exhaustion stops it. Fullness, unavailable sources, lost access or opening another UI stop the hold; release and press again to retry. No background filling loop is added.
- Explicit selection is temporary: leaving the input when not holding E, changing to another station, death, teleport, disconnect or unload clears it. The near-target hint shows the mode, type, counts and actual picker binding. Counts are advisory and rechecked at insertion.
- Priorities/exclusions are saved locally per character and station type, not per placed machine. Distinct sections `Charcoal Kiln preferences`, `Smelter preferences`, `Blast Furnace preferences` and `Battering Ram preferences` contain `Player <ID>` entries encoding `priority|exclusions` with comma-separated prefab IDs. Existing settings are preserved; the defaults below apply to new entries. Ram IDs are `Wood`, `FineWood`, `RoundLog` (Corewood) and `Blackwood` (Ashwood). Unknown IDs are retained. Malformed or unsavable preferences block controlled insertion and produce a log/message rather than using unrestricted defaults.
- Hidden inputs retain their saved priorities/exclusions. Priority numbers and Up/Down moves use only discovered types; hidden settings are not deleted or reset. All types remain in the underlying defaults/config, but discovery is required before picker selection or automatic use.
- Disabling `Input selection/Enabled` **or** `Automation/Automatic processing inputs` restores legacy inventory selection, so exclusions no longer protect vanilla E. The feature does not affect explicit manual item-use.

Fresh automatic priorities, from first to last (no default exclusions):

| Station | Default priority |
| --- | --- |
| Smelter | Copper Ore, Copper Scrap, Tin Ore, Scrap Bronze, Iron Ore, Scrap Iron, Silver Ore |
| Blast Furnace | Black Metal Scrap, Flametal Ore, Petrified Tissue |
| Charcoal Kiln | Wood, Finewood, Corewood |
| Battering Ram | Wood, Finewood, Corewood, Ashwood |

Display order matches default priority except for the Smelter, whose tiles remain Copper Ore, Tin Ore, Iron Ore, Silver Ore, Copper Scrap, Scrap Bronze, Scrap Iron. Only discovered inputs appear, including additional accepted runtime inputs after the built-in types; their automatic priority follows configured types in ordinal prefab-ID order.

Use the [Markdown testing checklist](TESTING.md) to check off individual steps and resume later. It preserves prior kiln/furnace/ram results. All vanilla inputs fit on one page. The inspected Blast Furnace has three distinct input types, including current Flametal ore (`FlametalOreNew`) and Petrified Tissue (`GoldOre`). The ram's engine consumes loaded wood as fuel without producing an item, so compare source counts rather than expecting refined output. Use small stacks and a backed-up world/character for repeat runs. The prepared release ZIP is in `dist/`.

### Non-consuming preview

With `Input selection/Enabled` disabled, the original **non-consuming Charcoal Kiln preview** remains available, also disabled by default. Clicking a type only highlights it in the preview, and E retains its existing selection behavior after closing.

- Set `Input selection preview/Enabled` to `true`, then aim at the Charcoal Kiln's wood-input interaction and press **F8** (`Input selection preview/Open key`).
- The grid displays discovered kiln inputs with separate inventory and eligible-storage counts. Discovered unavailable types remain visible but disabled. Counts and discovery filtering refresh four times per second while open. This preview-only mode does not restrict legacy E insertion.
- Click an available type or use keyboard Left/Right or controller D-pad Left/Right to highlight it. Close with Escape, controller B/Menu, F8 or the Close button.
- Movement/look and normal player interactions are blocked while open. After closing, held use/attack/cancel controls must be released before gameplay input resumes.
- The prototype requires local ownership of the kiln and counted containers, normal storage access and the configured storage radius. It closes when the target becomes invalid or the local player dies, teleports or disconnects. Keyboard opening is supported; controller opening and final native-game styling remain future work.

The user passed the initial opening, icons/counts/highlighting and Escape/control-restoration checks on 2026-11-09. This was the non-consuming preview, not selected-type insertion. Broader input, controller and UI-scale checks remain necessary; builds and helper tests cannot certify live UI behavior.

This is a local preview, not a published or in-game-certified release. Stub tests and a successful build do not prove live-game networking or persistence.

The discovery filter is a subsequent change to the previously passed selection milestone. Its automated checks pass, but the separate [discovery checkpoint](TESTING.md#discovery-filter-checkpoint---pending) still requires in-game testing.

## Development

```powershell
.\SupplyFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\SupplyFromStorage
dotnet run --project .\SupplyFromStorage\tests\SupplyFromStorage.Tests.csproj --configuration Release
```

See the [repository development guide](../docs/Development.md) for build prerequisites, package layout and validation limits.

[Source and support](https://github.com/dhamhuis/ValheimMods)