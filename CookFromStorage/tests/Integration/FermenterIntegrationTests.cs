using System;
using HarmonyLib;
using UnityEngine;
using ValheimMods.Testing;
using static CookFromStorage.Tests.CookTestSetup;

namespace CookFromStorage.Tests;

internal static class FermenterIntegrationTests
{
    internal static void Run(TestRunner runner)
    {
        var harmony = new Harmony("hoose.valheim.cookfromstorage.fermenter.tests");

        try
        {
            harmony.CreateClassProcessor(typeof(FermenterPatch)).Patch();
            runner.Run(LoadsExactlyOneBaseAndStartsFermentation);
            runner.Run(PreservesInventoryFirstAndExplicitItemUse);
            runner.Run(PreservesFermentingAndFinishedCollection);
            runner.Run(PreservesRoofAndExposureMessages);
            runner.Run(PreservesNoAcceptedItemMessage);
            runner.Run(UsesActualPrefabAcceptanceAndPreservesCheatFlag);
            runner.Run(UsesCurrentModdedConversion);
            runner.Run(EnforcesInclusiveStationAndStorageRadius);
            runner.Run(EnforcesStorageAccessBusyStateAndOwnership);
            runner.Run(SkipsUnavailableStationsAndOtherPlayers);
            runner.Run(PreservesHeldInteractionAndDisabledAutomation);
            runner.Run(RestoresRejectedLastItemAndReleasesReservation);
            runner.Run(RestoresWhenInsertionThrowsBeforeCommit);
            runner.Run(DoesNotRestoreAfterConfirmedCommitThrows);
            runner.Run(DoesNotDuplicateWhenCommitCannotBeConfirmed);
            runner.Run(RechecksStorageAfterShelterRefresh);
            runner.Run(RechecksStationBeforeConsumption);
        }
        finally
        {
            Reset();
            harmony.UnpatchSelf();
        }
    }

