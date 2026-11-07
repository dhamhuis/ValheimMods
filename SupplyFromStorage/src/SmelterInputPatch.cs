using HarmonyLib;

namespace SupplyFromStorage;

[HarmonyPatch(typeof(Smelter), "OnAddOre")]
internal static class SmelterInputPatch
{
    [HarmonyPrefix]
    private static bool BeforeInputRequested(
        Smelter __instance,
        Humanoid user,
        ItemDrop.ItemData item,
        ref bool __result)
    {
        if (!SupplyTransfer.TryHandleSmelterInput(__instance, user, item, out bool handledResult))
        {
            return true;
        }

        __result = handledResult;
        return false;
    }
}
