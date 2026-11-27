using HarmonyLib;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(Hud), "UpdateBuild")]
internal static class PlacementMenuPatch
{
    private static Hud _lastHud;
    private static PlacementStorage _lastClient;

    [HarmonyPrefix]
    private static void BeforeUpdateBuild(Hud __instance, Player player, ref bool forceUpdateAllBuildStatuses)
    {
        if (__instance.m_buildUi == null
            || !__instance.m_buildUi.gameObject.activeSelf
            || !PlacementStorage.TryGetClient(player, out var client)
            || !client.IsCultivator)
        {
            _lastHud = null;
            _lastClient = null;

            return;
        }

        if (_lastHud != __instance || _lastClient != client)
        {
            forceUpdateAllBuildStatuses = true;
            _lastHud = __instance;
            _lastClient = client;
        }
    }
}