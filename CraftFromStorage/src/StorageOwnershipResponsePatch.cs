using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(Container), "RPC_OpenResponse")]
internal static class StorageOwnershipResponsePatch
{
    [HarmonyPrefix]
    private static bool BeforeResponse(Container __instance, bool granted)
    {
        return !StorageCrafting.ReceiveResponse(__instance, granted);
    }
}