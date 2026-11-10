using HarmonyLib;

namespace SupplyFromStorage;

[HarmonyPatch(typeof(Fireplace), "Interact")]
internal static class FireplaceInteractPatch
{
    [HarmonyPrefix]
    private static bool BeforeInteract(Fireplace __instance, Humanoid user, bool hold, bool alt, ref bool __result)
    {
        if (!SupplyTransfer.TryHandleFireplaceFuel(__instance, user, hold, alt, out bool handledResult))
        {
            return true;
        }

        __result = handledResult;

        return false;
    }
}