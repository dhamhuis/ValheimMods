using HarmonyLib;

namespace ValheimMods.Common.Crafting;

[HarmonyPatch(typeof(Container), "Awake")]
internal static class ContainerCreatedPatch
{
    [HarmonyPostfix]
    private static void AfterAwake(Container __instance) => NearbyStorage.Add(__instance);
}