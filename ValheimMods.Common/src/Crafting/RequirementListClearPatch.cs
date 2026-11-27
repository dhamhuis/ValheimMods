using HarmonyLib;

namespace ValheimMods.Common.Crafting;

// Recipes with fewer requirements leave surplus slots untouched, so stale counts are cleared before every redraw.
[HarmonyPatch(typeof(InventoryGui), "SetupRequirementList")]
internal static class RequirementListClearPatch
{
    [HarmonyPrefix]
    private static void ClearCounts(InventoryGui __instance) => RequirementAmountText.ClearAll(__instance.m_recipeRequirementList);
}