using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), [typeof(string), typeof(int), typeof(int), typeof(bool)])]
internal static class InventoryConsumePatch
{
    [HarmonyPrefix]
    private static bool BeforeRemove(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased)
    {
        var context = CraftingContext.Current;

        if (!StorageCrafting.IsExecuting || context?.Plan == null || __instance != context.LocalPlayer.GetInventory())
        {
            return true;
        }

        context.Plan.Consume(name, amount, itemQuality, worldLevelBased);

        return false;
    }
}