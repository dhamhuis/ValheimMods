using HarmonyLib;
using UnityEngine;

namespace ValheimMods.Common.Crafting;

// With no recipe or upgradeable item selected, UpdateRecipe hides every requirement slot without SetupRequirementList,
// so hidden slots clear their counts here.
[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.HideRequirement))]
internal static class RequirementHideClearPatch
{
    [HarmonyPrefix]
    private static void ClearCounts(Transform elementRoot) => RequirementAmountText.Clear(elementRoot);
}