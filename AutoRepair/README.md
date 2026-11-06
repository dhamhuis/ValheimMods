# AutoRepair

AutoRepair by dhamhuis repairs eligible equipment in the local player's inventory when they interact with a crafting station. It uses Valheim's normal repair methods rather than changing item durability directly.

The [Thunderstore README](Thunderstore/README.md) is the player-facing page included in the release package. This README describes the mod's behavior and implementation.

## Behavior

| Aspect | Behavior |
| --- | --- |
| Trigger | Postfix on `CraftingStation.Interact` for the local player. |
| Items | Damaged inventory items, including unequipped equipment, eligible under vanilla repair checks. |
| Station rules | Uses `InventoryGui.HaveRepairableItems()` and `RepairOneItem()` for the current station's eligibility and usability rules. |
| Example | Forge-only iron gear is not repaired at a workbench. |
| Repair cost | Unchanged from vanilla. |

It does not repair buildings, increase station levels, or bypass vanilla equipment eligibility. It runs at station interaction, not continuously while the crafting panel is open.

## Implementation

- [AutoRepairPlugin](src/AutoRepairPlugin.cs) initializes Harmony, verifies its postfix registration, and removes its patches on teardown.
- [CraftingStationRepairPatch](src/CraftingStationRepairPatch.cs) invokes vanilla repair methods with an attempt limit equal to the inventory item count. If eligible items remain at that limit, it stops and logs a warning.

Startup logs distinguish a loaded plugin from a verified repair patch. Repair logs count attempts, not guaranteed successful repairs.

## Development and testing

See the [development guide](../docs/Development.md) for building, deployment, and package creation. Runtime changes should be checked with broken, partially damaged, and undamaged gear; wrong-station equipment; and unusable stations. Compilation alone does not verify Harmony behavior in-game.

Release notes are maintained in [CHANGELOG.md](CHANGELOG.md).