using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Container), nameof(Container.Awake))]
internal static class ContainerCreatedPatch
{
    [HarmonyPostfix]
    private static void AfterAwake(Container __instance) => PlacementStorage.Registry.Add(__instance);
}