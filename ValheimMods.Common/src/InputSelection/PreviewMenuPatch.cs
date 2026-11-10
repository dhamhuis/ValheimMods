using HarmonyLib;

namespace ValheimMods.Common.InputSelection;

[HarmonyPatch(typeof(Menu), "Update")]
internal static class PreviewMenuPatch
{
    [HarmonyPrefix]
    private static bool BeforeMenuUpdated()
    {
        return !InputPickerPreview.BlocksInput;
    }
}