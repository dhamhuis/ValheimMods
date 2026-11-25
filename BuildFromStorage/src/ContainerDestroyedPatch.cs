using HarmonyLib;

namespace BuildFromStorage;

[HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
internal static class ContainerDestroyedPatch
{
    [HarmonyPrefix]
    private static void BeforeDestroy(Container __instance) => BuildFromStoragePlugin.StorageRegistry.Remove(__instance);
}