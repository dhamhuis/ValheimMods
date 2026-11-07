using System;
using HarmonyLib;
using ValheimMods.Testing;

namespace AutoRepair.Tests;

internal static class StationInteractionTests
{
    internal static void Run(TestRunner runner)
    {
        var harmony = new Harmony("Hoose.valheim.autorepair.tests");

        try
        {
            harmony.PatchAll(typeof(StationInteractionTests).Assembly);
            runner.Run(HeldInteractionDoesNotRepair);
            runner.Run(RejectedStationDoesNotRepair);
            runner.Run(RemotePlayerDoesNotRepair);
            runner.Run(SuccessfulInteractionRepairsDespiteFalseResult);
        }
        finally
        {
            harmony.UnpatchSelf();
        }
    }

    /// <summary>Holding the interaction key must not trigger automatic repairs.</summary>
    private static void HeldInteractionDoesNotRepair(Action<bool, string> check)
    {
        var (player, gui, station) = Setup();
        station.Interact(player, true, false);
        check(gui.Repairs == 0, "Held interaction repaired an item.");
    }

    /// <summary>A rejected station interaction must not repair using the previous station.</summary>
    private static void RejectedStationDoesNotRepair(Action<bool, string> check)
    {
        var (player, gui, station) = Setup();
        station.Usable = false;
        station.Interact(player, false, false);
        check(gui.Repairs == 0, "Rejected interaction repaired an item.");
    }

    /// <summary>Another player's interaction must not repair the local player's equipment.</summary>
    private static void RemotePlayerDoesNotRepair(Action<bool, string> check)
    {
        var (_, gui, station) = Setup();
        station.Interact(new Player(), false, false);
        check(gui.Repairs == 0, "Remote interaction repaired a local item.");
    }

    /// <summary>Successful station use repairs equipment even though vanilla returns false.</summary>
    private static void SuccessfulInteractionRepairsDespiteFalseResult(Action<bool, string> check)
    {
        var (player, gui, station) = Setup();
        bool result = station.Interact(player, false, false);
        check(!result && gui.Repairs == 1, "Successful interaction did not repair exactly one item.");
    }

    private static (Player Player, InventoryGui Gui, CraftingStation Station) Setup()
    {
        var player = Player.m_localPlayer = new Player();
        var gui = InventoryGui.instance = new InventoryGui();
        player.Inventory.Items.Add(new ItemDrop.ItemData());
        gui.RepairsRemaining = 1;

        return (player, gui, new CraftingStation());
    }
}