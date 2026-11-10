using HarmonyLib;

namespace ValheimMods.Common.InputSelection;

[HarmonyPatch(typeof(Player), "TakeInput")]
internal static class PreviewPlayerInputPatch
{
    [HarmonyPostfix]
    private static void AfterInputChecked(Player __instance, ref bool __result)
    {
        InputPickerPreview.FilterPlayerInput(__instance, ref __result);
    }
}