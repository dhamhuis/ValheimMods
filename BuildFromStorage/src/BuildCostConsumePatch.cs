using HarmonyLib;

namespace BuildFromStorage;

[HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources), typeof(Piece.Requirement[]), typeof(int), typeof(int), typeof(int))]
internal static class BuildCostConsumePatch
{
    [HarmonyPostfix]
    private static void AfterConsumeResources(Player __instance)
    {
        var context = BuildStorageContext.Current;

        if (context != null && context.Player == __instance)
        {
            context.ConsumeStorage();
        }
    }
}