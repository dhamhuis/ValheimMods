using HarmonyLib;

namespace AutoRepair;

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
internal static class CraftingStationRepairPatch
{
    [HarmonyPostfix]
    private static void AfterInteract(Humanoid user)
    {
        Player localPlayer = Player.m_localPlayer;
        if (localPlayer == null || user != localPlayer)
        {
            return;
        }

        var inventoryGui = InventoryGui.instance;
        if (inventoryGui == null)
        {
            return;
        }

        var inventory = localPlayer.GetInventory();
        if (inventory == null)
        {
            return;
        }

        int maximumRepairs = inventory.GetAllItems().Count;
        int repairAttempts = 0;
        while (repairAttempts < maximumRepairs && inventoryGui.HaveRepairableItems())
        {
            inventoryGui.RepairOneItem();
            repairAttempts++;
        }

        if (repairAttempts > 0)
        {
            localPlayer.Message(MessageHud.MessageType.Center, " ", 0, null, false);
            AutoRepairPlugin.Log.LogInfo($"Completed {repairAttempts} repair attempts.");
        }

        if (repairAttempts == maximumRepairs && inventoryGui.HaveRepairableItems())
        {
            AutoRepairPlugin.Log.LogWarning("Stopped auto repair at the inventory item limit; repairable items remain.");
        }
    }
}