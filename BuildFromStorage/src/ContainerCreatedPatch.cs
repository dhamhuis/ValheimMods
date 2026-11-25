using HarmonyLib;

namespace BuildFromStorage;

[HarmonyPatch(typeof(Container), nameof(Container.Awake))]
internal static class ContainerCreatedPatch
{
    [HarmonyPostfix]
    private static void AfterAwake(Container __instance) => BuildFromStoragePlugin.StorageRegistry.Add(__instance);
}