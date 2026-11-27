using System;
using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement), typeof(bool), typeof(float))]
internal static class PlacementContextPatch
{
    [HarmonyPrefix]
    private static void BeforePlacement(Player __instance, out PlacementStorageContext.ScopeToken __state)
    {
        __state = PlacementStorageContext.BeginPlacement(__instance);
    }

    [HarmonyFinalizer]
    private static Exception AfterPlacement(Exception __exception, PlacementStorageContext.ScopeToken __state)
    {
        PlacementStorageContext.End(__state);

        return __exception;
    }
}