# Crafting and inventories

Status: Verified - installed signatures and repository implementation only.
Last verified: 2026-11-06.
Evidence: Targeted Mono.Cecil inspection at the [library baseline](Libraries.md); production files linked below; [behavior scenarios](../../CraftFromStorage/tests/Behavior/StorageBehaviorTests.cs) and [patch integration scenarios](../../CraftFromStorage/tests/Integration/PatchIntegrationTests.cs).
Limitations: Tests were not run for this documentation seed. No game-body or live recipe/persistence behavior verified; do not infer game behavior from test doubles.

## Use when

Changing recipe requirement displays/checks, alternative ingredients, storage-assisted counts, or actual ingredient consumption. Read [Storage](Storage.md) before mutating container inventories.

## API and behavior

### Installed signatures

Signatures below use C# notation and original parameter names. Optional/default-value attributes are available through the inspector; no default values are assumed here.

| Member | Original visibility / return | Parameters |
| --- | --- | --- |
| `Player.HaveRequirementItems` | Private instance / `bool` | `Recipe piece, bool discover, int qualityLevel, int amount` |
| `Player.GetFirstRequiredItem` | Public instance / `ItemDrop.ItemData` | `Inventory inventory, Recipe recipe, int qualityLevel, out int amount, out int extraAmount, int craftMultiplier` |
| `InventoryGui.SetupRequirement` | Public static / `bool` | `UnityEngine.Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality, int craftMultiplier` |
| `InventoryGui.DoCrafting` | Private instance / `void` | `Player player` |
| `InventoryGui.OnCraftPressed`, `InventoryGui.OnCraftCancelPressed` | Private instance / `void` | None |
| `Inventory.CountItems` | Public instance / `int` | `string name, int quality, bool matchWorldLevel` |
| `Inventory.RemoveItem` | Public instance / `void` | `string name, int amount, int itemQuality, bool worldLevelBased` |
| `Inventory.RemoveItem` | Public instance / `bool` | `ItemDrop.ItemData item, int amount` |

Other `RemoveItem` overloads exist (`int` and `ItemDrop.ItemData`, both returning `bool`). The name-based overload returns **void**, not success/failure. Use exact overloads; an item-object removal is not the same patch target as name-based recipe consumption.

`HaveRequirementItems` calls its recipe parameter `piece`, not `recipe`; the existing patch uses positional `__0` deliberately. `SetupRequirement` is static, so an `__instance` binding would be invalid.

### Scope preview/count changes

[CraftingContext](../../CraftFromStorage/src/CraftingContext.cs) is a disposable, nestable mod context, not a game API. It admits the local player only, requires a recipe with a crafting station and a current station, and restores the previous context on disposal.

- [RequirementCheckPatch](../../CraftFromStorage/src/RequirementCheckPatch.cs) enters context for non-discovery requirement checks and restores it in a finalizer.
- [RequirementDisplayPatch](../../CraftFromStorage/src/RequirementDisplayPatch.cs) enters context only for visible crafting UI display work, not general inventory queries.
- [InventoryCountPatch](../../CraftFromStorage/src/InventoryCountPatch.cs) adds storage counts only when the context enables them and the queried inventory is the local player's.
- [AlternativeIngredientPatch](../../CraftFromStorage/src/AlternativeIngredientPatch.cs) handles the context's local inventory and only-one-ingredient recipes. It uses the game's shared item name, quality, world-level constraints, requirement amount, and craft multiplier through the mod helper.

These guards preserve ordinary inventory behavior outside the intended station flow. Do not globally inflate `CountItems` or replace every requirement check.

### Plan and consume separately

[IngredientPlan](../../CraftFromStorage/src/IngredientPlan.cs) allocates concrete stack portions across inventories without double-allocating a stack. Inventory enumeration puts the player's items first, then accessible storage ordered by the storage helper. The plan applies recipe amounts, quality/alternative ingredient handling, and the multiplier with checked arithmetic.

[CraftTransactionPatch](../../CraftFromStorage/src/CraftTransactionPatch.cs) lets no-cost crafting and out-of-scope calls use the original path. Otherwise it starts [StorageCrafting](../../CraftFromStorage/src/StorageCrafting.cs), suppresses the initial original call, and later re-enters `DoCrafting` under an execution guard after ownership acquisition and a fresh plan.

[InventoryConsumePatch](../../CraftFromStorage/src/InventoryConsumePatch.cs) intercepts only name-based consumption against the local player's inventory during execution with a plan. `IngredientPlan.Consume` checks the expected request, validates its portions, and calls the item-object `RemoveItem` overload, throwing if the reserved state changed or removal failed.

These checks do **not** establish a general rollback/atomicity guarantee across every ingredient, exception, network event, or another mod's intervention. Investigate those cases before relying on them.

## Pitfalls

- Display counts, requirement availability, and consumption are separate surfaces. Updating one alone can show craftable items that cannot be consumed, or change unrelated inventory logic.
- Item names here are shared internal names (`m_shared.m_name`), not necessarily localized display strings.
- Quality and world-level filtering, alternative ingredients, upgrades, and multicraft amounts matter. Do not reduce a recipe to item-name totals.
- Context cleanup must survive exceptions. The existing finalizers are important, not redundant postfixes.
- Recipe/station/UI state can change while ownership is pending; a preview plan is not authorization to consume later.

## Unknowns

- Method-body behavior for new recipe types, stations, or game updates; matching signatures do not establish compatible internals.
- Live crafting and upgrade results, item saving, cancellation races, and coexistence with other crafting plugins.
- Comprehensive failure recovery after partial multi-ingredient mutation. Do not claim transaction rollback based on the mod helper's name.