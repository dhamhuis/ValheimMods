using HarmonyLib;
using UnityEngine;

namespace ValheimMods.Common.Crafting;

[HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
internal static class RequirementDisplayPatch
{
    [HarmonyPrefix]
    private static void BeforeDisplay(Transform elementRoot, Player player, bool craft, out CraftingContext __state)
    {
        RequirementAmountText.Clear(elementRoot);
        var gui = InventoryGui.instance;
        __state = !craft || gui == null || !InventoryGui.IsVisible() ? null : CraftingContext.Enter(player, gui.m_selectedRecipe.Recipe);
    }

    [HarmonyPostfix]
    private static void ShowStorageAmount(Transform elementRoot, Piece.Requirement req, bool __result, CraftingContext __state)
    {
        if (!__result || __state == null || req == null || req.m_resItem == null)
        {
            return;
        }

        string name = req.m_resItem.m_itemData.m_shared.m_name;
        int carried;
        __state.IncludeStorageCounts = false;

        try
        {
            carried = __state.LocalPlayer.GetInventory().CountItems(name, -1, true);
        }
        finally
        {
            __state.IncludeStorageCounts = true;
        }

        RequirementAmountText.Apply(elementRoot, carried, __state.CountStorage(name, -1, true), true);
    }

    [HarmonyFinalizer]
    private static void AfterDisplay(CraftingContext __state) => __state?.Dispose();
}