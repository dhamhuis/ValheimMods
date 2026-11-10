using System;
using System.Linq;
using UnityEngine;
using ValheimMods.Common.InputSelection;
using ValheimMods.Testing;

namespace SupplyFromStorage.Tests;

internal static class InputSelectionTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(PreferencesRoundTripAndPreserveUnknownTypes);
        runner.Run(InvalidPreferencesAreRejected);
        runner.Run(UnrankedTypesHaveStableOrder);
        runner.Run(PreferenceEditingPreservesOtherRules);
        runner.Run(HoldLatchesUntilRelease);
        runner.Run(TargetChangesAndMenusStopHeldInput);
        runner.Run(SelectionIsEphemeralButSurvivesRelease);
        runner.Run(PreferredStorageBeatsOtherInventory);
        runner.Run(InventoryWinsWithinSelectedType);
        runner.Run(ExclusionsSuppressVanillaInventoryFallback);
        runner.Run(ExplicitSelectionOverridesExclusion);
        runner.Run(ExactSelectionNeverSubstitutes);
        runner.Run(HoldContinuesIntoStorageWithoutChangingType);
        runner.Run(FullAndInaccessibleStationsConsumeNothing);
        runner.Run(SelectedInputSkipsIneligibleStorage);
        runner.Run(RejectedInventoryTransferRestoresTheSameItem);
        runner.Run(RejectedStorageTransferRestoresAndUnlocks);
        runner.Run(PostCommitExceptionDoesNotDuplicate);
        runner.Run(InvalidPreferencesBlockWithoutFallback);
        runner.Run(OtherStationsAndExplicitUseRemainVanilla);
    }

    private static void PreferencesRoundTripAndPreserveUnknownTypes(Action<bool, string> check)
    {
        var preferences = InputPreferences.Parse("Wood,ModdedWood,FineWood|FineWood,MissingType");
        var restored = InputPreferences.Parse(preferences.Serialize());
        check(restored.Serialize() == preferences.Serialize(), "Preference serialization was not stable.");
        check(restored.Choose(new[] { "FineWood", "ModdedWood" }) == "ModdedWood", "Rank/exclusion was not restored.");
        check(restored.IsExcluded("MissingType"), "Unknown excluded identifier was lost.");
    }

    private static void InvalidPreferencesAreRejected(Action<bool, string> check)
    {
        foreach (string value in new[] { "Wood", "Wood||", "Wood,Wood|", "Wood,|", " Wood|", "|Wood,Wood", "Wood|\n" })
        {
            bool rejected = false;

            try
            {
                InputPreferences.Parse(value);
            }
            catch (FormatException)
            {
                rejected = true;
            }

            check(rejected, "Invalid preferences were accepted: " + value);
        }
    }

    private static void UnrankedTypesHaveStableOrder(Action<bool, string> check)
    {
        var preferences = InputPreferences.Parse("Wood|");
        check(preferences.Order(new[] { "ZMod", "FineWood", "Wood", "AMod" })
            .SequenceEqual(new[] { "Wood", "AMod", "FineWood", "ZMod" }), "Unranked order depended on inventory enumeration.");
        check(InputPreferences.Parse("|Wood,FineWood").Choose(new[] { "Wood", "FineWood" }) == null,
            "All-excluded policy chose an input.");
    }

    private static void PreferenceEditingPreservesOtherRules(Action<bool, string> check)
    {
        var preferences = InputPreferences.Parse("MissingType,Wood,FineWood|FineWood");
        var changed = preferences.MoveFirst("FineWood", new[] { "Wood", "FineWood", "RoundLog" });
        check(changed.Serialize() == "FineWood,MissingType,Wood,RoundLog|FineWood", "Reordering lost unknown IDs or exclusions.");
        check(!changed.ToggleExcluded("FineWood").IsExcluded("FineWood"), "Excluded type could not be allowed again.");
        check(preferences.IsExcluded("FineWood"), "Editing mutated the old policy snapshot.");
    }

    private static void HoldLatchesUntilRelease(Action<bool, string> check)
    {
        var session = new InputTypeSession();
        var station = new object();
        var preferences = InputPreferences.Parse("Wood,FineWood|");
        session.Observe(station, true, true, false);
        check(session.Resolve(preferences, new[] { "Wood", "FineWood" }) == "Wood", "Initial auto choice ignored priority.");
        session.Observe(station, true, false, false);
        check(session.Resolve(preferences, new[] { "FineWood" }) == "Wood", "Hold switched to another available type.");
        check(session.Stop() && !session.Stop(), "A stopped hold would repeat its notification.");
        check(session.Resolve(preferences, new[] { "FineWood" }) == null, "Stopped hold resumed.");
        check(session.HeldType == "Wood" && session.IsStopped, "HUD state lost the stopped held type.");
        session.Observe(station, false, false, false);
        check(session.HeldType == null && !session.IsStopped, "Released hold retained stale HUD state.");
        session.Observe(station, true, true, false);
        check(session.Resolve(preferences, new[] { "FineWood" }) == "FineWood", "Release/repress did not choose a new type.");
    }

    private static void TargetChangesAndMenusStopHeldInput(Action<bool, string> check)
    {
        var session = new InputTypeSession();
        var station = new object();
        session.Observe(station, true, true, false);
        session.Observe(new object(), true, false, false);
        check(!session.CanInsert, "Moving to another target while holding resumed insertion.");
        session.Observe(station, true, true, false);
        session.Observe(station, true, false, true);
        check(!session.CanInsert, "Opening another UI did not stop held insertion.");
        session.Observe(station, false, false, false);
        session.Observe(station, true, true, false);
        check(session.CanInsert, "New press after interruption stayed blocked.");
    }

    private static void SelectionIsEphemeralButSurvivesRelease(Action<bool, string> check)
    {
        var session = new InputTypeSession();
        var station = new object();
        session.Observe(station, false, false, false);
        session.Select("FineWood");
        session.Observe(station, true, true, false);
        session.Observe(station, false, false, false);
        check(session.Selected == "FineWood", "Release unexpectedly cleared explicit selection.");
        session.Select(null);
        check(session.Selected == null, "Auto did not clear explicit selection.");
        session.Select("Wood");
        session.Observe(null, false, false, false);
        check(session.Selected == null, "Leaving the input did not clear explicit selection.");
        session.Observe(station, true, true, false);
        session.Clear();
        check(!session.CanInsert && session.Context == null, "Lifecycle cleanup retained a live input session.");
    }

    private static void PreferredStorageBeatsOtherInventory(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood,RoundLog|");
        var fine = Item("FineWood", 3);
        var wood = Item("Wood", 2);
        player.Inventory.Items.Add(fine);
        SupplyTestSetup.Chest(1, 0, 0, wood);
        bool handled = SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(handled && result && station.LastInput == "Wood", "Storage priority lost to lower-ranked inventory type.");
        check(fine.m_stack == 3 && wood.m_stack == 1, "Wrong source was consumed.");
    }

    private static void InventoryWinsWithinSelectedType(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|");
        var inventory = Item("Wood", 2);
        inventory.m_cheated = true;
        var stored = Item("Wood", 4);
        player.Inventory.Items.Add(inventory);
        SupplyTestSetup.Chest(1, 0, 0, stored);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(result && inventory.m_stack == 1 && stored.m_stack == 4, "Storage won over identical inventory type.");
        check(station.LastCheated, "Selected item cheated metadata was lost.");
    }

    private static void ExclusionsSuppressVanillaInventoryFallback(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood|FineWood,RoundLog");
        var fine = Item("FineWood", 3);
        player.Inventory.Items.Add(fine);
        check(SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result) && !result,
            "Excluded-only request fell through to vanilla.");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out _);
        check(fine.m_stack == 3 && station.QueueSize == 0, "Excluded inventory was consumed.");
        check(player.Messages.Count == 1, "An exhausted hold repeated its stop message.");
    }

    private static void ExplicitSelectionOverridesExclusion(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|FineWood");
        var fine = Item("FineWood", 2);
        player.Inventory.Items.Add(fine);
        Select(station, "FineWood");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(result && station.LastInput == "FineWood" && fine.m_stack == 1, "Explicit selection did not override auto exclusion.");
    }

    private static void ExactSelectionNeverSubstitutes(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood|");
        var impostor = Item("ModdedWood", 2);
        impostor.m_shared.m_name = "Wood";
        player.Inventory.Items.AddRange(new[] { Item("FineWood", 2), impostor });
        Select(station, "Wood");
        check(SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result) && !result,
            "Unavailable exact choice fell through or succeeded.");
        check(station.QueueSize == 0 && impostor.m_stack == 2, "Same shared-name impostor or another type was substituted.");
    }

    private static void HoldContinuesIntoStorageWithoutChangingType(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood|");
        player.Inventory.Items.Add(Item("Wood", 1));
        var wood = Item("Wood", 1);
        var fine = Item("FineWood", 4);
        SupplyTestSetup.Chest(1, 0, 0, wood, fine);
        SupplyTestSetup.Chest(2, 0, 0, Item("Wood", 1));
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool first);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool second);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool third);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool exhausted);
        check(first && second && third && !exhausted && station.QueueSize == 3,
            "Held transfer failed to continue inventory to/between storage or switched types.");
        check(fine.m_stack == 4, "Hold switched to a different wood after exhaustion.");
        KilnInputSelection.Session.Observe(station, false, false, false);
        KilnInputSelection.Session.Observe(station, true, true, false);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool next);
        check(next && station.LastInput == "FineWood", "New E press did not choose the next available type.");
    }

    private static void FullAndInaccessibleStationsConsumeNothing(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|");
        var wood = Item("Wood", 3);
        player.Inventory.Items.Add(wood);
        station.QueueSize = station.m_maxOre;
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool full);
        check(!full && wood.m_stack == 3, "Full station consumed an item.");
        station.QueueSize = 0;
        Select(station, null);
        PrivateArea.Allowed = false;
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool denied);
        check(!denied && wood.m_stack == 3, "Ward access loss consumed an item.");
        PrivateArea.Allowed = true;
        Select(station, null);
        station.m_nview.Owner = false;
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool remote);
        check(!remote && wood.m_stack == 3, "Non-owner insertion consumed an item.");
    }

    private static void RejectedInventoryTransferRestoresTheSameItem(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|");
        var wood = Item("Wood", 1);
        wood.m_quality = 3;
        player.Inventory.Items.Add(wood);
        station.BeforeAddOre = () => station.m_nview.Owner = false;
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(!result && player.Inventory.ContainsItem(wood) && wood.m_stack == 1 && wood.m_quality == 3,
            "Rejected inventory input was not restored with original identity/metadata.");
        check(station.QueueSize == 0, "Rejected input still entered the queue.");
    }

    private static void SelectedInputSkipsIneligibleStorage(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|");
        var remote = SupplyTestSetup.Chest(1, 0, 0, Item("Wood", 3));
        remote.m_nview.Owner = false;
        var busy = SupplyTestSetup.Chest(2, 0, 0, Item("Wood", 4));
        busy.m_inUse = true;
        var distant = SupplyTestSetup.Chest(21, 0, 0, Item("Wood", 5));
        var valid = SupplyTestSetup.Chest(3, 0, 0, Item("Wood", 6));
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(result && valid.Inventory.Items[0].m_stack == 5, "Eligible locally owned source was not used.");
        check(remote.Inventory.Items[0].m_stack == 3 && busy.Inventory.Items[0].m_stack == 4
            && distant.Inventory.Items[0].m_stack == 5, "Selected transfer consumed remote, busy or out-of-range storage.");
    }

    private static void RejectedStorageTransferRestoresAndUnlocks(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|");
        var wood = Item("Wood", 2);
        var chest = SupplyTestSetup.Chest(1, 0, 0, wood);
        station.BeforeAddOre = () => throw new InvalidOperationException("Modeled rejected callback");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(!result && wood.m_stack == 2 && !chest.m_inUse && station.QueueSize == 0,
            "Failed storage input was lost, committed, or left the container locked.");
    }

    private static void PostCommitExceptionDoesNotDuplicate(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|");
        var wood = Item("Wood", 2);
        player.Inventory.Items.Add(wood);
        station.AfterAddOre = () => throw new InvalidOperationException("Modeled post-commit effect failure");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(result && station.QueueSize == 1 && wood.m_stack == 1, "Post-commit exception restored a committed item.");
    }

    private static void InvalidPreferencesBlockWithoutFallback(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|");
        KilnInputSelection.Preferences = null;
        var wood = Item("Wood", 2);
        player.Inventory.Items.Add(wood);
        check(SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result) && !result,
            "Invalid preferences did not suppress unrestricted vanilla insertion.");
        check(wood.m_stack == 2 && station.QueueSize == 0, "Invalid configuration consumed an input.");
    }

    private static void OtherStationsAndExplicitUseRemainVanilla(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood|Wood");
        var wood = Item("Wood", 2);
        player.Inventory.Items.Add(wood);
        check(!SupplyTransfer.TryHandleSmelterInput(station, player, wood, out _), "Explicit item-use was intercepted.");
        station.m_name = "$piece_smelter";
        check(!SupplyTransfer.TryHandleSmelterInput(station, player, null, out _), "Other station's inventory path was intercepted.");
        station.m_name = "$piece_charcoalkiln";
        SupplyFromStoragePlugin.InputSelectionEnabled = false;
        check(!SupplyTransfer.TryHandleSmelterInput(station, player, null, out _), "Disabled selection changed legacy inventory behavior.");
    }

    private static (Player Player, Smelter Station) Setup(string preferences)
    {
        var player = SupplyTestSetup.Reset();
        var station = new Smelter { m_name = "$piece_charcoalkiln" };

        foreach (string type in new[] { "Wood", "FineWood", "RoundLog" })
        {
            station.m_conversion.Add(new Smelter.ItemConversion { m_from = SupplyTestSetup.StationDrop(type, type) });
        }

        SupplyFromStoragePlugin.InputSelectionEnabled = true;
        KilnInputSelection.Preferences = InputPreferences.Parse(preferences);
        KilnInputSelection.Session.Observe(station, true, true, false);

        return (player, station);
    }

    private static ItemDrop.ItemData Item(string prefabName, int count)
    {
        var item = SupplyTestSetup.Item(prefabName, count);
        item.m_dropPrefab = new GameObject(prefabName);

        return item;
    }

    private static void Select(Smelter station, string type)
    {
        KilnInputSelection.Session.Observe(station, false, false, false);
        KilnInputSelection.Session.Select(type);
        KilnInputSelection.Session.Observe(station, true, true, false);
    }
}