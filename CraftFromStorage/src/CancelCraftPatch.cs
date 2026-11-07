using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(InventoryGui), "OnCraftCancelPressed")]
internal static class CancelCraftPatch
{
    [HarmonyPrefix]
    private static void BeforeCancel() => StorageCrafting.Cancel();
}