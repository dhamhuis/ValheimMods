using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
internal static class RequirementCheckPatch
{
    [HarmonyPrefix]
    private static void BeforeCheck(Player __instance, Recipe __0, bool discover, out CraftingContext __state)
    {
        __state = discover ? null : CraftingContext.Enter(__instance, __0);
    }

    [HarmonyFinalizer]
    private static void AfterCheck(CraftingContext __state) => __state?.Dispose();
}