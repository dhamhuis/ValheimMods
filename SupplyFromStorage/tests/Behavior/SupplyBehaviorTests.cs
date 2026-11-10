using System;
using UnityEngine;
using ValheimMods.Testing;
using static SupplyFromStorage.Tests.SupplyTestSetup;

namespace SupplyFromStorage.Tests;

internal static class SupplyBehaviorTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(FireplacesUseOnlyStorageFuel);
        runner.Run(CookingStationsUseFuelWithoutCookingIngredients);
        runner.Run(SmeltersUseAcceptedFuelAndProcessingInputs);
        runner.Run(OnlyCurrentSmelterConversionsAreAccepted);
        runner.Run(VanillaProcessingFamiliesUseTheirCurrentConversions);
        runner.Run(FuelAndInputCapacityAreNeverExceeded);
        runner.Run(FailedMachineInsertionRestoresStorage);
        runner.Run(StorageAndStationsUseInclusivePlayerCenteredRange);
        runner.Run(ConfiguredStorageRadiusLimitsStorageButNotStationReach);
        runner.Run(InaccessibleAndBusyContainersAreSkipped);
        runner.Run(StaleContainersAndQuantitiesAreRevalidated);
        runner.Run(RemoteOwnedStorageIsNotConsumed);
        runner.Run(UnsuccessfulInteractionChecksAreRateLimited);
    }

    /// <summary>Fireplace fuel is supplied from storage without consuming the player's inventory.</summary>
    private static void FireplacesUseOnlyStorageFuel(Action<bool, string> check)
    {
        var player = Reset();
        player.Inventory.Items.Add(Item("$item_wood", 4));
        var chest = Chest(2, 0, 0, Item("$item_wood", 5));
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 3
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);

        var result = SupplyTransfer.TransferFireplaceFuel(fireplace, player, NearbyStorage.Find(player));

        check(result == SupplyTransfer.TransferResult.Transferred, "Fireplace did not accept its fuel.");
        check(fireplace.Fuel == 1 && chest.Inventory.Items[0].m_stack == 4,
            "A fireplace interaction transferred more than one fuel item.");
        check(SupplyTransfer.TransferFireplaceFuel(fireplace, player, NearbyStorage.Find(player))
            == SupplyTransfer.TransferResult.Transferred && fireplace.Fuel == 2
            && chest.Inventory.Items[0].m_stack == 3, "A subsequent fireplace interaction did not transfer one item.");
        check(player.Inventory.Items[0].m_stack == 4, "Automatic fueling consumed the player's inventory.");
    }

    /// <summary>Cooking stations receive fuel only and never receive cooking ingredients.</summary>
    private static void CookingStationsUseFuelWithoutCookingIngredients(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", 2), Item("$item_raw_meat", 3));
        var station = new CookingStation
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2
        };
        station.transform.position = new UnityEngine.Vector3(1, 0, 0);

        var result = SupplyTransfer.TransferCookingStationFuel(station, player, NearbyStorage.Find(player));

        check(result == SupplyTransfer.TransferResult.Transferred, "Cooking station did not accept fuel.");
        check(station.Fuel == 1 && chest.Inventory.Items[0].m_stack == 1,
            "A cooking-station interaction transferred more than one fuel item.");
        check(chest.Inventory.Items.Exists(item => item.m_shared.m_name == "$item_raw_meat" && item.m_stack == 3),
            "Cooking ingredients were consumed by automatic supply.");
    }

    /// <summary>Smelters accept fuel and their current conversion inputs from storage.</summary>
    private static void SmeltersUseAcceptedFuelAndProcessingInputs(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_coal", 3), Item("$item_iron", 2), Item("$item_stone", 4));
        var smelter = new Smelter
        {
            m_fuelItem = StationDrop("Coal", "$item_coal"),
            m_maxFuel = 2,
            m_maxOre = 1
        };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("IronOre", "$item_iron"),
            m_to = StationDrop("Iron", "$item_iron_ingot")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        var sources = NearbyStorage.Find(player);

        var fuel = SupplyTransfer.TransferSmelterFuel(smelter, player, sources);
        var input = SupplyTransfer.TransferSmelterInputs(smelter, player, sources);

        check(fuel == SupplyTransfer.TransferResult.Transferred && smelter.Fuel == 1, "Smelter fuel did not transfer once.");
        check(input == SupplyTransfer.TransferResult.Transferred && smelter.QueueSize == 1, "Smelter input was not queued.");
        check(chest.Inventory.Items.Find(item => item.m_shared.m_name == "$item_coal").m_stack == 2,
            "Smelter fuel debit was incorrect.");
        check(chest.Inventory.Items.Find(item => item.m_shared.m_name == "$item_iron").m_stack == 1,
            "Smelter input debit was incorrect.");
        check(chest.Inventory.Items.Find(item => item.m_shared.m_name == "$item_stone").m_stack == 4,
            "Unrelated storage items were consumed.");
    }

    /// <summary>Only current Smelter conversion entries are accepted as processing inputs.</summary>
    private static void OnlyCurrentSmelterConversionsAreAccepted(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_barley", 2), Item("$item_flax", 2));
        var smelter = new Smelter
        {
            m_maxOre = 1
        };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Barley", "$item_barley")
        });
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);

        var result = SupplyTransfer.TransferSmelterInputs(smelter, player, NearbyStorage.Find(player));

        check(result == SupplyTransfer.TransferResult.Transferred && smelter.LastInput == "Barley",
            "The active conversion input was not selected.");
        check(chest.Inventory.Items.Exists(item => item.m_shared.m_name == "$item_flax" && item.m_stack == 2),
            "An unrelated processing input was consumed.");
    }

    /// <summary>Vanilla machine families transfer only the inputs named by their current conversion lists.</summary>
    private static void VanillaProcessingFamiliesUseTheirCurrentConversions(Action<bool, string> check)
    {
        var scenarios = new[]
        {
            ("Charcoal kiln wood", "Wood", "$item_wood"),
            ("Smelter ore", "CopperOre", "$item_copper_ore"),
            ("Blast furnace scrap", "BlackMetalScrap", "$item_blackmetal_scrap"),
            ("Windmill barley", "Barley", "$item_barley"),
            ("Spinning-wheel flax", "Flax", "$item_flax"),
            ("Eitr refinery input", "Softtissue", "$item_softtissue")
        };

        foreach (var (label, prefabName, itemName) in scenarios)
        {
            var player = Reset();
            var chest = Chest(1, 0, 0, Item(itemName, 1));
            var smelter = new Smelter
            {
                m_maxOre = 1
            };
            smelter.m_conversion.Add(new Smelter.ItemConversion
            {
                m_from = StationDrop(prefabName, itemName)
            });
            smelter.transform.position = new Vector3(1, 0, 0);

            var result = SupplyTransfer.TransferSmelterInputs(smelter, player, NearbyStorage.Find(player));

            check(result == SupplyTransfer.TransferResult.Transferred && smelter.LastInput == prefabName
                && chest.Inventory.Items.Count == 0, $"{label} did not use its current Smelter conversion.");
        }
    }

    /// <summary>Fuel and processing queues are not filled past their configured capacities.</summary>
    private static void FuelAndInputCapacityAreNeverExceeded(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", 2), Item("$item_ore", 2));
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2,
            Fuel = 2
        };
        var smelter = new Smelter
        {
            m_maxOre = 2,
            QueueSize = 2
        };
        smelter.m_conversion.Add(new Smelter.ItemConversion
        {
            m_from = StationDrop("Ore", "$item_ore")
        });
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);
        smelter.transform.position = new UnityEngine.Vector3(1, 0, 0);
        var sources = NearbyStorage.Find(player);

        var fuel = SupplyTransfer.TransferFireplaceFuel(fireplace, player, sources);
        var input = SupplyTransfer.TransferSmelterInputs(smelter, player, sources);

        check(fuel == SupplyTransfer.TransferResult.Full && input == SupplyTransfer.TransferResult.Full,
            "A full station reported a successful transfer.");
        check(chest.Inventory.Items.Count == 2 && chest.Inventory.Items[0].m_stack == 2
            && chest.Inventory.Items[1].m_stack == 2, "A full station consumed storage items.");
    }

    /// <summary>A rejected machine RPC restores the removed storage item and reports failure.</summary>
    private static void FailedMachineInsertionRestoresStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", 1));
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 1,
            AcceptFuel = false
        };
        fireplace.transform.position = new Vector3(1, 0, 0);

        var result = SupplyTransfer.TransferFireplaceFuel(fireplace, player, NearbyStorage.Find(player));

        check(result == SupplyTransfer.TransferResult.Failed, "A rejected fuel RPC was treated as successful.");
        check(chest.Inventory.Items.Count == 1 && chest.Inventory.Items[0].m_stack == 1,
            "The failed transfer did not restore its storage item.");
        check(!chest.m_inUse && fireplace.Fuel == 0, "Failed transfer did not release storage or changed machine fuel.");
    }

    /// <summary>Both machine and storage range use an inclusive 20m player-centered sphere.</summary>
    private static void StorageAndStationsUseInclusivePlayerCenteredRange(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(0, 20, 0, Item("$item_wood", 1));
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 1
        };
        fireplace.transform.position = new UnityEngine.Vector3(0, 20, 0);

        check(NearbyStorage.Find(player).Contains(chest), "Storage at exactly 20m was excluded.");
        check(SupplyTransfer.TransferFireplaceFuel(fireplace, player, NearbyStorage.Find(player))
            == SupplyTransfer.TransferResult.Transferred, "A station at exactly 20m was excluded.");

        Reset();
        var outsideChest = Chest(0, 20.001f, 0, Item("$item_wood", 1));
        var nearbyFireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 1
        };
        nearbyFireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);
        check(!NearbyStorage.Find(Player.m_localPlayer).Contains(outsideChest), "Storage outside the sphere was included.");
        check(SupplyTransfer.TransferFireplaceFuel(nearbyFireplace, Player.m_localPlayer, NearbyStorage.Find(Player.m_localPlayer))
            == SupplyTransfer.TransferResult.NoItems, "Out-of-range storage supplied fuel.");
    }

    /// <summary>The configured radius bounds storage within 1-100m; station reach stays fixed at 20m.</summary>
    private static void ConfiguredStorageRadiusLimitsStorageButNotStationReach(Action<bool, string> check)
    {
        var player = Reset();
        SupplyFromStoragePlugin.StorageRadius = 5f;
        var outside = Chest(0, 5.01f, 0, Item("$item_wood", 1));
        var edge = Chest(0, 5, 0, Item("$item_wood", 1));
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 2
        };
        fireplace.transform.position = new Vector3(15, 0, 0);

        var containers = NearbyStorage.Find(player);
        check(containers.Contains(edge) && !containers.Contains(outside), "A 5m radius did not bound storage inclusively.");
        check(SupplyTransfer.TransferFireplaceFuel(fireplace, player, containers) == SupplyTransfer.TransferResult.Transferred
            && edge.Inventory.Items.Count == 0 && outside.Inventory.Items.Count == 1,
            "A small storage radius blocked a station within normal reach or used out-of-range storage.");

        SupplyFromStoragePlugin.StorageRadius = 100f;
        var far = Chest(0, 100, 0, Item("$item_wood", 1));
        check(NearbyStorage.Find(player).Contains(far), "Storage at the 100m maximum was excluded.");

        SupplyFromStoragePlugin.StorageRadius = 500f;
        var beyond = Chest(0, 100.01f, 0, Item("$item_wood", 1));
        check(!NearbyStorage.Find(player).Contains(beyond), "A radius above 100m was not clamped.");

        SupplyFromStoragePlugin.StorageRadius = 0f;
        check(NearbyStorage.Radius == 1f, "A radius below 1m was not clamped.");
    }

    /// <summary>Personal, warded, locally busy and remotely busy containers are unavailable.</summary>
    private static void InaccessibleAndBusyContainersAreSkipped(Action<bool, string> check)
    {
        var player = Reset();
        var denied = Chest(1, 0, 0, Item("$item_wood", 1));
        denied.Allowed = false;
        var locallyBusy = Chest(2, 0, 0, Item("$item_wood", 1));
        locallyBusy.m_inUse = true;
        var remotelyBusy = Chest(3, 0, 0, Item("$item_wood", 1));
        remotelyBusy.m_nview.Owner = false;
        remotelyBusy.m_nview.Zdo.Busy = 1;
        var warded = Chest(4, 0, 0, Item("$item_wood", 1));
        warded.m_checkGuardStone = true;
        PrivateArea.Allowed = false;
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 4
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);

        var sources = NearbyStorage.Find(player);
        var result = SupplyTransfer.TransferFireplaceFuel(fireplace, player, sources);

        check(result == SupplyTransfer.TransferResult.NoItems && fireplace.Fuel == 0,
            "An inaccessible container supplied fuel.");
        check(denied.Inventory.Items[0].m_stack == 1 && locallyBusy.Inventory.Items[0].m_stack == 1
            && remotelyBusy.Inventory.Items[0].m_stack == 1 && warded.Inventory.Items[0].m_stack == 1,
            "An inaccessible container was consumed.");
    }

    /// <summary>Access and quantity changes after discovery are rechecked before consuming.</summary>
    private static void StaleContainersAndQuantitiesAreRevalidated(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", 1));
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 1
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);
        var sources = NearbyStorage.Find(player);
        chest.Allowed = false;

        var accessResult = SupplyTransfer.TransferFireplaceFuel(fireplace, player, sources);
        check(accessResult == SupplyTransfer.TransferResult.NoItems && fireplace.Fuel == 0
            && chest.Inventory.Items[0].m_stack == 1, "A container that lost access was consumed.");

        chest.Allowed = true;
        var staleItem = chest.Inventory.Items[0];
        staleItem.m_stack = 0;
        chest.Inventory.Items.Clear();
        var quantityResult = SupplyTransfer.TransferFireplaceFuel(fireplace, player, sources);
        check(quantityResult == SupplyTransfer.TransferResult.NoItems && fireplace.Fuel == 0,
            "A stale item quantity was consumed.");
    }

    /// <summary>Non-owned storage is skipped instead of risking a concurrent remote debit.</summary>
    private static void RemoteOwnedStorageIsNotConsumed(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", 1));
        chest.m_nview.Owner = false;
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 1
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);

        check(NearbyStorage.Find(player).Count == 0, "Remote-owned storage was treated as locally transferable.");
        check(SupplyTransfer.TransferFireplaceFuel(fireplace, player, NearbyStorage.Find(player))
            == SupplyTransfer.TransferResult.NoItems, "Remote-owned storage was consumed.");
        check(chest.Inventory.Items[0].m_stack == 1, "Remote-owned storage quantity changed.");
    }

    /// <summary>Repeated unsuccessful interaction attempts are throttled, while a later attempt can succeed.</summary>
    private static void UnsuccessfulInteractionChecksAreRateLimited(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        var fireplace = new Fireplace
        {
            m_fuelItem = StationDrop("Wood", "$item_wood"),
            m_maxFuel = 1
        };
        fireplace.transform.position = new UnityEngine.Vector3(1, 0, 0);
        SupplyTransfer.TryHandleFireplaceFuel(fireplace, player, false, false, out _);
        chest.Inventory.Items.Add(Item("$item_wood", 1));
        SupplyTransfer.TryHandleFireplaceFuel(fireplace, player, false, false, out _);
        check(fireplace.Fuel == 0, "An empty-source check was retried immediately.");

        Time.realtimeSinceStartup += 5.1f;
        SupplyTransfer.TryHandleFireplaceFuel(fireplace, player, false, false, out _);
        check(fireplace.Fuel == 1 && chest.Inventory.Items.Count == 0, "The station was not retried after its cooldown.");
    }
}