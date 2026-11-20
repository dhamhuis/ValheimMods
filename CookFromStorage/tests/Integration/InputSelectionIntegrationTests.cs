using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimMods.Common.InputSelection;
using ValheimMods.Testing;
using static CookFromStorage.Tests.CookTestSetup;

namespace CookFromStorage.Tests;

internal static class InputSelectionIntegrationTests
{
    internal static void Run(TestRunner runner)
    {
        var harmony = new Harmony("hoose.valheim.cookfromstorage.selection.tests");

        try
        {
            harmony.CreateClassProcessor(typeof(CookingStationPatch)).Patch();
            harmony.CreateClassProcessor(typeof(FermenterPatch)).Patch();
            runner.Run(PickerTargetsIngredientsNotFuelOrFoundry);
            runner.Run(PreviewUsesCurrentDistinctInputsAndExactCounts);
            runner.Run(PriorityAcrossSourcesLatchesUntilRelease);
            runner.Run(InventoryWinsWithinTypeAndPreservesCheatFlag);
            runner.Run(ExclusionsAllowManualOverrideWithoutReplacingSelection);
            runner.Run(NoAvailableInputUsesVanillaMessage);
            runner.Run(UnavailableSelectionDoesNotSubstituteAndUsesLocalizedName);
            runner.Run(CollectionAndNativeHoldGuardsRemainVanilla);
            runner.Run(InvalidScopeBlocksAndDisablingRestoresLegacyBehavior);
            runner.Run(ReadinessFailureStopsUntilRelease);
            runner.Run(TargetAndAccessChangesCannotResumeAHold);
            runner.Run(FermenterPriorityPreservesRunningAndFinishedBatches);
            runner.Run(FermenterShelterRefreshRechecksOwnership);
            runner.Run(RejectedInputsRestoreAndPostCommitErrorsDoNotDuplicate);
            runner.Run(ChangedIncompatibleInputsCannotBypassVanillaRestrictions);
            runner.Run(PreviewShowsOnlyCurrentCharacterDiscoveries);
            runner.Run(AutomaticChoiceRequiresDiscoveryAcrossSources);
            runner.Run(UnknownManualAndLatchedInputsStayBlocked);
        }
        finally
        {
            Reset();
            harmony.UnpatchSelf();
        }
    }

    /// <summary>The picker routes direct racks and food switches but never fuel, Foundry casts or single-input stations.</summary>
    private static void PickerTargetsIngredientsNotFuelOrFoundry(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        Arm(rack, "RawMeat,NeckTail|");
        player.HoverObject = new GameObject { ParentComponents = new Component[] { rack } };
        check(CookingInputPreview.GetStation(player) == rack, "Direct rack did not expose its picker.");
        rack.m_addFoodSwitch = new Switch();
        check(CookingInputPreview.GetStation(player) == null, "Oven body was mistaken for its food switch.");
        player.HoverObject.ParentComponents = new Component[] { rack, rack.m_addFoodSwitch };
        check(CookingInputPreview.GetStation(player) == rack, "Food switch did not expose its picker.");
        player.HoverObject.ParentComponents = new Component[] { rack, new Switch() };
        check(CookingInputPreview.GetStation(player) == null, "Fuel switch opened the ingredient picker.");
        player.HoverObject.ParentComponents = new Component[] { rack, rack.m_addFoodSwitch };
        rack.m_name = "$piece_frostfoundry";
        check(CookingInputPreview.GetStation(player) == null, "Foundry casts acquired an automatic picker.");
        rack.m_name = "$piece_cookingstation";
        rack.m_conversion.RemoveAt(1);
        check(CookingInputPreview.GetStation(player) == null, "A single-input station acquired selection behavior.");

        var brew = Brew();
        player.HoverObject.ParentComponents = new Component[] { brew };
        check(CookingInputPreview.GetStation(player) == brew, "Fermenter did not expose its picker.");
        brew.m_nview.Owner = false;
        check(CookingInputPreview.GetStation(player) == null, "Remote-owned Fermenter exposed editable input.");
    }

