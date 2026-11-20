using System;
using System.Linq;
using UnityEngine;
using ValheimMods.Common.InputSelection;
using ValheimMods.Testing;

namespace SupplyFromStorage.Tests;

internal static class InputSelectionTests
{
    private static string _stationType;

    internal static void Run(TestRunner runner)
    {
        runner.Run(PreferencesRoundTripAndPreserveUnknownTypes);
        runner.Run(InvalidPreferencesAreRejected);
        runner.Run(UnrankedTypesHaveStableOrder);
        runner.Run(PreferenceEditingPreservesOtherRules);
        runner.Run(PriorityMovesOneStepAndPreservesUnknownTypes);
        runner.Run(HoldLatchesUntilRelease);
        runner.Run(TargetChangesAndMenusStopHeldInput);
        runner.Run(SelectionIsEphemeralButSurvivesRelease);

        foreach (string stationType in new[] { "$piece_charcoalkiln", "$piece_smelter", "$piece_blastfurnace", "$tool_batteringram" })
        {
            _stationType = stationType;
            runner.Run(PreferredStorageBeatsOtherInventory);
            runner.Run(InventoryWinsWithinSelectedType);
            runner.Run(ExclusionsSuppressVanillaInventoryFallback);
            runner.Run(NoAvailableInputUsesVanillaMessage);
            runner.Run(ExplicitSelectionOverridesExclusion);
            runner.Run(ExactSelectionNeverSubstitutes);
            runner.Run(HoldContinuesIntoStorageThenNextPriority);
            runner.Run(HoldSurvivesAimingAwayAndBack);
            runner.Run(FullAndInaccessibleStationsConsumeNothing);
            runner.Run(SelectedInputSkipsIneligibleStorage);
            runner.Run(RejectedInventoryTransferRestoresTheSameItem);
            runner.Run(RejectedStorageTransferRestoresAndUnlocks);
            runner.Run(PostCommitExceptionDoesNotDuplicate);
            runner.Run(InvalidPreferencesBlockWithoutFallback);
            runner.Run(OtherStationsAndExplicitUseRemainVanilla);
            runner.Run(AutomaticChoiceRequiresDiscoveryAcrossSources);
            runner.Run(UnknownManualAndLatchedInputsStayBlocked);
        }

        runner.Run(UnavailableSelectedInputUsesLocalizedDisplayName);
        runner.Run(StationPreferenceScopesAreIndependent);
        runner.Run(FreshDefaultsChooseAvailableInputsInRequestedPriority);
        runner.Run(CrossStationHoldRestartsWithAutomaticChoice);
        runner.Run(UnsupportedStationsRemainLegacy);
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

    private static void PriorityMovesOneStepAndPreservesUnknownTypes(Action<bool, string> check)
    {
        string[] accepted = { "Wood", "FineWood", "RoundLog" };
        var preferences = InputPreferences.Parse("MissingType,Wood,FineWood|FineWood");
        var movedDown = preferences.MoveDown("Wood", accepted);
        check(movedDown.Order(accepted).SequenceEqual(new[] { "FineWood", "Wood", "RoundLog" }),
            "Moving down did not change precedence by one accepted input.");
        check(movedDown.Serialize() == "MissingType,FineWood,Wood,RoundLog|FineWood",
            "Moving down did not preserve unknown preferences and exclusions.");

        var movedUp = movedDown.MoveUp("RoundLog", accepted);
        check(movedUp.Order(accepted).SequenceEqual(new[] { "FineWood", "RoundLog", "Wood" }),
            "Moving up did not change precedence by one accepted input.");
        check(preferences.MoveUp("Wood", accepted).Serialize() == preferences.Serialize()
            && preferences.MoveDown("RoundLog", accepted).Serialize() == preferences.Serialize(),
            "Moving an input beyond either end changed the preference.");
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
        check(player.Messages.Count == 1 && player.LastMessage == "Items set to Never auto-add (F8)",
            "An exhausted hold repeated or lost its exclusion message.");
    }

    private static void NoAvailableInputUsesVanillaMessage(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood|");
        check(SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result) && !result,
            "An empty request fell through after selection had resolved it.");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out _);
        check(player.Messages.Count == 1 && player.LastMessage == "$msg_noprocessableitems",
            "Nothing to add did not show vanilla's no-processable-items message once.");
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

