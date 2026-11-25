using HarmonyLib;

namespace BuildFromStorage;

[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece), typeof(Piece))]
internal static class BuildCostPlanPatch
{
    [HarmonyPrefix]
    private static bool BeforePlacePiece(Player __instance, Piece piece, ref bool __result)
    {
        var context = BuildStorageContext.Current;

        if (context == null || context.Player != __instance || piece == null
            || __instance.PlacementCostDisabled
            || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())))
        {
            return true;
        }

        var plan = BuildResourcePlan.Create(context, piece.m_resources, 0, -1, 1);

        if (plan == null)
        {
            BuildFromStoragePlugin.LogWarning("Build resources changed before placement; the piece was not placed.");
            __result = false;

            return false;
        }

        if (!plan.UsesStorage)
        {
            return true;
        }

        if (!context.TryReserve(plan))
        {
            BuildFromStoragePlugin.LogWarning("Nearby storage became unavailable before placement; the piece was not placed.");
            __result = false;

            return false;
        }

        return true;
    }
}