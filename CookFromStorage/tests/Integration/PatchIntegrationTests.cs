using System;
using HarmonyLib;
using UnityEngine;
using ValheimMods.Testing;
using static CookFromStorage.Tests.CookTestSetup;

namespace CookFromStorage.Tests;

internal static class PatchIntegrationTests
{
    internal static void Run(TestRunner runner)
    {
        var harmony = new Harmony("hoose.valheim.cookfromstorage.tests");

        try
        {
            harmony.CreateClassProcessor(typeof(CookingStationPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ContainerCreatedPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ContainerDestroyedPatch)).Patch();
            runner.Run(TransfersOneAcceptedIngredientOnEachEmptyStationInteraction);
            runner.Run(DirectStationInteractionTransfersStorage);
            runner.Run(DirectInteractionPreservesVanillaGuards);
            runner.Run(DirectInteractionPreservesInventoryAndPickup);
            runner.Run(StorageIngredientsReportRequiredFireAndTransferAfterLighting);
            runner.Run(StorageIngredientsReportFullStation);
            runner.Run(NoStorageIngredientPreservesVanillaMessage);
            runner.Run(UsesPlayerIngredientsBeforeStorageAndPreservesManualInput);
            runner.Run(OnlyCurrentStationConversionsAreTransferred);
            runner.Run(FrostFoundryKeepsCastSelectionManual);
            runner.Run(SkipsOutputPickupAndUnavailableStations);
            runner.Run(RejectingStationRestoresItsStorageIngredient);
            runner.Run(EnforcesStationAndStorageRangeAndAccess);
            runner.Run(ConfiguredStorageRadiusLimitsStorageButNotStationReach);
            runner.Run(DisabledAutomationDoesNotTransfer);
            runner.Run(ContainerLifecyclePatchesTrackStorage);
        }
        finally
        {
            Reset();
            harmony.UnpatchSelf();
        }
    }

    /// <summary>Foundry casts remain manual while inventory input and collection retain vanilla behavior.</summary>
    private static void FrostFoundryKeepsCastSelectionManual(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_test_cast", "TestCast", 2));
        var station = Station(Conversion("TestCast", "$item_test_cast"));
        station.m_name = "$piece_frostfoundry";
        station.name = "piece_FrostFoundry(Clone)";
        station.m_addFoodSwitch = new Switch();

        bool result = station.OnAddFoodSwitch(station.m_addFoodSwitch, player, null);
        check(!result && station.AddedItems.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "The Foundry input switch automatically consumed a stored cast.");

        var transfer = CookingTransfer.TransferOneIngredient(station, player, NearbyStorage.Find(player));
        check(transfer == CookingTransfer.TransferResult.NotReady && chest.Inventory.Items[0].m_stack == 2,
            "The direct transfer path bypassed the Foundry exclusion.");

        player.Inventory.Items.Add(Item("$item_test_cast", "TestCast", 1));
        result = station.OnAddFoodSwitch(station.m_addFoodSwitch, player, null);
        check(result && player.Inventory.Items.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "Foundry exclusion broke vanilla inventory-based cast selection.");

        var manualItem = Item("$item_test_cast", "TestCast", 1);
        player.Inventory.Items.Add(manualItem);
        result = station.OnAddFoodSwitch(station.m_addFoodSwitch, player, manualItem);
        check(result && player.Inventory.Items.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "Foundry exclusion broke explicitly selected cast input.");

        station.DoneItem = true;
        result = station.OnAddFoodSwitch(station.m_addFoodSwitch, player, null);
        check(result && !station.DoneItem && chest.Inventory.Items[0].m_stack == 2,
            "Foundry exclusion broke normal output collection.");
    }

