using System;
using HarmonyLib;
using UnityEngine;
using ValheimMods.Testing;
using static SupplyFromStorage.Tests.SupplyTestSetup;

namespace SupplyFromStorage.Tests;

internal static class PatchIntegrationTests
{
    internal static void Run(TestRunner runner)
    {
        var harmony = new Harmony("hoose.valheim.supplyfromstorage.tests");

        try
        {
            harmony.PatchAll(typeof(PatchIntegrationTests).Assembly);
            runner.Run(FireplaceInteractionsSupplyOneItemAndRespectVanillaActions);
            runner.Run(SwitchCallbacksSupplyOneItemPerUse);
            runner.Run(SmelterMessagesReflectAvailableInputs);
            runner.Run(CookingStationFuelSwitchSuppliesOnlyFuel);
            runner.Run(FrostKilnFuelMessagesReflectStorage);
            runner.Run(FrostFoundryFuelMessagesReflectStorage);
            runner.Run(AutomationTogglesAreIndependent);
            runner.Run(StationUpdatesDoNotSupplyItems);
            runner.Run(CraftFromStorageInventoryHookDoesNotInterceptTransfers);
            runner.Run(ContainerLifecyclePatchesTrackStorage);
        }
        finally
        {
            Reset();
            harmony.UnpatchSelf();
        }
    }

    /// <summary>Fireplace E actions supply at most one item and preserve vanilla inventory/toggle behavior.</summary>
    private static void FireplaceInteractionsSupplyOneItemAndRespectVanillaActions(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", 4));
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 4,
            m_holdRepeatInterval = 0.5f
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);

        fireplace.Interact(player, false, false);
        check(fireplace.Fuel == 1 && chest.Inventory.Items[0].m_stack == 3,
            "One fireplace interaction did not transfer exactly one item.");

        Time.time += 0.5f;
        fireplace.Interact(player, true, false);
        check(fireplace.Fuel == 2 && chest.Inventory.Items[0].m_stack == 2,
            "A repeated held interaction did not transfer exactly one item.");

        fireplace.Interact(player, true, false);
        check(fireplace.Fuel == 2 && chest.Inventory.Items[0].m_stack == 2,
            "The fireplace repeat guard was bypassed.");

