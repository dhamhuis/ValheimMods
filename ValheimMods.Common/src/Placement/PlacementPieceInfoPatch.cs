using System;
using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
internal static class PlacementPieceInfoPatch
{
    internal static PlacementStorageContext.Scope DisplayScope { get; private set; }

    [HarmonyPrefix]
    private static void BeforePieceInfo(Hud __instance, out PlacementPieceInfoState __state)
    {
        RequirementAmountText.ClearAll(__instance.m_requirementItems);
        var token = PlacementStorageContext.BeginRequirements(Player.m_localPlayer);
        __state = new PlacementPieceInfoState(token, DisplayScope);
        DisplayScope = token?.Scope;
    }

    [HarmonyFinalizer]
    private static Exception AfterPieceInfo(Exception __exception, PlacementPieceInfoState __state)
    {
        if (__state != null)
        {
            DisplayScope = __state.PreviousDisplayScope;
            PlacementStorageContext.End(__state.Token);
        }

        return __exception;
    }
}