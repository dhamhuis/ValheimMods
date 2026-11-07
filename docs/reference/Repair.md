# Station repair

Status: Verified - installed signatures and repository implementation only.
Last verified: 2026-11-06.
Evidence: Targeted Mono.Cecil inspection of `assembly_valheim.dll` at the [baseline](Libraries.md); [repair patch](../../AutoRepair/src/CraftingStationRepairPatch.cs), [startup registration check](../../AutoRepair/src/AutoRepairPlugin.cs), and [stub integration scenarios](../../AutoRepair/tests/Integration/StationInteractionTests.cs).
Limitations: Test scenarios were inspected, not run for this documentation change. No game-body inspection or in-game repair verification performed.

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

The two private `InventoryGui` members are callable in production source through the publicized game reference. Their names/signatures are verified, but their internal eligibility rules and side effects have not been independently established by this reference.

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

- Exact method-body rules for station level, materials, DLC, durability, and repair effects/messages.
- Interaction with other repair or crafting mods and changes in game UI timing.
- Live evidence for worn/broken gear, wrong/unusable stations, and multiplayer. Use the [development checklist](../Development.md) and record actual results before claiming verification.