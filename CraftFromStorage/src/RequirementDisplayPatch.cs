using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
internal static class RequirementDisplayPatch
{
    [HarmonyPrefix]
    private static void BeforeDisplay(Player player, bool craft, out CraftingContext __state)
    {
        var gui = InventoryGui.instance;
        __state = !craft || gui == null || !InventoryGui.IsVisible() ? null : CraftingContext.Enter(player, gui.m_selectedRecipe.Recipe);
    }

    [HarmonyFinalizer]
    private static void AfterDisplay(CraftingContext __state) => __state?.Dispose();
}