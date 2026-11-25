using HarmonyLib;

namespace BuildFromStorage;

[HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems), typeof(string), typeof(int), typeof(bool))]
internal static class BuildCostCountPatch
{
    [HarmonyPostfix]
    private static void IncludeStorage(
        Inventory __instance,
        string name,
        int quality,
        bool matchWorldLevel,
        ref int __result)
    {
        var context = BuildStorageContext.Current;

        if (context == null || context.RequirementCheckDepth == 0 || context.SuppressCountAugmentation
            || __instance != context.Player.GetInventory())
        {
            return;
        }

        foreach (var container in context.Containers)
        {
            if (!context.IsAccessible(container))
            {
                continue;
            }

            var inventory = container.GetInventory();

            if (inventory != null)
            {
                __result = checked(__result + inventory.CountItems(name, quality, matchWorldLevel));
            }
        }
    }
}