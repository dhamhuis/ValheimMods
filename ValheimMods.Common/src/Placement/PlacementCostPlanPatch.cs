using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece), typeof(Piece))]
internal static class PlacementCostPlanPatch
{
    [HarmonyPrefix]
    private static bool BeforePlacePiece(Player __instance, Piece piece, ref bool __result)
    {
        var context = PlacementStorageContext.Current;

        if (context == null || context.Player != __instance || piece == null
            || __instance.PlacementCostDisabled
            || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())))
        {
            return true;
        }

        var plan = PlacementResourcePlan.Create(context, piece.m_resources, 0, -1, 1);

        if (plan == null)
        {
            context.Client.LogWarning("Placement resources changed before placement; the piece was not placed.");
            __result = false;

            return false;
        }

        if (!plan.UsesStorage)
        {
            return true;
        }

        if (!context.TryReserve(plan))
        {
            context.Client.LogWarning("Nearby storage became unavailable before placement; the piece was not placed.");
            __result = false;

            return false;
        }

        return true;
    }
}