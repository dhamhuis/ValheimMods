using HarmonyLib;

namespace ValheimMods.Common.InputSelection;

[HarmonyPatch(typeof(PlayerController), "TakeInput")]
internal static class PreviewControllerInputPatch
{
    [HarmonyPostfix]
    private static void AfterInputChecked(PlayerController __instance, ref bool __result)
    {
        if (InputPickerPreview.BlocksInput && __instance.GetComponent<Player>() == Player.m_localPlayer)
        {
            __result = false;
        }
    }
}