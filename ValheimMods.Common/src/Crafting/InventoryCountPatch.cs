using HarmonyLib;

namespace ValheimMods.Common.Crafting;

[HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems), [typeof(string), typeof(int), typeof(bool)])]
internal static class InventoryCountPatch
{
    [HarmonyPostfix]
    private static void AfterCount(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result)
    {
        var context = CraftingContext.Current;

        if (context?.IncludeStorageCounts == true && __instance == context.LocalPlayer.GetInventory())
        {
            __result = checked(__result + context.CountStorage(name, quality, matchWorldLevel));
        }
    }
}