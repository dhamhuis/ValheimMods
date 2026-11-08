using System;
using HarmonyLib;

namespace ValheimMods.Common.Crafting;

[HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
internal static class CraftTransactionPatch
{
    [HarmonyPrefix]
    private static bool BeforeCraft(InventoryGui __instance, Player player)
    {
        if (!CraftingContext.IsStationRecipe(player, __instance.m_craftRecipe) || StorageCrafting.IsExecuting)
        {
            return true;
        }

        if (StorageCrafting.IsPending)
        {
            return false;
        }

        if (player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost))
        {
            return true;
        }

        RecipeStorage.StartCraft(__instance, player);

        return false;
    }

    [HarmonyFinalizer]
    private static void AfterCraft(Exception __exception)
    {
        if (__exception != null)
        {
            RecipeStorage.LogError($"Crafting failed: {__exception}");
        }
    }
}