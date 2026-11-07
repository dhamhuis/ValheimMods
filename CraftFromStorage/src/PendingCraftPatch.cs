using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
internal static class PendingCraftPatch
{
    [HarmonyPrefix]
    private static bool BeforePress()
    {
        if (!StorageCrafting.IsPending)
        {
            return true;
        }

        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null, false);

        return false;
    }
}