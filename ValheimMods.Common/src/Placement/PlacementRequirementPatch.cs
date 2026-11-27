using System;
using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
internal static class PlacementRequirementPatch
{
    [HarmonyPrefix]
    private static void BeforeRequirementCheck(
        Player __instance,
        Player.RequirementMode mode,
        out PlacementStorageContext.ScopeToken __state)
    {
        __state = mode == Player.RequirementMode.CanBuild
            ? PlacementStorageContext.BeginRequirements(__instance)
            : null;
    }

    [HarmonyFinalizer]
    private static Exception AfterRequirementCheck(Exception __exception, PlacementStorageContext.ScopeToken __state)
    {
        PlacementStorageContext.End(__state);

        return __exception;
    }
}