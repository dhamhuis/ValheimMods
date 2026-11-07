using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
internal static class AlternativeIngredientPatch
{
    [HarmonyPrefix]
    private static bool BeforeSelection(
        Player __instance,
        Inventory inventory,
        Recipe recipe,
        int qualityLevel,
        ref int amount,
        ref int extraAmount,
        int craftMultiplier,
        ref ItemDrop.ItemData __result)
    {
        using var context = CraftingContext.Enter(__instance, recipe);

        if (context == null || inventory != __instance.GetInventory() || !recipe.m_requireOnlyOneIngredient)
        {
            return true;
        }

        __result = context.FirstIngredient(recipe, qualityLevel, craftMultiplier, out amount, out extraAmount);

        return false;
    }
}