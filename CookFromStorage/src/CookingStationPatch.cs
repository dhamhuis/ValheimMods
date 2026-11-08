using HarmonyLib;

namespace CookFromStorage;

[HarmonyPatch(typeof(CookingStation), "OnInteract")]
internal static class CookingStationPatch
{
    [HarmonyPrefix]
    private static bool BeforeFoodRequested(
        CookingStation __instance,
        Humanoid user,
        ref bool __result)
    {
        if (!CookingTransfer.TryHandleFoodRequest(__instance, user, out bool handledResult))
        {
            return true;
        }

        __result = handledResult;
        return false;
    }
}