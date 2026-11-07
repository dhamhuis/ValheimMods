using HarmonyLib;

namespace SupplyFromStorage;

[HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch")]
internal static class CookingStationFuelPatch
{
    [HarmonyPostfix]
    private static void AfterFuelRequested(
        CookingStation __instance,
        Humanoid user,
        ItemDrop.ItemData item,
        bool __result)
    {
        SupplyTransfer.CookingStationFuelRequested(__instance, user, item, __result);
    }
}
