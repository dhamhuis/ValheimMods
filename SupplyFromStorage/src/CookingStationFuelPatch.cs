using HarmonyLib;

namespace SupplyFromStorage;

[HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch")]
internal static class CookingStationFuelPatch
{
    [HarmonyPrefix]
    private static bool BeforeFuelRequested(
        CookingStation __instance,
        Humanoid user,
        ItemDrop.ItemData item,
        ref bool __result)
    {
        if (!SupplyTransfer.TryHandleCookingStationFuel(__instance, user, item))
        {
            return true;
        }

        __result = true;

        return false;
    }
}