using HarmonyLib;

namespace ValheimMods.Common.Crafting;

[HarmonyPatch(typeof(InventoryGui), "OnCraftCancelPressed")]
internal static class CancelCraftPatch
{
    [HarmonyPrefix]
    private static void BeforeCancel() => StorageCrafting.Cancel();
}