        Reset();
        chest = Chest(1, 0, 0, Item("$item_wood", 2));
        player.Inventory.Items.Add(Item("$item_wood", 1));
        fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 3
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);
        fireplace.Interact(player, false, false);
        check(fireplace.Fuel == 1 && player.Inventory.Items.Count == 0
            && chest.Inventory.Items[0].m_stack == 2,
            "A vanilla inventory transfer also consumed storage fuel.");

        Reset();
        chest = Chest(1, 0, 0, Item("$item_wood", 1));
        fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2,
            m_canTurnOff = true,
            Fuel = 1
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);
        check(fireplace.Interact(Player.m_localPlayer, false, false),
            "The vanilla fire toggle was not preserved.");
        check(fireplace.Fuel == 1 && chest.Inventory.Items[0].m_stack == 1,
            "A fire-toggle action supplied storage fuel.");

        Reset();
        chest = Chest(1, 0, 0, Item("$item_wood", 1));
        fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);
        fireplace.Interact(Player.m_localPlayer, false, true);
        check(fireplace.Fuel == 0 && chest.Inventory.Items[0].m_stack == 1,
            "An alternate fireplace action supplied storage fuel.");
    }

    /// <summary>Smelter switch callbacks transfer one fuel or accepted input per E action.</summary>
    private static void SwitchCallbacksSupplyOneItemPerUse(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_coal", 3), Item("$item_ore", 3));
        var smelter = new Smelter
        {
            m_fuelItem = StationDrop("Coal", "$item_coal"),
            m_maxFuel = 2,
            m_maxOre = 2
        };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);

        var sw = new Switch();
        smelter.OnAddFuel(sw, player, null);
        smelter.OnAddOre(sw, player, null);
        check(smelter.Fuel == 1 && smelter.QueueSize == 1,
            "A single smelter switch action did not add exactly one item.");

        smelter.OnAddFuel(sw, player, null);
        smelter.OnAddOre(sw, player, null);
        check(smelter.Fuel == 2 && smelter.QueueSize == 2,
            "Repeated smelter switch actions did not add one item each.");

        smelter.OnAddFuel(sw, player, null);
        smelter.OnAddOre(sw, player, null);
        check(smelter.Fuel == 2 && smelter.QueueSize == 2
            && chest.Inventory.Items[0].m_stack == 1 && chest.Inventory.Items[1].m_stack == 1,
            "A full smelter accepted more items than its configured capacity.");

        Reset();
        chest = Chest(1, 0, 0, Item("$item_coal", 2));
        player.Inventory.Items.Add(Item("$item_coal", 1));
        smelter = new Smelter
        {
            m_fuelItem = StationDrop("Coal", "$item_coal"),
            m_maxFuel = 2
        };
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        smelter.OnAddFuel(new Switch(), player, null);
        check(smelter.Fuel == 1 && player.Inventory.Items.Count == 0
            && chest.Inventory.Items[0].m_stack == 2,
            "Vanilla smelter fuel insertion also consumed storage fuel.");
    }

    /// <summary>The no-processable-items message is shown only when neither inventory nor accessible storage has input.</summary>
    private static void SmelterMessagesReflectAvailableInputs(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_ore", 2));
        var smelter = new Smelter { m_maxOre = 2 };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);

        bool result = smelter.OnAddOre(new Switch(), player, null);
        check(result && player.LastMessage == null && smelter.QueueSize == 1
            && chest.Inventory.Items[0].m_stack == 1,
            "A successful storage input showed the no-processable-items message.");

        player = Reset();
        smelter = new Smelter { m_maxOre = 2 };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        result = smelter.OnAddOre(new Switch(), player, null);
        check(!result && player.LastMessage == "$msg_noprocessableitems",
            "The no-processable-items message was hidden when no input was available.");

        player = Reset();
        chest = Chest(1, 0, 0, Item("$item_ore", 1));
        player.Inventory.Items.Add(Item("$item_ore", 1));
        smelter = new Smelter { m_maxOre = 2 };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        result = smelter.OnAddOre(new Switch(), player, null);
        check(result && player.LastMessage == "$msg_added $item_ore" && smelter.QueueSize == 1
            && player.Inventory.Items.Count == 0 && chest.Inventory.Items[0].m_stack == 1,
            "The storage prefix intercepted a valid vanilla inventory input.");

        player = Reset();
        chest = Chest(1, 0, 0, Item("$item_ore", 1));
        smelter = new Smelter { m_maxOre = 1, QueueSize = 1 };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        result = smelter.OnAddOre(new Switch(), player, null);
        check(!result && player.LastMessage == "$msg_itsfull"
            && chest.Inventory.Items[0].m_stack == 1,
            "A full station showed the no-processable-items message or consumed storage.");
    }

    /// <summary>Cooking stations supply fuel only and avoid duplicate transfer after a vanilla inventory insert.</summary>
    private static void CookingStationFuelSwitchSuppliesOnlyFuel(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", 2), Item("$item_raw_meat", 1));
        var station = new CookingStation
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2
        };
        station.transform.position = new UnityEngine.Vector3(1, 0, 0);
        var sw = new Switch();

        station.OnAddFuelSwitch(sw, player, null);
        station.OnAddFuelSwitch(sw, player, null);
        check(station.Fuel == 2 && chest.Inventory.CountItems("$item_wood", -1, false) == 0,
            "Cooking-station E actions did not add exactly one fuel each.");
        check(chest.Inventory.Items.Exists(item => item.m_shared.m_name == "$item_raw_meat" && item.m_stack == 1),
            "A cooking ingredient was supplied as fuel.");

        Reset();
        chest = Chest(1, 0, 0, Item("$item_wood", 1));
        player.Inventory.Items.Add(Item("$item_wood", 1));
        station = new CookingStation
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2
        };
        station.transform.position = new UnityEngine.Vector3(1, 0, 0);
        station.OnAddFuelSwitch(new Switch(), player, null);
        check(station.Fuel == 1 && chest.Inventory.Items[0].m_stack == 1,
            "Vanilla cooking-station fuel insertion also consumed storage fuel.");
    }

    /// <summary>Ice-as-fuel uses storage without a premature missing-ice message.</summary>
    private static void FrostKilnFuelMessagesReflectStorage(Action<bool, string> check)
    {
        CheckFuelMessages(check, false, "$item_ice");
    }

    /// <summary>Foundry fuel uses storage without a premature missing-liquid-frost message.</summary>
    private static void FrostFoundryFuelMessagesReflectStorage(Action<bool, string> check)
    {
        CheckFuelMessages(check, true, "$item_frozenfuel");
    }

    private static void CheckFuelMessages(Action<bool, string> check, bool cookingStation, string fuelName)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item(fuelName, 3));
        var fuelDrop = StationDrop("TestFuel", fuelName);
        var oven = new CookingStation { m_fuelItem = fuelDrop, m_maxFuel = 3 };
        var smelter = new Smelter { m_fuelItem = fuelDrop, m_maxFuel = 3 };
        oven.transform.position = smelter.transform.position = new Vector3(1, 0, 0);
        Func<ItemDrop.ItemData, bool> interact = item => cookingStation
            ? oven.OnAddFuelSwitch(new Switch(), player, item)
            : smelter.OnAddFuel(new Switch(), player, item);
        Func<float> fuel = () => cookingStation ? oven.Fuel : smelter.Fuel;

        bool result = interact(null);
        check(result && fuel() == 1 && chest.Inventory.Items[0].m_stack == 2,
            "Northern fuel transfer did not consume exactly one storage fuel item.");
        check(player.Messages.Count == 1 && player.Messages[0] == "$msg_added " + fuelName,
            "A no-fuel message appeared before the successful storage-fuel message.");

        player.Messages.Clear();
        player.Inventory.Items.Add(Item(fuelName, 1));
        result = interact(null);
        check(result && fuel() == 2 && player.Inventory.Items.Count == 0 && chest.Inventory.Items[0].m_stack == 2
            && player.Messages.Count == 1 && player.LastMessage == "$msg_added " + fuelName,
            "Inventory-first fuel handling was changed or storage was also consumed.");

        player.Messages.Clear();
        var wrongItem = Item("$item_wrong", 1);
        player.Inventory.Items.Add(wrongItem);
        result = interact(wrongItem);
        check(!result && fuel() == 2 && chest.Inventory.Items[0].m_stack == 2
            && player.LastMessage == "$msg_wrongitem",
            "Explicit wrong-item handling was replaced by storage fueling.");

        player.Inventory.Items.Clear();
        player.Messages.Clear();
        result = interact(null);
        check(result && fuel() == 3 && chest.Inventory.Items[0].m_stack == 1 && player.Messages.Count == 1,
            "Repeated valid fuel interactions were throttled or emitted multiple messages.");

        player.Messages.Clear();
        result = interact(null);
        check(!result && fuel() == 3 && chest.Inventory.Items[0].m_stack == 1
            && player.LastMessage == "$msg_itsfull", "Full-station feedback or capacity guard was lost.");

        oven.Fuel = smelter.Fuel = 0;
        chest.Inventory.Items.Clear();
        player.Messages.Clear();
        result = interact(null);
        check(!result && fuel() == 0 && player.LastMessage == "$msg_donthaveany " + fuelName,
            "Missing fuel in both inventory and storage no longer showed vanilla's message.");

        SupplyTransfer.Clear();
        chest.Inventory.Items.Add(Item(fuelName, 1));
        SupplyFromStoragePlugin.FuelAutomationEnabled = false;
        player.Messages.Clear();
        result = interact(null);
        check(!result && fuel() == 0 && chest.Inventory.Items[0].m_stack == 1
            && player.LastMessage == "$msg_donthaveany " + fuelName,
            "Disabled automation consumed fuel or suppressed vanilla feedback.");

        SupplyFromStoragePlugin.FuelAutomationEnabled = true;
        chest.Allowed = false;
        result = interact(null);
        check(!result && fuel() == 0 && chest.Inventory.Items[0].m_stack == 1,
            "Inaccessible storage was used for fuel.");
    }

    /// <summary>Fuel and processing-input configuration switches do not control each other.</summary>
    private static void AutomationTogglesAreIndependent(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_coal", 1), Item("$item_ore", 1));
        var smelter = new Smelter
        {
            m_fuelItem = StationDrop("Coal", "$item_coal"),
            m_maxFuel = 1,
            m_maxOre = 1
        };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        SupplyFromStoragePlugin.FuelAutomationEnabled = false;
        smelter.OnAddFuel(new Switch(), player, null);
        smelter.OnAddOre(new Switch(), player, null);

        check(smelter.Fuel == 0 && smelter.QueueSize == 1,
            "Disabling fuel also disabled processing inputs.");

        player = Reset();
        chest = Chest(1, 0, 0, Item("$item_coal", 1), Item("$item_ore", 1));
        smelter = new Smelter
        {
            m_fuelItem = StationDrop("Coal", "$item_coal"),
            m_maxFuel = 1,
            m_maxOre = 1
        };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        SupplyFromStoragePlugin.ProcessingAutomationEnabled = false;
        smelter.OnAddFuel(new Switch(), player, null);
        smelter.OnAddOre(new Switch(), player, null);

        check(smelter.Fuel == 1 && smelter.QueueSize == 0
            && chest.Inventory.Items.Exists(item => item.m_shared.m_name == "$item_ore" && item.m_stack == 1),
            "Disabling processing inputs also disabled fuel or consumed a processing input.");
    }

    /// <summary>Station update callbacks no longer supply items without a user interaction.</summary>
    private static void StationUpdatesDoNotSupplyItems(Action<bool, string> check)
    {
        Reset();
        var chest = Chest(1, 0, 0, Item("$item_coal", 2), Item("$item_ore", 2), Item("$item_wood", 2));
        var smelter = new Smelter
        {
            m_fuelItem = StationDrop("Coal", "$item_coal"),
            m_maxFuel = 2,
            m_maxOre = 2
        };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        var station = new CookingStation
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2
        };
        station.transform.position = new UnityEngine.Vector3(1, 0, 0);
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);

        smelter.UpdateSmelter();
        station.Update();
        fireplace.UpdateFireplace();

        check(smelter.Fuel == 0 && smelter.QueueSize == 0 && station.Fuel == 0 && fireplace.Fuel == 0,
            "A station update supplied items without an E interaction.");
        check(chest.Inventory.Items.TrueForAll(item => item.m_stack == 2),
            "A station update consumed storage items.");
    }

    /// <summary>The CraftFromStorage string-removal hook does not intercept storage-item transfers.</summary>
    private static void CraftFromStorageInventoryHookDoesNotInterceptTransfers(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_ore", 2));
        var smelter = new Smelter { m_maxOre = 1 };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        CraftFromStorageConsumptionProbe.Active = true;
        CraftFromStorageConsumptionProbe.Calls = 0;

        smelter.OnAddOre(new Switch(), player, null);

        check(smelter.QueueSize == 1 && chest.Inventory.Items[0].m_stack == 1,
            "Storage transfer did not consume exactly one machine input.");
        check(CraftFromStorageConsumptionProbe.Calls == 0,
            "CraftFromStorage's string-based inventory hook intercepted a storage transfer.");
        player.Inventory.RemoveItem("$item_ore", 1, -1, true);
        check(CraftFromStorageConsumptionProbe.Calls == 1 && player.Inventory.Items.Count == 0,
            "The compatibility probe did not target CraftFromStorage's string-removal overload.");

        CraftFromStorageConsumptionProbe.Active = false;
    }

    /// <summary>Container lifecycle patches add and remove containers from the nearby source set.</summary>
    private static void ContainerLifecyclePatchesTrackStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = new Container();
        chest.transform.position = new UnityEngine.Vector3(1, 0, 0);
        chest.Awake();
        check(NearbyStorage.Find(player).Contains(chest), "Container Awake was not tracked.");

        chest.OnDestroyed();
        check(!NearbyStorage.Find(player).Contains(chest), "Container destruction left a stale source.");
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), [typeof(string), typeof(int), typeof(int), typeof(bool)])]
internal static class CraftFromStorageConsumptionProbe
{
    internal static bool Active { get; set; }

    internal static int Calls { get; set; }

    [HarmonyPrefix]
    private static bool BeforeRemove()
    {
        if (!Active)
        {
            return true;
        }

        Calls++;

        return false;
    }
}