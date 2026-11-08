# Crafting and inventories

Status: Verified - installed signatures, targeted prefab inspection, shared engine source, stub tests and user-reported local cooking success.
Last verified: 2026-11-07.
Evidence: Targeted Mono.Cecil inspection at the [library baseline](Libraries.md); production files linked below; [behavior scenarios](../../CraftFromStorage/tests/Behavior/StorageBehaviorTests.cs) and [patch integration scenarios](../../CraftFromStorage/tests/Integration/PatchIntegrationTests.cs).
Limitations: Shared-engine and category-isolation tests use game doubles, not live crafting or networking. Inspected prefab data establishes component configuration, not in-game execution or persistence. After the food-station split and Fermenter deployment, the user reported on 2026-11-07, "Tested. Cooking fully works." This establishes user-reported local success, not detailed per-recipe, multiplayer or persistence coverage.

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

[CraftingContext](../../ValheimMods.Common/src/Crafting/CraftingContext.cs) is a disposable, nestable mod context, not a game API. It admits the local player only, requires a recipe with a crafting station and a current station, and restores the previous context on disposal.

[RecipeStorage](../../ValheimMods.Common/src/Crafting/RecipeStorage.cs) owns one shared Harmony patch set and registers independent food/non-food clients. CookFromStorage handles food-station keys; CraftFromStorage handles all other station recipes. The recipe's station and current station must agree on category, and that category's client must be registered/enabled. Either plugin works alone; adding the other does not install another count/consumption patch set. Unloading one client cancels its active transaction without removing the other client's hooks. Existing planning, quality, alternatives, multicraft, ownership and late-response policies are shared rather than copied. The cooking automation toggle also gates food recipes.

- [RequirementCheckPatch](../../ValheimMods.Common/src/Crafting/RequirementCheckPatch.cs) enters context for non-discovery requirement checks and restores it in a finalizer.
- [RequirementDisplayPatch](../../ValheimMods.Common/src/Crafting/RequirementDisplayPatch.cs) enters context only for visible crafting UI display work, not general inventory queries.
- [InventoryCountPatch](../../ValheimMods.Common/src/Crafting/InventoryCountPatch.cs) adds storage counts only when the context enables them and the queried inventory is the local player's.
- [AlternativeIngredientPatch](../../ValheimMods.Common/src/Crafting/AlternativeIngredientPatch.cs) handles the context's local inventory and only-one-ingredient recipes. It uses the game's shared item name, quality, world-level constraints, requirement amount, and craft multiplier through the mod helper.

These guards preserve ordinary inventory behavior outside the intended station flow. Do not globally inflate `CountItems` or replace every requirement check.

### Plan and consume separately

[IngredientPlan](../../ValheimMods.Common/src/Crafting/IngredientPlan.cs) allocates concrete stack portions across inventories without double-allocating a stack. Inventory enumeration puts the player's items first, then accessible storage ordered by the storage helper. The plan applies recipe amounts, quality/alternative ingredient handling, and the multiplier with checked arithmetic.

[CraftTransactionPatch](../../ValheimMods.Common/src/Crafting/CraftTransactionPatch.cs) lets no-cost crafting and out-of-scope calls use the original path. Otherwise it starts [StorageCrafting](../../ValheimMods.Common/src/Crafting/StorageCrafting.cs), suppresses the initial original call, and later re-enters `DoCrafting` under an execution guard after ownership acquisition and a fresh plan.

[InventoryConsumePatch](../../ValheimMods.Common/src/Crafting/InventoryConsumePatch.cs) intercepts only name-based consumption against the local player's inventory during execution with a plan. `IngredientPlan.Consume` checks the expected request, validates its portions, and calls the item-object `RemoveItem` overload, throwing if the reserved state changed or removal failed.

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

## Food-station troubleshooting

Targeted metadata inspection on 2026-11-07 against the unchanged [assembly fingerprint](Libraries.md) verified public `CraftingStation.m_name` (`string`) and public `Recipe.m_craftingStation` (`CraftingStation`). Recipe stations can therefore be distinguished by their configured station identity/reference without requiring different component classes. Food-related gameplay does not itself imply the slot-based `CookingStation` API. Exact lookup found no `CraftingStation.m_skill` member on this fingerprint; this is not evidence that no other classification mechanism exists.

On 2026-11-07, local read-only UnityPy inspection used `valheim_Data/StreamingAssets/SoftRef/manifest_extended` to locate the requested prefabs in bundle `c4210710`, SHA-256 `84F55CFB24AF51B9F0494FD9D1A355BF009E08545FF8CCF607C7D88D2E4E1EC8`. It inspected only the selected prefab roots/descendants and component fields. Components with the CraftingStation field layout contain `m_name`, `m_craftRequireFire`, `m_rangeBuild` and other recipe-station configuration. External MonoScript class references could not be resolved in this bounded bundle read; component classification uses the field layout, not a directly resolved script identity:

| Prefab | Verified station key / owner |
| --- | --- |
| `piece_cauldron` | `$piece_cauldron` / CookFromStorage |
| `piece_preptable` | `$piece_preptable` / CookFromStorage |
| `piece_MeadCauldron` | `$piece_meadcauldron` / CookFromStorage |

This is serialized prefab evidence, not live gameplay. The exact-key filter avoids relying on localized English names or classifying every consumable recipe as food. Unknown modded station keys default to the non-food category; modded stations reusing these keys share their category.

The user's earlier 2026-11-07 test profile had only CookFromStorage loaded, explaining unavailable food-recipe storage counts under the old component-based mod split. Food recipes have now moved to CookFromStorage through the shared engine. [Food integration tests](../../CookFromStorage/tests/Integration/RecipeIntegrationTests.cs) cover all three keys' counts, displays and consumption with cooking alone, non-food exclusion, crafting-only food exclusion, coexistence without doubled hooks, category unloading, alternative ingredients and ownership. Live station crafting after the split still needs confirmation. On 2026-11-07 the user reported "Test 1 complete. All passed." with all four plugins deployed (food and non-food recipes from storage), then "All passed." with CraftFromStorage deployed alone: non-food stations used storage while Cauldron, Food Preparation Table and Mead Ketill recipes used inventory only. These are summary local single-player results. A later same-day save/reload test, including a full game restart, also passed with all four plugins (see [combined deployment test](Processing.md#combined-local-deployment-test)); multiplayer remains unverified.