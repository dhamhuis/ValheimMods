using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
internal static class ContainerDestroyedPatch
{
    [HarmonyPrefix]
    private static void BeforeDestroy(Container __instance) => PlacementStorage.Registry.Remove(__instance);
}