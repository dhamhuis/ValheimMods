using HarmonyLib;
using UnityEngine;

namespace SupplyFromStorage;

[HarmonyPatch(typeof(Fireplace), "Interact")]
internal static class FireplaceInteractPatch
{
    [HarmonyPrefix]
    private static void BeforeInteract(Fireplace __instance, bool hold, out bool __state)
    {
        __state = !hold || Time.time - __instance.m_lastUseTime >= __instance.m_holdRepeatInterval;
    }

    [HarmonyPostfix]
    private static void AfterInteract(Fireplace __instance, Humanoid user, bool alt, bool __result, bool __state)
    {
        SupplyTransfer.FireplaceInteracted(__instance, user, __state && !alt, __result);
    }
}
