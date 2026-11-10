using HarmonyLib;
using UnityEngine;

namespace ValheimMods.Common.InputSelection;

[HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
internal static class PreviewCursorPatch
{
    [HarmonyPrefix]
    private static bool BeforeCursorUpdated()
    {
        if (!InputPickerPreview.IsOpen)
        {
            return true;
        }

        ZCursor.LockState = CursorLockMode.None;
        ZCursor.Show();

        return false;
    }
}