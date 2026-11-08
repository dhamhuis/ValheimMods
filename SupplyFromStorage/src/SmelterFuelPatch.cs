using HarmonyLib;

namespace SupplyFromStorage;

[HarmonyPatch(typeof(Smelter), "OnAddFuel")]
internal static class SmelterFuelPatch
{
    [HarmonyPrefix]
    private static bool BeforeFuelRequested(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (!SupplyTransfer.TryHandleSmelterFuel(__instance, user, item))
        {
            return true;
        }

        __result = true;

        return false;
    }
}