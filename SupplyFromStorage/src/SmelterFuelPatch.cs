using HarmonyLib;

namespace SupplyFromStorage;

[HarmonyPatch(typeof(Smelter), "OnAddFuel")]
internal static class SmelterFuelPatch
{
    [HarmonyPostfix]
    private static void AfterFuelRequested(Smelter __instance, Humanoid user, ItemDrop.ItemData item, bool __result)
    {
        SupplyTransfer.SmelterFuelRequested(__instance, user, item, __result);
    }
}