    private static void HoldContinuesIntoStorageThenNextPriority(Action<bool, string> check)
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
        check(first && second && third && station.QueueSize == 3 && station.LastInput == "Wood" && fine.m_stack == 4,
            "Held transfer failed to continue inventory to/between storage or switched types early.");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool next);
        check(next && station.LastInput == "FineWood" && fine.m_stack == 3,
            "Automatic hold did not advance to the next priority after exhaustion.");
        player.Inventory.Items.Add(Item("Wood", 1));
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool latched);
        check(latched && station.LastInput == "FineWood", "Automatic hold switched back while its type remained available.");
        ProcessorInputSelection.Session.Observe(station, false, false, false);
        ProcessorInputSelection.Session.Observe(station, true, true, false);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool repress);
        check(repress && station.LastInput == "Wood", "New E press did not restart from the top priority.");
    }

    private static void HoldSurvivesAimingAwayAndBack(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood|");
        var fine = Item("FineWood", 3);
        player.Inventory.Items.Add(fine);
        Select(station, "FineWood");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool first);
        ProcessorInputSelection.Session.Observe(null, true, false, false);
        check(!ProcessorInputSelection.Session.CanInsert, "Hold inserted while aiming at nothing.");
        ProcessorInputSelection.Session.Observe(station, true, false, false);
        player.Inventory.Items.Add(Item("Wood", 2));
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool resumed);
        check(first && resumed && fine.m_stack == 1 && station.LastInput == "FineWood"
            && ProcessorInputSelection.Session.Selected == "FineWood"
            && !player.Messages.Any(message => message.Contains("Release")),
            "Aiming away and back stopped the hold, lost the exact choice, or showed a restart message.");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out _);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool exhausted);
        check(!exhausted && station.LastInput == "FineWood"
            && player.Messages.Count(message => message.Contains("Release")) == 1,
            "Exact choice substituted another type after exhaustion.");
        ProcessorInputSelection.Session.Observe(station, true, false, true);
        check(!ProcessorInputSelection.Session.CanInsert, "Opening a menu did not stop the hold.");
        ProcessorInputSelection.Session.Observe(null, true, false, false);
        ProcessorInputSelection.Session.Observe(null, false, false, false);
        ProcessorInputSelection.Session.Observe(station, false, false, false);
        check(ProcessorInputSelection.Session.Selected == null, "Releasing away from the station retained its exact choice.");
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
        ProcessorInputSelection.Preferences = null;
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
        player.KnownMaterials.Clear();
        check(!SupplyTransfer.TryHandleSmelterInput(station, player, wood, out _), "Explicit item-use was intercepted.");
        station.m_name = "$piece_windmill";
        check(!SupplyTransfer.TryHandleSmelterInput(station, player, null, out _), "Other station's inventory path was intercepted.");
        station.m_name = _stationType;
        SupplyFromStoragePlugin.InputSelectionEnabled = false;
        check(!SupplyTransfer.TryHandleSmelterInput(station, player, null, out _), "Disabled selection changed legacy inventory behavior.");
    }

    private static (Player Player, Smelter Station) Setup(string preferences)
    {
        var player = SupplyTestSetup.Reset();
        var station = new Smelter { m_name = _stationType };

        foreach (string type in new[] { "Wood", "FineWood", "RoundLog" })
        {
            station.m_conversion.Add(new Smelter.ItemConversion { m_from = SupplyTestSetup.StationDrop(type, type) });
        }

        SupplyFromStoragePlugin.InputSelectionEnabled = true;
        ProcessorInputSelection.Preferences = InputPreferences.Parse(preferences);
        ProcessorInputSelection.PreferenceStationType = _stationType;
        ProcessorInputSelection.Session.Observe(station, true, true, false);

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
        ProcessorInputSelection.Session.Observe(station, false, false, false);
        ProcessorInputSelection.Session.Select(type);
        ProcessorInputSelection.Session.Observe(station, true, true, false);
    }

    private static void StationPreferenceScopesAreIndependent(Action<bool, string> check)
    {
        check(ProcessorInputSelection.PreferenceSection("$piece_charcoalkiln") == "Charcoal Kiln preferences",
            "Existing kiln config section changed.");
        check(ProcessorInputSelection.PreferenceSection("$tool_batteringram") == "Battering Ram preferences",
            "Ram preferences do not have their own named section.");
        check(new[] { "$piece_charcoalkiln", "$piece_smelter", "$piece_blastfurnace", "$tool_batteringram" }
            .Select(ProcessorInputSelection.PreferenceSection).Distinct().Count() == 4,
            "Two station types share the same preference section.");
        var (player, station) = Setup("Wood|");
        var wood = Item("Wood", 2);
        player.Inventory.Items.Add(wood);
        ProcessorInputSelection.PreferenceStationType = "$piece_charcoalkiln";
        station.m_name = "$piece_smelter";
        check(SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result) && !result
            && wood.m_stack == 2, "A stale preference scope allowed cross-station insertion.");
    }

    /// <summary>Fresh defaults choose each available type in station priority order, with no automatic exclusions.</summary>
    private static void FreshDefaultsChooseAvailableInputsInRequestedPriority(Action<bool, string> check)
    {
        var cases = new[]
        {
            (Station: "$piece_smelter", Priority: new[] { "CopperOre", "CopperScrap", "TinOre", "BronzeScrap", "IronOre", "IronScrap", "SilverOre" }),
            (Station: "$piece_blastfurnace", Priority: new[] { "BlackMetalScrap", "FlametalOreNew", "GoldOre" }),
            (Station: "$piece_charcoalkiln", Priority: new[] { "Wood", "FineWood", "RoundLog" }),
            (Station: "$tool_batteringram", Priority: new[] { "Wood", "FineWood", "RoundLog", "Blackwood" })
        };

        foreach (var scenario in cases)
        {
            var preferences = InputPreferences.Parse(ProcessorInputSelection.DefaultPreferences(scenario.Station));
            var available = scenario.Priority.Reverse().Concat(new[] { "ModdedInput" }).ToList();

            foreach (string expected in scenario.Priority)
            {
                check(preferences.Choose(available) == expected,
                    $"Fresh {scenario.Station} defaults did not prefer {expected} over remaining inputs.");
                available.Remove(expected);
            }

            check(preferences.Choose(available) == "ModdedInput",
                $"Fresh {scenario.Station} defaults blocked an additional runtime input.");
            check(scenario.Priority.All(type => !preferences.IsExcluded(type)),
                $"Fresh {scenario.Station} defaults excluded a supported input.");
        }
    }

    private static void UnavailableSelectedInputUsesLocalizedDisplayName(Action<bool, string> check)
    {
        _stationType = "$tool_batteringram";
        var (player, station) = Setup("FineWood|");
        station.m_conversion.Find(conversion => conversion.m_from.name == "FineWood")
            .m_from.m_itemData.m_shared.m_name = "$item_finewood";
        Select(station, "FineWood");

        bool handled = SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool result);
        check(handled && !result && player.Messages.Count == 1
            && player.Messages[0] == "No Finewood available. Release use before trying again.",
            "Unavailable-input feedback exposed the prefab ID instead of the localized item name.");
    }

    private static void CrossStationHoldRestartsWithAutomaticChoice(Action<bool, string> check)
    {
        _stationType = "$piece_smelter";
        var (player, station) = Setup("Wood|");
        player.Inventory.Items.Add(Item("Wood", 3));
        Select(station, "Wood");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool first);
        station.QueueSize = station.m_maxOre;
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool full);
        var furnace = new Smelter { m_name = "$piece_blastfurnace", m_conversion = station.m_conversion };
        ProcessorInputSelection.PreferenceStationType = furnace.m_name;
        ProcessorInputSelection.Session.Observe(null, true, false, false);
        ProcessorInputSelection.Session.Observe(furnace, true, false, false);
        check(ProcessorInputSelection.Session.Selected == null, "Exact selection followed the hold to another station.");
        SupplyTransfer.TryHandleSmelterInput(furnace, player, null, out bool held);
        check(first && !full && held && furnace.QueueSize == 1 && furnace.LastInput == "Wood",
            "A continuing hold did not restart on the newly aimed station.");
    }

    private static void UnsupportedStationsRemainLegacy(Action<bool, string> check)
    {
        foreach (string name in new[] { "$piece_eitrrefinery", "$piece_windmill", "$piece_spinningwheel", "$modded_smelter" })
        {
            check(!ProcessorInputSelection.Supports(name), "Selection expanded to an unsupported station: " + name);
        }
    }

    private static void AutomaticChoiceRequiresDiscoveryAcrossSources(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood,RoundLog|");
        player.KnownMaterials.Clear();
        player.KnownMaterials.Add("RoundLog");
        var wood = Item("Wood", 2);
        var fine = Item("FineWood", 2);
        var known = Item("RoundLog", 1);
        player.Inventory.Items.AddRange(new[] { fine, known });
        SupplyTestSetup.Chest(1, 0, 0, wood);
        string saved = ProcessorInputSelection.Preferences.Serialize();
        check(SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool inserted) && inserted
            && station.LastInput == "RoundLog" && known.m_stack == 0 && wood.m_stack == 2 && fine.m_stack == 2,
            "Auto preferred an undiscovered inventory/storage input over a discovered type.");
        Select(station, null);
        check(SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool blocked) && !blocked
            && station.QueueSize == 1 && wood.m_stack == 2 && fine.m_stack == 2,
            "Unknown-only input fell through to vanilla or consumed storage.");
        player.KnownMaterials.Add("Wood");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool resumed);
        check(!resumed && wood.m_stack == 2, "Discovering an input restarted a stopped hold.");
        Select(station, null);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool discovered);
        check(discovered && wood.m_stack == 1 && ProcessorInputSelection.Preferences.Serialize() == saved,
            "Discovery failed to restore saved priority after release/repress.");
    }

    private static void UnknownManualAndLatchedInputsStayBlocked(Action<bool, string> check)
    {
        var (player, station) = Setup("Wood,FineWood|");
        var wood = Item("Wood", 3);
        var fine = Item("FineWood", 2);
        player.Inventory.Items.AddRange(new[] { wood, fine });
        player.KnownMaterials.Remove("Wood");
        Select(station, "Wood");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool manual);
        check(!manual && wood.m_stack == 3 && fine.m_stack == 2
            && !player.LastMessage.Contains("Wood"), "Stale manual choice consumed or named an undiscovered input.");
        player.KnownMaterials.Add("Wood");
        Select(station, null);
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool first);
        player.KnownMaterials.Remove("Wood");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool latched);
        check(first && latched && wood.m_stack == 2 && fine.m_stack == 1 && station.LastInput == "FineWood",
            "An automatic hold bypassed discovery instead of advancing to the next discovered type.");
        player.KnownMaterials.Add("Wood");
        SupplyTransfer.TryHandleSmelterInput(station, player, null, out bool resumed);
        check(resumed && wood.m_stack == 2 && fine.m_stack == 0, "An automatic hold switched back while its type remained available.");
    }
}