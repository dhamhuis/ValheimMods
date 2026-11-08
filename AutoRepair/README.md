# AutoRepair

AutoRepair by Hoose repairs eligible equipment in the local player's inventory when they interact with a crafting station. It uses Valheim's normal repair methods rather than changing item durability directly.

The [Thunderstore README](Thunderstore/README.md) is the player-facing page included in the release package. This README describes the mod's behavior and implementation.

## Supported repair stations

AutoRepair supports all four vanilla equipment-repair stations:

- **Workbench**
- **Forge**
- **Black Forge**
- **Galdr Table**

The station must be usable and meet the equipment's normal repair requirements. The Artisan Table, Stonecutter and food recipe stations are not ordinary vanilla equipment-repair stations; upgrades belong to their parent station rather than acting as separate repair stations. Compatible modded crafting stations are supported when Valheim's repair rules assign equipment to them.

## Repair behavior

| Aspect | Behavior |
| --- | --- |
| Trigger | Postfix on a non-held `CraftingStation.Interact` for the local player, only when the interacted station is now the player's current station. |
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

The [AutoRepair integration tests](tests/Integration/StationInteractionTests.cs) exercise held, rejected, remote-player and successful station interactions using HarmonyX and game stubs. Run them with `dotnet run --project .\AutoRepair\tests\AutoRepair.Tests.csproj --configuration Release` from the repository root.

The test project separates `Integration/` and `TestDoubles/` and uses the independent [ValheimMods.Testing](../tests/ValheimMods.Testing/) support project for game/Unity stubs and neutral logging. It does not depend on CraftFromStorage or replace the in-game checks.

Each interaction scenario has its own named method, fresh fixture and short XML summary. The shared runner prints scenario names, identifies failing tests and reports test/assertion totals.

Release notes are maintained in [CHANGELOG.md](CHANGELOG.md).