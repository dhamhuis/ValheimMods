# Station repair

Status: Verified - installed signatures, targeted repair-body and recipe-data inspection, and repository implementation.
Last verified: 2026-11-07.
Evidence: Targeted Mono.Cecil inspection of `assembly_valheim.dll` at the [baseline](Libraries.md), including `InventoryGui.CanRepair`, `HaveRepairableItems`, `RepairOneItem` and `Inventory.GetWornItems`; local UnityPy recipe inspection described below; [repair patch](../../AutoRepair/src/CraftingStationRepairPatch.cs), [startup registration check](../../AutoRepair/src/AutoRepairPlugin.cs), and [stub integration scenarios](../../AutoRepair/tests/Integration/StationInteractionTests.cs).
Limitations: Test scenarios were inspected, not run for this documentation-only change. Game-body and serialized-data inspection do not establish live per-station repair success, modded recipe assignments or multiplayer behavior.

## Use when

Repairing equipment on station interaction, choosing a repair hook, or checking why a station interaction must not trigger repair.

## API and behavior

### Installed signatures

| Member | Original visibility / return | Parameters |
| --- | --- | --- |
| `CraftingStation.Interact` | Public instance / `bool` | `Humanoid user, bool repeat, bool alt` |
| `Player.GetCurrentCraftingStation` | Public instance / `CraftingStation` | None. |
| `InventoryGui.HaveRepairableItems` | Private instance / `bool` | None. |
| `InventoryGui.RepairOneItem` | Private instance / `void` | None. |
| `Inventory.GetAllItems` | Public instance / `List<ItemDrop.ItemData>` | None; other overloads also exist. |

The two private `InventoryGui` members are callable in production source through the publicized game reference.

### Vanilla equipment-repair stations

The complete ordinary vanilla equipment-repair list for the inspected recipe data is:

| Station | Station key |
| --- | --- |
| Workbench | `$piece_workbench` |
| Forge | `$piece_forge` |
| Black Forge | `$piece_blackforge` |
| Galdr Table | `$piece_magetable` |

On 2026-11-07, read-only UnityPy inspection enumerated all 481 recipe assets under the manifest's `Assets/GameElements/Recipes/` paths in SoftRef bundle `c4210710`, SHA-256 `84F55CFB24AF51B9F0494FD9D1A355BF009E08545FF8CCF607C7D88D2E4E1EC8`. It followed output `ItemDrop` fields and both `Recipe.m_craftingStation` and `Recipe.m_repairStation` pointers. Of these, 278 recipe outputs had both `m_useDurability` and `m_canBeReparied` enabled; their non-null station references contained only the four keys above, including when disabled recipes were retained. `Recipe_Adze` and `Recipe_Chisel` had null outputs and were excluded; no other pointers remained unresolved. Classification used serialized field layouts, not resolved MonoScript class identities.

Both flags matter: many foods and materials have the repairable flag set but do not use durability. Including those outputs would incorrectly identify the Cauldron, food tables, Artisan Table and Stonecutter as equipment-repair stations.

Targeted IL inspection on the baseline game DLL established that `Inventory.GetWornItems` selects durability-using items below maximum durability. `HaveRepairableItems` checks current station usability and passes those items to `CanRepair`. The normal `CanRepair` path requires the repairable flag and an item recipe, accepts a matching crafting **or** repair station `m_name`, and checks the recipe's minimum station level against the station level capped at four. The body also has no-cost and lower-world-level exceptions; these are not evidence that ordinary current-world equipment belongs to additional repair stations. AutoRepair delegates to those vanilla rules and has no hardcoded station whitelist, so compatible modded equipment/stations can extend the list.

### Deep North scope

The 481-recipe scan above included northern gear; for example, `Recipe_SwordGold` assigns `$piece_blackforge` as its crafting station and no separate repair station. No additional station key appeared among durable, repairable recipe outputs. This is stronger than a pre-Deep-North station list, but it does not establish every final conversion output's runtime ObjectDB recipe lookup.

The installed bundle also contains `UpgradeStation` with `$piece_upgradestation`, `m_upgrader=1`, `m_hasCraftTab=0` and `m_canRepair=1`. It shares the CraftingStation field layout but is not assigned by the scanned repairable recipes. A repair-enabled UI flag alone does not make it an ordinary repair destination: `CanRepair` still applies recipe station matching, station level and the documented no-cost/lower-world-level exceptions. Its localized player-facing name and per-item runtime behavior remain unverified. The Frost Foundry uses the oven-style processing component rather than CraftingStation; see [northern machine evidence](Processing.md#deep-north-compatibility-findings).

### Existing flow and policy

The postfix runs after `CraftingStation.Interact`. It requires a non-repeat interaction by `Player.m_localPlayer` and checks that player's current crafting station equals the interacted station. It also requires `InventoryGui.instance` and the player's inventory. This uses resulting station state instead of assuming every interaction succeeded.

It repeatedly calls the game's repair helpers while repairable items remain, bounded by the initial inventory item count. It logs the number of attempts and warns if repairable items remain at the limit. The limit prevents an unbounded loop if another patch or changed API fails to make progress; an attempt is not proof of a successful repair.

Use this existing flow for station-triggered repair. Do not substitute direct durability writes without investigating the game's eligibility, effects, and persistence behavior.

## Pitfalls

- Do not repair for remote players, held/repeated interactions, or a station the local player did not successfully enter.
- Do not assume `Interact` returning `true` alone establishes the relevant GUI/station state.
- Do not assume all inventory items are eligible for the current station; defer to the game helper pending deeper inspection.
- Registration inspection verifies that the patch is installed, not that it produces correct effects in game.

## Unknowns

- Full repair effects/messages, DLC interactions and persistence beyond the inspected worn-item and station-eligibility paths.
- Interaction with other repair or crafting mods and changes in game UI timing.
- Live evidence for worn/broken gear, wrong/unusable stations, and multiplayer. Use the [development checklist](../Development.md) and record actual results before claiming verification.