    private static void LoadsExactlyOneBaseAndStartsFermentation(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 3));
        var station = Station();
        bool reserved = false;
        station.OnInsert = () => reserved = chest.m_inUse;

        bool first = station.Interact(player, false, false);
        bool second = station.Interact(player, false, false);

        check(first && !second && station.AddedItems == 1 && chest.Inventory.Items[0].m_stack == 2,
            "Fermenter did not load exactly one base and stop accepting inputs while fermenting.");
        check(station.GetStatus() == Fermenter.Status.Fermenting
            && station.GetContent() == "MeadBaseHealthMinor".GetStableHashCode(),
            "Fermenter did not record the selected base.");
        check(reserved && !chest.m_inUse && player.LastMessage == null,
            "Successful insertion failed to reserve/release storage or showed the no-input message.");
    }

    private static void PreservesInventoryFirstAndExplicitItemUse(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 2));
        player.Inventory.Items.Add(Item("$item_meadbase", "MeadBaseHealthMinor", 1));
        var station = Station();

        bool result = station.Interact(player, false, false);
        check(result && player.Inventory.Items.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "Storage was consumed before the player's accepted base.");

        station = Station();
        var manualItem = Item("$item_meadbase", "MeadBaseHealthMinor", 1);
        player.Inventory.Items.Add(manualItem);
        result = station.UseItem(player, manualItem);
        check(result && player.Inventory.Items.Count == 0 && chest.Inventory.Items[0].m_stack == 2,
            "Explicit item use also consumed storage.");
    }

    private static void PreservesFermentingAndFinishedCollection(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 2));
        var station = Station();
        station.Content = "MeadBaseHealthMinor".GetStableHashCode();

        bool result = station.Interact(player, false, false);
        check(!result && station.AddedItems == 0 && chest.Inventory.Items[0].m_stack == 2,
            "An already fermenting station consumed another base.");

        station.ReadyForCollection = true;
        result = station.Interact(player, false, false);
        check(result && station.Collections == 1 && station.Content == 0 && station.AddedItems == 0
            && chest.Inventory.Items[0].m_stack == 2, "Collection also loaded a base or changed vanilla output handling.");
    }

    private static void PreservesRoofAndExposureMessages(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 1));
        var station = Station();
        station.OnUpdateCover = () => station.m_hasRoof = false;

        bool result = station.Interact(player, false, false);
        check(!result && player.LastMessage == "$piece_fermenter_needroof"
            && chest.Inventory.Items[0].m_stack == 1, "Missing-roof feedback or no-consumption guard was lost.");

        station.OnUpdateCover = () =>
        {
            station.m_hasRoof = true;
            station.m_exposed = true;
        };
        result = station.Interact(player, false, false);
        check(!result && player.LastMessage == "$piece_fermenter_exposed"
            && chest.Inventory.Items[0].m_stack == 1, "Exposed-station feedback or no-consumption guard was lost.");

        station.OnUpdateCover = () => station.m_exposed = false;
        result = station.Interact(player, false, false);
        check(result && station.AddedItems == 1 && chest.Inventory.Items.Count == 0,
            "A newly sheltered Fermenter did not load its stored base.");
    }

    private static void PreservesNoAcceptedItemMessage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_wood", "Wood", 2));
        var station = Station();

        bool result = station.Interact(player, false, false);

        check(!result && player.LastMessage == "$msg_noprocessableitems" && chest.Inventory.Items[0].m_stack == 2,
            "No-accepted-input handling changed or an unrelated item was consumed.");
    }

    private static void UsesActualPrefabAcceptanceAndPreservesCheatFlag(Action<bool, string> check)
    {
        var player = Reset();
        var wrongPrefab = Item("$item_meadbase", "UnacceptedBase", 1);
        var accepted = Item("$item_meadbase", "MeadBaseHealthMinor", 2);
        accepted.m_cheated = true;
        var chest = Chest(1, 0, 0, wrongPrefab, accepted);
        var station = Station();

        bool result = station.Interact(player, false, false);

        check(result && wrongPrefab.m_stack == 1 && accepted.m_stack == 1 && station.CheatedInput,
            "Shared-name matching bypassed actual prefab acceptance or discarded the cheat flag.");
        check(chest.Inventory.Items.Count == 2 && station.AddedItems == 1,
            "More than the single accepted base was consumed.");
    }

    private static void EnforcesInclusiveStationAndStorageRadius(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(20, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 2));
        var station = Station();
        station.transform.position = new Vector3(20.01f, 0, 0);

        bool result = station.Interact(player, false, false);
        check(!result && chest.Inventory.Items[0].m_stack == 2, "An out-of-range station consumed storage.");

        station.transform.position = new Vector3(20, 0, 0);
        chest.transform.position = new Vector3(20.01f, 0, 0);
        result = station.Interact(player, false, false);
        check(!result && chest.Inventory.Items[0].m_stack == 2, "Out-of-range storage was consumed.");

        chest.transform.position = new Vector3(20, 0, 0);
        result = station.Interact(player, false, false);
        check(result && chest.Inventory.Items[0].m_stack == 1, "The inclusive 20m boundary rejected a valid input.");
    }

    private static void UsesCurrentModdedConversion(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_custom_base", "CustomBrewBase", 2));
        var station = Station();
        station.m_conversion[0].m_from = new ItemDrop
        {
            name = "CustomBrewBase",
            m_itemData = Item("$item_custom_base", "CustomBrewBase", 1)
        };

        bool result = station.Interact(player, false, false);

        check(result && chest.Inventory.Items[0].m_stack == 1
            && station.Content == "CustomBrewBase".GetStableHashCode(),
            "Fermenter storage loading ignored its current modded conversion.");
    }

    private static void EnforcesStorageAccessBusyStateAndOwnership(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 2));
        var station = Station();
        chest.Allowed = false;
        check(!station.Interact(player, false, false), "Denied storage was consumed.");

        chest.Allowed = true;
        chest.m_inUse = true;
        check(!station.Interact(player, false, false), "Busy storage was consumed.");

        chest.m_inUse = false;
        chest.m_nview.Owner = false;
        check(!station.Interact(player, false, false), "Remote-owned storage was consumed.");

        chest.m_nview.Owner = true;
        PrivateArea.Allowed = false;
        check(station.Interact(player, false, false) && station.AddedItems == 0,
            "Ward-denied station failed to preserve vanilla handling.");
        check(chest.Inventory.Items[0].m_stack == 2, "Unavailable storage or station lost ingredients.");
    }

    private static void SkipsUnavailableStationsAndOtherPlayers(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 2));
        var station = Station();
        station.m_nview.Owner = false;
        check(!station.Interact(player, false, false), "A remote-owned station loaded from storage.");

        station.m_nview.Owner = true;
        station.m_nview.Valid = false;
        check(!station.Interact(player, false, false), "An invalid station loaded from storage.");

        station.m_nview.Valid = true;
        station.isActiveAndEnabled = false;
        check(!station.Interact(player, false, false), "An inactive station loaded from storage.");

        station.isActiveAndEnabled = true;
        check(!station.Interact(new Player(), false, false) && chest.Inventory.Items[0].m_stack == 2,
            "Another player's interaction consumed local storage.");
    }

    private static void PreservesHeldInteractionAndDisabledAutomation(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("$item_meadbase", "MeadBaseHealthMinor", 2));
        var station = Station();

        check(!station.Interact(player, true, false) && station.CoverUpdates == 0,
            "Held interaction bypassed vanilla's early rejection.");

        CookFromStoragePlugin.AutomaticIngredientsEnabled = false;
        check(!station.Interact(player, false, false) && chest.Inventory.Items[0].m_stack == 2,
            "Disabled automation loaded from storage.");

        player.Inventory.Items.Add(Item("$item_meadbase", "MeadBaseHealthMinor", 1));
        check(station.Interact(player, false, false) && player.Inventory.Items.Count == 0,
            "Disabled automation also disabled vanilla inventory input.");
    }

    private static void RestoresRejectedLastItemAndReleasesReservation(Action<bool, string> check)
    {
        var player = Reset();
        var item = Item("$item_meadbase", "MeadBaseHealthMinor", 1);
        var chest = Chest(1, 0, 0, item);
        var station = Station();
        station.RejectInput = true;

        bool result = station.Interact(player, false, false);

        check(!result && chest.Inventory.ContainsItem(item) && item.m_stack == 1 && station.Content == 0,
            "Rejected insertion did not restore the removed final stack item.");
        check(!chest.m_inUse, "Rejected insertion left storage reserved.");
    }

    private static void RestoresWhenInsertionThrowsBeforeCommit(Action<bool, string> check)
    {
        var player = Reset();
        var item = Item("$item_meadbase", "MeadBaseHealthMinor", 2);
        var chest = Chest(1, 0, 0, item);
        var station = Station();
        station.OnInsert = () => throw new InvalidOperationException("Test callback failed before insertion.");

        bool result = station.Interact(player, false, false);

        check(!result && item.m_stack == 2 && station.Content == 0 && !chest.m_inUse,
            "A callback failure before insertion lost the base or reservation.");
    }

    private static void DoesNotRestoreAfterConfirmedCommitThrows(Action<bool, string> check)
    {
        var player = Reset();
        var item = Item("$item_meadbase", "MeadBaseHealthMinor", 2);
        var chest = Chest(1, 0, 0, item);
        var station = Station();
        station.ThrowAfterInsertion = true;

        bool result = station.Interact(player, false, false);

        check(result && item.m_stack == 1 && station.AddedItems == 1 && !chest.m_inUse,
            "An exception after confirmed insertion duplicated the input or left storage reserved.");
    }

    private static void DoesNotDuplicateWhenCommitCannotBeConfirmed(Action<bool, string> check)
    {
        var player = Reset();
        var item = Item("$item_meadbase", "MeadBaseHealthMinor", 2);
        var chest = Chest(1, 0, 0, item);
        var station = Station();
        station.OnReadContent = () => throw new InvalidOperationException("Test content read failed.");

        bool result = station.Interact(player, false, false);

        check(!result && item.m_stack == 1 && station.AddedItems == 1 && !chest.m_inUse,
            "An unconfirmable insertion duplicated the base or left storage reserved.");
    }

    private static void RechecksStorageAfterShelterRefresh(Action<bool, string> check)
    {
        var player = Reset();
        var item = Item("$item_meadbase", "MeadBaseHealthMinor", 2);
        var chest = Chest(1, 0, 0, item);
        var station = Station();
        station.OnUpdateCover = () => chest.m_nview.Owner = false;

        bool result = station.Interact(player, false, false);

        check(!result && item.m_stack == 2 && station.AddedItems == 0 && !chest.m_inUse,
            "Storage whose ownership changed during preparation was consumed.");
    }

    private static Fermenter Station()
    {
        var station = new Fermenter();
        station.transform.position = new Vector3(1, 0, 0);
        station.m_conversion.Add(new Fermenter.ItemConversion
        {
            m_from = new ItemDrop
            {
                name = "MeadBaseHealthMinor",
                m_itemData = Item("$item_meadbase", "MeadBaseHealthMinor", 1)
            }
        });

        return station;
    }

    private static void RechecksStationBeforeConsumption(Action<bool, string> check)
    {
        var player = Reset();
        var item = Item("$item_meadbase", "MeadBaseHealthMinor", 2);
        var chest = Chest(1, 0, 0, item);
        var station = Station();
        station.OnUpdateCover = () => station.m_nview.Owner = false;

        bool result = station.Interact(player, false, false);

        check(!result && item.m_stack == 2 && station.AddedItems == 0 && !chest.m_inUse,
            "Station ownership changed before removal but the stored base was consumed.");
    }
}