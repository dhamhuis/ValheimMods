using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources), typeof(Piece.Requirement[]), typeof(int), typeof(int), typeof(int))]
internal static class PlacementCostConsumePatch
{
    [HarmonyPostfix]
    private static void AfterConsumeResources(Player __instance)
    {
        var context = PlacementStorageContext.Current;

        if (context != null && context.Player == __instance)
        {
            context.ConsumeStorage();
        }
    }
}