    /// <summary>Stations without a separate food switch also receive one storage ingredient per interaction.</summary>
    private static void DirectStationInteractionTransfersStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 3));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));

        bool first = station.Interact(player, false, false);
        bool second = station.Interact(player, false, false);

        check(first && second && station.AddedItems.Count == 2 && chest.Inventory.Items[0].m_stack == 1,
            "Direct cooking-station interactions did not transfer exactly one storage ingredient each.");
        check(player.LastMessage == null, "Direct storage insertion showed vanilla's no-ingredient message.");
    }

    /// <summary>Held direct interactions and station bodies with a separate food switch remain rejected.</summary>
    private static void DirectInteractionPreservesVanillaGuards(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));

        bool result = station.Interact(player, true, false);
        check(!result && station.AddedItems.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "A vanilla-rejected held interaction consumed storage.");

        station.m_addFoodSwitch = new Switch();
        result = station.Interact(player, false, false);
        check(!result && station.AddedItems.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "A switch-driven station's body bypassed vanilla's interaction guard.");

        result = station.OnAddFoodSwitch(station.m_addFoodSwitch, player, null);
        check(result && station.AddedItems.Count == 1 && chest.Inventory.Items[0].m_stack == 1,
            "The separate food switch failed or transferred more than one ingredient.");
    }

    /// <summary>Direct interactions preserve inventory priority and cooked-item pickup before storage.</summary>
    private static void DirectInteractionPreservesInventoryAndPickup(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        player.Inventory.Items.Add(Item("$item_raw_meat", "RawMeat", 1));

        bool result = station.Interact(player, false, false);
        check(result && player.Inventory.Items.Count == 0 && station.AddedItems.Count == 1
            && chest.Inventory.Items[0].m_stack == 2, "Direct interaction consumed storage before player inventory.");

        station.DoneItem = true;
        result = station.Interact(player, false, false);
        check(result && !station.DoneItem && station.AddedItems.Count == 1 && chest.Inventory.Items[0].m_stack == 2,
            "Direct cooked-item pickup also consumed a storage ingredient.");
    }

    /// <summary>Each empty-station interaction transfers at most one accepted ingredient.</summary>
    private static void TransfersOneAcceptedIngredientOnEachEmptyStationInteraction(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 3));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));

        bool first = station.OnAddFoodSwitch(new Switch(), player, null);
        bool second = station.OnAddFoodSwitch(new Switch(), player, null);

        check(first && second && station.AddedItems.Count == 2 && chest.Inventory.Items[0].m_stack == 1,
            "Repeated interactions did not add exactly one ingredient each.");
        check(player.LastMessage == null, "Successful storage insertion showed vanilla's no-ingredient message.");
    }

    /// <summary>Vanilla inventory ingredients and explicitly selected items retain priority over storage.</summary>
    private static void UsesPlayerIngredientsBeforeStorageAndPreservesManualInput(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        var inventoryItem = Item("$item_raw_meat", "RawMeat", 1);
        player.Inventory.Items.Add(inventoryItem);

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);

        check(result && player.Inventory.Items.Count == 0 && station.AddedItems.Count == 1
            && chest.Inventory.Items[0].m_stack == 2, "Storage was used before vanilla's player-inventory ingredient.");

        var manualItem = Item("$item_raw_meat", "RawMeat", 1);
        player.Inventory.Items.Add(manualItem);
        result = station.OnAddFoodSwitch(new Switch(), player, manualItem);
        check(result && station.AddedItems.Count == 2 && chest.Inventory.Items[0].m_stack == 2,
            "An explicitly selected vanilla ingredient also consumed storage.");
    }

    /// <summary>Only ingredients present in the station's live conversion list are taken from storage.</summary>
    private static void OnlyCurrentStationConversionsAreTransferred(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(
            1,
            0,
            0,
            Item("$item_raw_fish", "RawFish", 2),
            Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);

        check(result && station.AddedItems[0] == "RawMeat" && chest.Inventory.Items[0].m_stack == 2
            && chest.Inventory.Items[1].m_stack == 1, "An unrelated food item was consumed.");
    }

    /// <summary>Storage ingredients report missing fire without consuming anything, then transfer once the fire is lit.</summary>
    private static void StorageIngredientsReportRequiredFireAndTransferAfterLighting(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.m_requireFire = true;
        station.FireLit = false;

        bool result = station.Interact(player, false, false);
        check(!result && player.LastMessage == "$msg_needfire" && chest.Inventory.Items[0].m_stack == 2
            && station.AddedItems.Count == 0, "Missing fire was incorrectly reported as missing cookable ingredients.");

        station.FireLit = true;
        player.LastMessage = null;
        result = station.Interact(player, false, false);
        check(result && station.AddedItems.Count == 1 && chest.Inventory.Items[0].m_stack == 1
            && player.LastMessage == null, "A lit cooking rack did not accept the stored ingredient.");
    }

    /// <summary>A full station with available storage ingredients reports lack of room rather than missing ingredients.</summary>
    private static void StorageIngredientsReportFullStation(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.AddedItems.AddRange(["RawMeat", "RawMeat"]);

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && player.LastMessage == "$msg_nocookroom" && chest.Inventory.Items[0].m_stack == 2,
            "A full station reported missing ingredients or consumed storage.");
    }

    /// <summary>Without an accepted storage ingredient, vanilla still reports no cookable items even if fire is missing.</summary>
    private static void NoStorageIngredientPreservesVanillaMessage(Action<bool, string> check)
    {
        var player = Reset();
        Chest(1, 0, 0, Item("$item_raw_fish", "RawFish", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.m_requireFire = true;
        station.FireLit = false;

        bool result = station.Interact(player, false, false);
        check(!result && player.LastMessage == "$msg_nocookableitems" && station.AddedItems.Count == 0,
            "No accepted ingredients incorrectly showed a fire or capacity message.");
    }

    /// <summary>Cooked-item pickup, full slots, unlit fire and remote ownership never consume storage.</summary>
    private static void SkipsOutputPickupAndUnavailableStations(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.DoneItem = true;
        bool result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(result && !station.DoneItem && station.AddedItems.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "A cooked-item pickup was intercepted as a storage transfer.");

        station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.m_slots = [new Transform()];
        station.AddedItems.Add("RawMeat");
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && station.AddedItems.Count == 1 && chest.Inventory.Items[0].m_stack == 2,
            "A full cooking station consumed a storage ingredient.");

        station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.m_requireFire = true;
        station.FireLit = false;
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && chest.Inventory.Items[0].m_stack == 2,
            "A fire-required station consumed an ingredient while its fire was unlit.");

        station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.m_nview.Owner = false;
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && chest.Inventory.Items[0].m_stack == 2,
            "A remote-owned cooking station was modified.");
    }

    /// <summary>A rejected station insertion restores the original storage stack.</summary>
    private static void RejectingStationRestoresItsStorageIngredient(Action<bool, string> check)
    {
        var player = Reset();
        var ingredient = Item("$item_raw_meat", "RawMeat", 3);
        var chest = Chest(1, 0, 0, ingredient);
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.RejectInput = true;

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);

        check(!result && ingredient.m_stack == 3 && chest.Inventory.Items.Contains(ingredient)
            && station.AddedItems.Count == 0, "A rejected ingredient was not restored to its original stack.");
    }

    /// <summary>Storage permissions and the player-centered radius are rechecked before consuming an ingredient.</summary>
    private static void EnforcesStationAndStorageRangeAndAccess(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(20, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.transform.position = new Vector3(21, 0, 0);

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && chest.Inventory.Items[0].m_stack == 2, "A station outside the player-centered radius was used.");

        station.transform.position = new Vector3(1, 0, 0);
        chest.transform.position = new Vector3(20.01f, 0, 0);
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && chest.Inventory.Items[0].m_stack == 2, "Storage outside the inclusive radius was used.");

        chest.transform.position = new Vector3(1, 0, 0);
        chest.Allowed = false;
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && chest.Inventory.Items[0].m_stack == 2, "Permission-denied storage was consumed.");

        chest.Allowed = true;
        chest.m_nview.Owner = false;
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && chest.Inventory.Items[0].m_stack == 2, "Remote-owned storage was consumed.");
    }

    /// <summary>The configured radius bounds storage within 1-100m; station reach stays fixed at 20m.</summary>
    private static void ConfiguredStorageRadiusLimitsStorageButNotStationReach(Action<bool, string> check)
    {
        var player = Reset();
        CookFromStoragePlugin.StorageRadius = 5f;
        var outside = Chest(5.01f, 0, 0, Item("$item_raw_meat", "RawMeat", 2));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        station.transform.position = new Vector3(15, 0, 0);

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && outside.Inventory.Items[0].m_stack == 2, "Storage outside a 5m radius was used.");

        var edge = Chest(0, 5, 0, Item("$item_raw_meat", "RawMeat", 1));
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(result && edge.Inventory.Items.Count == 0 && outside.Inventory.Items[0].m_stack == 2,
            "A small storage radius blocked a station within normal reach or skipped storage at the radius edge.");

        CookFromStoragePlugin.StorageRadius = 500f;
        check(NearbyStorage.Radius == 100f, "A radius above 100m was not clamped.");
        CookFromStoragePlugin.StorageRadius = 0f;
        check(NearbyStorage.Radius == 1f, "A radius below 1m was not clamped.");
    }

    /// <summary>The configuration toggle disables storage transfers without changing vanilla input handling.</summary>
    private static void DisabledAutomationDoesNotTransfer(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 1));
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));
        CookFromStoragePlugin.AutomaticIngredientsEnabled = false;

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);

        check(!result && station.AddedItems.Count == 0 && chest.Inventory.Items[0].m_stack == 1,
            "Disabled automation transferred an ingredient.");
    }

    /// <summary>Container lifecycle patches add and remove containers from the shared registry.</summary>
    private static void ContainerLifecyclePatchesTrackStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = new Container();
        chest.transform.position = new Vector3(1, 0, 0);
        chest.Inventory.Items.Add(Item("$item_raw_meat", "RawMeat", 1));
        chest.Awake();
        var station = Station(Conversion("RawMeat", "$item_raw_meat"));

        bool result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(result && station.AddedItems.Count == 1, "Container creation did not register the storage source.");

        chest = new Container();
        chest.transform.position = new Vector3(1, 0, 0);
        chest.Inventory.Items.Add(Item("$item_raw_meat", "RawMeat", 1));
        chest.Awake();
        chest.OnDestroyed();
        station = Station(Conversion("RawMeat", "$item_raw_meat"));
        result = station.OnAddFoodSwitch(new Switch(), player, null);
        check(!result && station.AddedItems.Count == 0, "Destroyed storage remained registered.");
    }
}