    /// <summary>Snapshots deduplicate live conversions and count exact types without changing inventory or remote storage.</summary>
    private static void PreviewUsesCurrentDistinctInputsAndExactCounts(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        rack.m_conversion.Add(rack.m_conversion[0]);
        Arm(rack, "RawMeat,NeckTail|");
        player.HoverObject = new GameObject { ParentComponents = new Component[] { rack } };
        player.Inventory.Items.Add(Item("$item_necktail", "NeckTail", 2));
        player.Inventory.Items.Add(Item("$item_raw_meat", "ImpostorMeat", 7));
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 4));
        var remote = Chest(2, 0, 0, Item("$item_raw_meat", "RawMeat", 20));
        remote.m_nview.Owner = false;
        var snapshot = CookingInputPreview.Read(player);
        check(snapshot.Items.Count == 2 && snapshot.Items[0].Item.name == "RawMeat"
            && snapshot.Items[0].InventoryCount == 0 && snapshot.Items[0].StorageCount == 4
            && snapshot.Items[1].InventoryCount == 2, "Snapshot used shared-name impostors, duplicates or remote storage.");
        check(player.Inventory.Items[0].m_stack == 2 && chest.Inventory.Items[0].m_stack == 4,
            "Reading the snapshot consumed an ingredient.");
        rack.m_conversion.Add(Conversion("ModdedFood", "$item_moddedfood"));
        snapshot = CookingInputPreview.Read(player);
        var defaults = InputPreferences.Parse(CookingInputSelection.DefaultPreferences(rack));
        check(snapshot.Items.Count == 3 && defaults.Choose(new[] { "ModdedFood", "NeckTail", "RawMeat" }) == "RawMeat",
            "Live extra conversions were lost or new defaults ignored conversion order.");
    }

    /// <summary>Priority chooses a type across sources; Auto exhaustion advances to the next priority during the same hold.</summary>
    private static void PriorityAcrossSourcesLatchesUntilRelease(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        rack.m_slots = new Transform[5];
        var neck = Item("$item_necktail", "NeckTail", 2);
        player.Inventory.Items.Add(neck);
        var stored = Item("$item_raw_meat", "RawMeat", 2);
        Chest(1, 0, 0, stored);
        Arm(rack, "RawMeat,NeckTail|");
        check(Use(rack, player) && stored.m_stack == 1 && neck.m_stack == 2,
            "Preferred stored type lost to a different inventory ingredient.");
        check(Use(rack, player) && stored.m_stack == 0, "Repeated callback did not keep its type.");
        check(Use(rack, player) && neck.m_stack == 1, "Auto exhaustion did not advance to the next priority.");
        Arm(rack, "RawMeat,NeckTail|");
        check(Use(rack, player) && neck.m_stack == 0, "Release/repress failed to choose the next available type.");
    }

    /// <summary>Inventory supplies the chosen type before storage and the exact item's cheat flag reaches the RPC.</summary>
    private static void InventoryWinsWithinTypeAndPreservesCheatFlag(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        var carried = Item("$item_raw_meat", "RawMeat", 1);
        carried.m_cheated = true;
        player.Inventory.Items.Add(carried);
        var stored = Item("$item_raw_meat", "RawMeat", 2);
        Chest(1, 0, 0, stored);
        Arm(rack, "RawMeat,NeckTail|");
        check(Use(rack, player) && player.Inventory.Items.Count == 0 && stored.m_stack == 2
            && rack.CheatedInputs[0], "Exact inventory ingredient or its cheat flag was lost.");
        check(Use(rack, player) && stored.m_stack == 1 && !rack.CheatedInputs[1],
            "Latched input did not continue from inventory into storage.");
    }

    /// <summary>Automatic exclusions cover inventory and storage but explicit picker/item use can override them.</summary>
    private static void ExclusionsAllowManualOverrideWithoutReplacingSelection(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        var raw = Item("$item_raw_meat", "RawMeat", 2);
        var neck = Item("$item_necktail", "NeckTail", 1);
        player.Inventory.Items.AddRange(new[] { raw, neck });
        Arm(rack, "|RawMeat,NeckTail");
        check(!Use(rack, player) && raw.m_stack == 2 && neck.m_stack == 1
            && player.LastMessage == "Items set to Never auto-add (F8)",
            "Auto consumed or did not explain an excluded inventory ingredient.");
        Arm(rack, "|RawMeat,NeckTail", "RawMeat");
        check(Use(rack, player) && raw.m_stack == 1, "Explicit picker choice did not override exclusion.");
        check(rack.OnAddFoodSwitch(new Switch(), player, neck) && neck.m_stack == 0
            && CookingInputSelection.Session.Selected == "RawMeat",
            "Explicit item use was blocked or replaced the picker choice.");
    }

    /// <summary>With no accepted ingredient anywhere, stations show their vanilla no-item message.</summary>
    private static void NoAvailableInputUsesVanillaMessage(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        rack.m_noCookableItemsMessage = "$custom_nothing";
        Arm(rack, "RawMeat,NeckTail|");
        check(!Use(rack, player) && player.LastMessage == "$custom_nothing",
            "Empty rack did not show its configured vanilla no-cookable message.");
        var brew = Brew();
        Arm(brew, CookingInputSelection.DefaultPreferences(brew));
        check(!brew.Interact(player, false, false) && player.LastMessage == "$msg_noprocessableitems",
            "Empty Fermenter did not show vanilla's no-processable message.");
    }

    /// <summary>An unavailable explicit choice never substitutes, reports its localized name and stops the current hold.</summary>
    private static void UnavailableSelectionDoesNotSubstituteAndUsesLocalizedName(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        var neck = Item("$item_necktail", "NeckTail", 2);
        player.Inventory.Items.Add(neck);
        Arm(rack, "RawMeat,NeckTail|", "RawMeat");
        check(!Use(rack, player) && neck.m_stack == 2
            && player.LastMessage == "No Boar meat available. Release use before trying again.",
            "Unavailable selection substituted or displayed an internal prefab ID.");
        var raw = Item("$item_raw_meat", "RawMeat", 1);
        player.Inventory.Items.Add(raw);
        check(!Use(rack, player) && raw.m_stack == 1, "Adding ingredients resumed a stopped hold.");
        Arm(rack, "RawMeat,NeckTail|", "RawMeat");
        check(Use(rack, player) && raw.m_stack == 0, "Release/repress did not resume exact selection.");
    }

    /// <summary>Finished output collection takes precedence even with invalid preferences and native held guards remain intact.</summary>
    private static void CollectionAndNativeHoldGuardsRemainVanilla(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        Arm(rack, "|RawMeat,NeckTail");
        CookingInputSelection.Preferences = null;
        rack.DoneItem = true;
        check(Use(rack, player) && !rack.DoneItem && rack.AddedItems.Count == 0,
            "Selection blocked collection or loaded an ingredient while collecting.");
        check(!rack.Interact(player, true, false), "Direct rack held interaction bypassed vanilla.");

        var brew = Brew();
        Arm(brew, "|MeadBaseHealthMinor,MeadBaseStaminaMinor");
        brew.Content = "MeadBaseHealthMinor".GetStableHashCode();
        brew.ReadyForCollection = true;
        check(Use(brew, player) && brew.Collections == 1 && brew.AddedItems == 0,
            "Fermenter collection also loaded another base.");
        check(!brew.Interact(player, true, false) && brew.AddedItems == 0,
            "Selection added held-repeat support to the Fermenter.");
    }

    /// <summary>Stale or unavailable preferences block controlled insertion; disabling selection/automation restores legacy inventory use.</summary>
    private static void InvalidScopeBlocksAndDisablingRestoresLegacyBehavior(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        var neck = Item("$item_necktail", "NeckTail", 3);
        player.Inventory.Items.Add(neck);
        Arm(rack, "RawMeat,NeckTail|");
        CookingInputSelection.PreferenceStationType = "$piece_fermenter";
        check(!Use(rack, player) && neck.m_stack == 3, "Stale station preferences permitted insertion.");
        Arm(rack, "RawMeat,NeckTail|");
        CookingInputSelection.Preferences = null;
        check(!Use(rack, player) && neck.m_stack == 3, "Invalid preferences fell back to unrestricted vanilla selection.");
        CookFromStoragePlugin.InputSelectionEnabled = false;
        check(Use(rack, player) && neck.m_stack == 2, "Disabling selection broke legacy inventory use.");
        CookFromStoragePlugin.InputSelectionEnabled = true;
        CookFromStoragePlugin.AutomaticIngredientsEnabled = false;
        check(Use(rack, player) && neck.m_stack == 1, "Disabling automation left controlled insertion active.");
    }

    /// <summary>Missing fire/fullness consumes nothing and does not restart a stopped hold when the condition clears.</summary>
    private static void ReadinessFailureStopsUntilRelease(Action<bool, string> check)
    {
        foreach (bool full in new[] { false, true })
        {
            var player = Reset();
            var rack = Rack();
            rack.m_requireFire = true;
            rack.FireLit = full;

            if (full)
            {
                rack.AddedItems.AddRange(new[] { "RawMeat", "RawMeat" });
            }

            var item = Item("$item_raw_meat", "RawMeat", 2);
            player.Inventory.Items.Add(item);
            Arm(rack, "RawMeat,NeckTail|");
            check(!Use(rack, player) && item.m_stack == 2
                && player.LastMessage == (full ? "$msg_nocookroom" : "$msg_needfire"),
                "Unready station consumed input or lost native readiness feedback.");
            rack.FireLit = true;
            rack.AddedItems.Clear();
            check(!Use(rack, player) && item.m_stack == 2, "A repaired readiness condition restarted the hold.");
            Arm(rack, "RawMeat,NeckTail|");
            check(Use(rack, player) && item.m_stack == 1, "Release/repress failed after restoring readiness.");
        }
    }

    /// <summary>Holds survive aiming away, restart with Auto on another station, and access changes still stop without fallback.</summary>
    private static void TargetAndAccessChangesCannotResumeAHold(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        rack.m_slots = new Transform[5];
        var other = Rack();
        other.m_slots = new Transform[5];
        var raw = Item("$item_raw_meat", "RawMeat", 3);
        var neck = Item("$item_necktail", "NeckTail", 5);
        var chest = Chest(1, 0, 0, raw, neck);
        Arm(rack, "RawMeat,NeckTail|", "NeckTail");
        CookingInputSelection.Session.Observe(null, true, false, false);
        check(!Use(rack, player) && neck.m_stack == 5, "A hold inserted while aiming at nothing.");
        CookingInputSelection.Session.Observe(rack, true, false, false);
        check(Use(rack, player) && neck.m_stack == 4 && CookingInputSelection.Session.Selected == "NeckTail"
            && !player.Messages.Any(message => message.Contains("Release")),
            "Aiming away and back stopped the hold, lost its exact choice or showed a restart message.");
        CookingInputSelection.PreferenceStationType = CookingInputSelection.StationType(other);
        CookingInputSelection.Session.Observe(other, true, false, false);
        check(CookingInputSelection.Session.Selected == null && Use(other, player) && raw.m_stack == 2 && neck.m_stack == 4,
            "Another station did not restart the hold with its Auto choice.");
        Arm(other, "RawMeat,NeckTail|", "RawMeat");
        chest.m_nview.Owner = false;
        check(!Use(other, player) && raw.m_stack == 2, "Remote-owned source was consumed.");
        chest.m_nview.Owner = true;
        Arm(other, "RawMeat,NeckTail|", "RawMeat");
        PrivateArea.Allowed = false;
        check(!Use(other, player) && raw.m_stack == 2, "Ward-denied station consumed storage.");
        PrivateArea.Allowed = true;
        check(!Use(other, player) && raw.m_stack == 2, "Restoring access resumed a stopped hold.");
    }

    /// <summary>Fermenter Auto chooses a preferred stored base over carried alternatives without touching running batches.</summary>
    private static void FermenterPriorityPreservesRunningAndFinishedBatches(Action<bool, string> check)
    {
        var player = Reset();
        var brew = Brew();
        var health = Item("$item_healthbase", "MeadBaseHealthMinor", 2);
        player.Inventory.Items.Add(health);
        var stamina = Item("$item_staminabase", "MeadBaseStaminaMinor", 2);
        Chest(1, 0, 0, stamina);
        Arm(brew, "MeadBaseStaminaMinor,MeadBaseHealthMinor|MeadBaseHealthMinor");
        check(Use(brew, player) && stamina.m_stack == 1 && health.m_stack == 2
            && brew.Content == "MeadBaseStaminaMinor".GetStableHashCode(),
            "Fermenter did not choose the preferred stored base.");
        check(!Use(brew, player) && stamina.m_stack == 1, "A running batch accepted another base.");
        brew.ReadyForCollection = true;
        check(Use(brew, player) && brew.Collections == 1 && stamina.m_stack == 1,
            "Finished-batch collection also loaded a base.");
        health.m_cheated = true;
        Arm(brew, "|MeadBaseHealthMinor,MeadBaseStaminaMinor", "MeadBaseHealthMinor");
        check(Use(brew, player) && health.m_stack == 1 && brew.CheatedInput,
            "Explicit excluded inventory base or its cheat flag was lost.");
    }

    /// <summary>Shelter is refreshed before controlled brewing, and refreshed ownership is checked again before removing a base.</summary>
    private static void FermenterShelterRefreshRechecksOwnership(Action<bool, string> check)
    {
        var player = Reset();
        var brew = Brew();
        var item = Item("$item_healthbase", "MeadBaseHealthMinor", 2);
        player.Inventory.Items.Add(item);
        Arm(brew, "MeadBaseHealthMinor,MeadBaseStaminaMinor|");
        brew.OnUpdateCover = () => brew.m_hasRoof = false;
        check(!Use(brew, player) && item.m_stack == 2 && player.LastMessage == "$piece_fermenter_needroof",
            "Missing roof consumed a selected base or lost the roof message.");
        Arm(brew, "MeadBaseHealthMinor,MeadBaseStaminaMinor|");
        brew.OnUpdateCover = () =>
        {
            brew.m_hasRoof = true;
            brew.m_nview.Owner = false;
        };
        check(!Use(brew, player) && item.m_stack == 2 && brew.AddedItems == 0,
            "Ownership change during cover refresh consumed a base.");
    }

    /// <summary>Both source kinds restore rejected cooking/brewing inputs and never duplicate after a confirmed commit throws.</summary>
    private static void RejectedInputsRestoreAndPostCommitErrorsDoNotDuplicate(Action<bool, string> check)
    {
        foreach (bool brewing in new[] { false, true })
        {
            foreach (bool stored in new[] { false, true })
            {
                var player = Reset();
                Component station = brewing ? Brew() : Rack();
                string type = brewing ? "MeadBaseHealthMinor" : "RawMeat";
                var item = Item(brewing ? "$item_healthbase" : "$item_raw_meat", type, 1);
                var chest = stored ? Chest(1, 0, 0, item) : null;

                if (!stored)
                {
                    player.Inventory.Items.Add(item);
                }

                if (station is CookingStation rack)
                {
                    rack.RejectInput = true;
                }
                else if (station is Fermenter brew)
                {
                    brew.RejectInput = true;
                }

                Arm(station, CookingInputSelection.DefaultPreferences(station));
                check(!Use(station, player) && item.m_stack == 1 && (chest == null || !chest.m_inUse),
                    $"Rejected {(brewing ? "brewing" : "cooking")} input was not restored/unreserved.");

                if (station is CookingStation retryRack)
                {
                    retryRack.RejectInput = false;
                    retryRack.ThrowAfterInsertion = true;
                }
                else if (station is Fermenter retryBrew)
                {
                    retryBrew.RejectInput = false;
                    retryBrew.ThrowAfterInsertion = true;
                }

                Arm(station, CookingInputSelection.DefaultPreferences(station));
                check(Use(station, player) && item.m_stack == 0 && (chest == null || !chest.m_inUse),
                    "Confirmed post-commit exception restored a duplicate or retained a reservation.");
            }
        }
    }

    /// <summary>A newly incompatible shared-name input disappears from the picker and a stale choice cannot bypass CookItem's guard.</summary>
    private static void ChangedIncompatibleInputsCannotBypassVanillaRestrictions(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        rack.m_conversion.Add(Conversion("ModdedFood", "$item_moddedfood"));
        var raw = Item("$item_raw_meat", "RawMeat", 2);
        player.Inventory.Items.Add(raw);
        Arm(rack, "RawMeat,NeckTail,ModdedFood|", "RawMeat");
        rack.m_incompatibleItems.Add(new CookingStation.ItemMessage
        {
            m_item = rack.m_conversion[0].m_from,
            m_message = "$msg_incompatible"
        });
        check(CookingInputSelection.Inputs(rack).Count == 2,
            "The picker offered an input forbidden by the station's incompatible-item rules.");
        rack.m_conversion.RemoveAt(2);
        check(!Use(rack, player) && raw.m_stack == 2 && rack.AddedItems.Count == 0,
            "A stale choice fell through when changed inputs left only one valid type.");
    }

    private static CookingStation Rack()
    {
        return Station(Conversion("RawMeat", "$item_raw_meat"), Conversion("NeckTail", "$item_necktail"));
    }

    private static Fermenter Brew()
    {
        var station = new Fermenter();
        station.m_conversion.Add(new Fermenter.ItemConversion { m_from = Conversion("MeadBaseHealthMinor", "$item_healthbase").m_from });
        station.m_conversion.Add(new Fermenter.ItemConversion { m_from = Conversion("MeadBaseStaminaMinor", "$item_staminabase").m_from });

        return station;
    }

    private static void Arm(Component station, string preferences, string selected = null)
    {
        CookFromStoragePlugin.InputSelectionEnabled = true;
        CookingInputSelection.Preferences = InputPreferences.Parse(preferences);
        CookingInputSelection.PreferenceStationType = CookingInputSelection.StationType(station);
        CookingInputSelection.Session.Observe(station, false, false, false);
        CookingInputSelection.Session.Select(selected);
        CookingInputSelection.Session.Observe(station, true, true, false);
    }

    private static bool Use(Component station, Player player)
    {
        return station switch
        {
            CookingStation rack => rack.Interact(player, false, false),
            Fermenter brew => brew.Interact(player, false, false),
            _ => throw new ArgumentException("Expected a cooking station or Fermenter.", nameof(station))
        };
    }

    private static void PreviewShowsOnlyCurrentCharacterDiscoveries(Action<bool, string> check)
    {
        foreach (bool brewing in new[] { false, true })
        {
            var player = Reset();
            Component station = brewing ? Brew() : Rack();
            var inputs = CookingInputSelection.Inputs(station);
            var unknownInput = inputs[0];
            var knownInput = inputs[1];
            player.KnownMaterials.Clear();
            Arm(station, CookingInputSelection.DefaultPreferences(station));
            player.HoverObject = new GameObject { ParentComponents = new[] { station } };
            var unknown = Item(unknownInput.m_itemData.m_shared.m_name, unknownInput.name, 3);
            Chest(1, 0, 0, unknown);
            var carried = Item(unknown.m_shared.m_name, unknownInput.name, 2);
            player.Inventory.Items.Add(carried);
            check(CookingInputPreview.Read(player).Items.Count == 0,
                "Undiscovered cooking/brewing inputs leaked through inventory/storage availability.");
            player.KnownMaterials.Add(knownInput.m_itemData.m_shared.m_name);
            var snapshot = CookingInputPreview.Read(player);
            check(snapshot.Items.Count == 1 && snapshot.Items[0].Item == knownInput && !snapshot.Items[0].IsAvailable
                && CookingInputPreview.GetStation(player) == station,
                "One discovered zero-count input was hidden or disabled the multi-input station's selection.");
            player.KnownMaterials.Add(unknownInput.name);
            check(CookingInputPreview.Read(player).Items.Count == 1, "Discovery used prefab IDs rather than shared item names.");
            var preferences = CookingInputSelection.Preferences.ToggleExcluded(unknownInput.name);
            player.KnownMaterials.Add(unknownInput.m_itemData.m_shared.m_name);
            snapshot = CookingInputPreview.Read(player);
            check(snapshot.Items.Select(entry => entry.Item.name).SequenceEqual(inputs.Select(input => input.name))
                && snapshot.Items[0].InventoryCount == 2 && snapshot.Items[0].StorageCount == 3
                && preferences.IsExcluded(unknownInput.name), "New discovery lost display order, exact counts or saved exclusion.");
            var other = new Player { HoverObject = player.HoverObject };
            check(CookingInputPreview.Read(other).Items.Count == 0 && unknown.m_stack == 3 && carried.m_stack == 2,
                "Discoveries leaked between characters or preview reading consumed an input.");
        }
    }

    private static void AutomaticChoiceRequiresDiscoveryAcrossSources(Action<bool, string> check)
    {
        foreach (bool brewing in new[] { false, true })
        {
            var player = Reset();
            Component station = brewing ? Brew() : Rack();
            var inputs = CookingInputSelection.Inputs(station);
            var unknown = Item(inputs[0].m_itemData.m_shared.m_name, inputs[0].name, 2);
            var storedUnknown = Item(inputs[0].m_itemData.m_shared.m_name, inputs[0].name, 3);
            var known = Item(inputs[1].m_itemData.m_shared.m_name, inputs[1].name, 1);
            player.Inventory.Items.AddRange(new[] { unknown, known });
            Chest(1, 0, 0, storedUnknown);
            player.KnownMaterials.Remove(unknown.m_shared.m_name);
            string saved = CookingInputSelection.DefaultPreferences(station);
            Arm(station, saved);
            check(Use(station, player) && known.m_stack == 0 && unknown.m_stack == 2 && storedUnknown.m_stack == 3,
                "Cooking/brewing Auto chose an undiscovered carried/stored type over a known type.");

            if (station is Fermenter brew)
            {
                brew.Content = 0;
            }

            Arm(station, saved);
            check(!Use(station, player) && unknown.m_stack == 2 && storedUnknown.m_stack == 3,
                "Unknown-only cooking/brewing inputs fell through to vanilla.");
            player.KnownMaterials.Add(unknown.m_shared.m_name);
            check(!Use(station, player) && unknown.m_stack == 2, "Discovery resumed a stopped cooking/brewing hold.");
            Arm(station, saved);
            check(Use(station, player) && unknown.m_stack == 1 && storedUnknown.m_stack == 3
                && CookingInputSelection.Preferences.Serialize() == saved,
                "Discovery did not enable the saved preference with inventory-first within type.");
        }
    }

    private static void UnknownManualAndLatchedInputsStayBlocked(Action<bool, string> check)
    {
        var player = Reset();
        var rack = Rack();
        var raw = Item("$item_raw_meat", "RawMeat", 3);
        var neck = Item("$item_necktail", "NeckTail", 2);
        player.Inventory.Items.AddRange(new[] { raw, neck });
        player.KnownMaterials.Remove(raw.m_shared.m_name);
        Arm(rack, "RawMeat,NeckTail|", "RawMeat");
        check(!Use(rack, player) && raw.m_stack == 3 && neck.m_stack == 2
            && !player.LastMessage.Contains("RawMeat"), "A stale undiscovered manual choice was consumed or named.");
        check(rack.OnAddFoodSwitch(new Switch(), player, raw) && raw.m_stack == 2,
            "Discovery filtering intercepted explicit vanilla item use.");
        rack.AddedItems.Clear();
        player.KnownMaterials.Add(raw.m_shared.m_name);
        Arm(rack, "RawMeat,NeckTail|");
        check(Use(rack, player) && raw.m_stack == 1, "Known input failed to establish a latch.");
        player.KnownMaterials.Remove(raw.m_shared.m_name);
        rack.AddedItems.Clear();
        check(Use(rack, player) && raw.m_stack == 1 && neck.m_stack == 1,
            "An automatic hold consumed an undiscovered type instead of advancing to the next discovered one.");
    }
}