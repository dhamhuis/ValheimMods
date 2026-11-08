using HarmonyLib;

namespace CookFromStorage;

[HarmonyPatch(typeof(Container), "OnDestroyed")]
internal static class ContainerDestroyedPatch
{
    [HarmonyPrefix]
    private static void BeforeDestroy(Container __instance) => NearbyStorage.Remove(__instance);
}