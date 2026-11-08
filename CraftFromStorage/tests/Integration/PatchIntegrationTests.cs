using System;
using UnityEngine;
using ValheimMods.Testing;
using static CraftFromStorage.Tests.CraftingTestSetup;

namespace CraftFromStorage.Tests;

internal static class PatchIntegrationTests
{
    internal static void Run(TestRunner runner)
    {
        try
        {
            runner.Run(RequirementCheckIncludesStorage);
            runner.Run(DiscoveryDoesNotLearnFromStorage);
            runner.Run(RequirementDisplayIncludesStorage);
            runner.Run(BuildingDisplayExcludesStorage);
            runner.Run(RequirementExceptionRestoresContext);
            runner.Run(AlternativeSelectionFindsStorage);
            runner.Run(TransactionRedirectsIngredientRemoval);
            runner.Run(PlayerNoCostBypassesStorage);
            runner.Run(WorldNoCostBypassesStorage);
            runner.Run(TransactionExceptionReleasesState);
            runner.Run(PendingOwnershipBlocksOtherActions);
            runner.Run(TimeoutReleasesChest);
            runner.Run(RetryCompletesWithoutOldReply);
            runner.Run(DuplicateCraftReplyDoesNotOpenChest);
            runner.Run(HeldInteractionDoesNotAuthorizeReply);
            runner.Run(ManualOpeningAcceptsOnlyOneReply);
            runner.Run(ExpiredManualIntentSuppressesReply);
            runner.Run(ManualOpeningRechecksRange);
            runner.Run(ManualOpeningRechecksPersonalAccess);
            runner.Run(ManualOpeningRechecksPlayerDeath);
            runner.Run(ManualOpeningRechecksWardAccess);
            runner.Run(ManualOpeningRequiresOwnership);
            runner.Run(LostManualReplyDoesNotLockChest);
            runner.Run(DestructionRemovesResponseGuards);
            runner.Run(UntouchedChestKeepsVanillaResponse);
            runner.Run(CancelPatchReleasesPendingRequest);
        }
        finally
        {
            Reset();
        }
    }

    /// <summary>The requirement prefix and count postfix combine eligible storage ingredients.</summary>
    private static void RequirementCheckIncludesStorage(Action<bool, string> check)
    {
        var (player, _, recipe, _) = Setup();
        check(player.HaveRequirementItems(recipe, false, 1, 1), "Requirement check ignored storage.");
    }

    /// <summary>Recipe discovery must still require knowledge from the player's own inventory.</summary>
    private static void DiscoveryDoesNotLearnFromStorage(Action<bool, string> check)
    {
        var (player, _, recipe, _) = Setup();
        check(!player.HaveRequirementItems(recipe, true, 1, 1), "Discovery learned from storage.");
    }

    /// <summary>Crafting requirement display includes storage without leaking inventory-count scope.</summary>
    private static void RequirementDisplayIncludesStorage(Action<bool, string> check)
    {
        var (player, _, recipe, _) = Setup();
        check(
            InventoryGui.SetupRequirement(new Transform(), recipe.m_resources[0], player, true, 1, 1),
            "Crafting display ignored storage.");
        check(
            player.Inventory.CountItems("wood", -1, true) == 0 && CraftingContext.Current == null,
            "Display finalizer leaked storage counts.");
    }

    /// <summary>Building requirement display remains limited to the player's inventory.</summary>
    private static void BuildingDisplayExcludesStorage(Action<bool, string> check)
    {
        var (player, _, recipe, _) = Setup();
        check(
            !InventoryGui.SetupRequirement(new Transform(), recipe.m_resources[0], player, false, 1, 1),
            "Building display counted storage.");
    }

    /// <summary>The requirement finalizer restores scope even when the original method throws.</summary>
    private static void RequirementExceptionRestoresContext(Action<bool, string> check)
    {
        var (player, _, recipe, _) = Setup();
        player.OnRequirementCheck = () => throw new InvalidOperationException("Simulated requirement failure.");
        bool failed = false;

        try
        {
            player.HaveRequirementItems(recipe, false, 1, 1);
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }

        check(failed && CraftingContext.Current == null, "Requirement exception was lost or leaked scope.");
    }

    /// <summary>The alternative-ingredient prefix selects an eligible stack from storage.</summary>
    private static void AlternativeSelectionFindsStorage(Action<bool, string> check)
    {
        var (player, _, recipe, _) = Setup();
        recipe.m_requireOnlyOneIngredient = true;
        check(
            player.GetFirstRequiredItem(player.Inventory, recipe, 1, out int amount, out _, 1) != null && amount == 2,
            "Alternative ingredient was not selected from storage.");
    }

    /// <summary>A patched transaction creates output, consumes storage, and raises its change callback.</summary>
    private static void TransactionRedirectsIngredientRemoval(Action<bool, string> check)
    {
        var (player, chest, recipe, gui) = Setup();
        gui.OnCraft = current =>
        {
            check(current.HaveRequirementItems(recipe, false, 1, 1), "Reserved chest was unavailable to requirements.");
            current.Inventory.Items.Add(Item("output", 1));
            current.Inventory.RemoveItem("wood", 2, -1, true);
        };

        gui.DoCrafting(player);
        check(
            player.Inventory.CountItems("output", -1, true) == 1 && chest.Inventory.Items[0].m_stack == 2
                && chest.Inventory.Changes == 1 && !chest.m_inUse && CraftingContext.Current == null,
            "Transaction output, ingredient debit, callback or cleanup was incorrect.");
    }

    /// <summary>Player no-cost mode invokes vanilla crafting without a storage transaction.</summary>
    private static void PlayerNoCostBypassesStorage(Action<bool, string> check)
    {
        var (player, _, _, gui) = Setup();
        player.NoCost = true;
        bool bypassed = false;
        gui.OnCraft = _ => bypassed = !StorageCrafting.IsExecuting;
        gui.DoCrafting(player);
        check(bypassed, "Player no-cost mode entered a storage transaction.");
    }

    /// <summary>The world no-craft-cost key bypasses storage transaction handling.</summary>
    private static void WorldNoCostBypassesStorage(Action<bool, string> check)
    {
        var (player, _, _, gui) = Setup();
        ZoneSystem.instance.NoCost = true;
        bool bypassed = false;
        gui.OnCraft = _ => bypassed = !StorageCrafting.IsExecuting;
        gui.DoCrafting(player);
        check(bypassed, "World no-cost mode entered a storage transaction.");
    }

    /// <summary>Patched vanilla exceptions propagate while reservations and scope are cleaned up.</summary>
    private static void TransactionExceptionReleasesState(Action<bool, string> check)
    {
        var (player, chest, _, gui) = Setup();
        gui.OnCraft = _ => throw new InvalidOperationException("Simulated DoCrafting failure.");
        bool failed = false;

        try
        {
            gui.DoCrafting(player);
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }

        check(
            failed && !chest.m_inUse && !StorageCrafting.IsPending && CraftingContext.Current == null,
            "Transaction exception was lost or leaked state.");
    }

    /// <summary>An outstanding ownership request blocks duplicate crafts and manual chest interaction.</summary>
    private static void PendingOwnershipBlocksOtherActions(Action<bool, string> check)
    {
        var (player, chest, _, gui) = Setup();
        chest.m_nview.Owner = false;
        gui.DoCrafting(player);
        gui.OnCraftPressed();
        chest.Interact(player, false, false);
        check(
            StorageCrafting.IsPending && gui.CraftPresses == 0 && player.LastMessage == "$msg_inuse",
            "Pending ownership allowed a competing action.");
    }

    /// <summary>A lost ownership reply times out without spending ingredients or locking the chest.</summary>
    private static void TimeoutReleasesChest(Action<bool, string> check)
    {
        var (_, chest, _, _) = TimedOutSetup();
        check(
            !StorageCrafting.IsPending && !StorageCrafting.IsBusy(chest) && chest.Inventory.Items[0].m_stack == 4,
            "Timeout spent ingredients or left the chest locked.");
    }

    /// <summary>A new request can finish exactly one craft without receiving the original lost reply.</summary>
    private static void RetryCompletesWithoutOldReply(Action<bool, string> check)
    {
        var (player, chest, _, gui) = TimedOutSetup();
        int crafted = 0;
        gui.OnCraft = current =>
        {
            current.Inventory.RemoveItem("wood", 2, -1, true);
            crafted++;
        };

        gui.DoCrafting(player);
        check(StorageCrafting.HasRequest(chest), "Retry did not start a new ownership request.");
        chest.m_nview.Owner = true;
        chest.RPC_OpenResponse(1, true);
        CraftFromStoragePlugin.Instance.Advance();
        check(
            crafted == 1 && chest.Opened == 0 && chest.Inventory.Items[0].m_stack == 2,
            "Retry did not craft exactly once without opening the chest.");
    }

    /// <summary>A duplicate response after successful remote crafting cannot open the chest UI.</summary>
    private static void DuplicateCraftReplyDoesNotOpenChest(Action<bool, string> check)
    {
        var (player, chest, _, gui) = Setup();
        chest.m_nview.Owner = false;
        gui.OnCraft = current => current.Inventory.RemoveItem("wood", 2, -1, true);
        gui.DoCrafting(player);
        chest.m_nview.Owner = true;
        chest.RPC_OpenResponse(1, true);
        CraftFromStoragePlugin.Instance.Advance();
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0, "Duplicate crafting reply opened the chest.");
    }

    /// <summary>Holding chest interaction does not authorize an old crafting response.</summary>
    private static void HeldInteractionDoesNotAuthorizeReply(Action<bool, string> check)
    {
        var (player, chest, _, _) = TimedOutSetup();
        chest.m_nview.Owner = true;
        chest.Interact(player, true, false);
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0, "Held interaction authorized an old reply.");
    }

    /// <summary>Explicit manual interaction permits one reply, then suppresses duplicates.</summary>
    private static void ManualOpeningAcceptsOnlyOneReply(Action<bool, string> check)
    {
        var (player, chest, _, _) = TimedOutSetup();
        chest.m_nview.Owner = true;
        chest.m_nview.OnRequest = (_, _) => chest.RPC_OpenResponse(1, true);
        chest.Interact(player, false, false);
        check(chest.Opened == 1, "Explicit manual opening was blocked.");
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 1, "Manual authorization accepted a second reply.");
    }

    /// <summary>A manual reply arriving after its intent expires cannot open or lock the chest.</summary>
    private static void ExpiredManualIntentSuppressesReply(Action<bool, string> check)
    {
        var (_, chest) = ManualOpenSetup();
        Time.realtimeSinceStartup += 6;
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0 && !StorageCrafting.IsBusy(chest), "Expired manual intent opened or locked the chest.");
    }

    /// <summary>Moving beyond interaction range before a manual reply prevents opening.</summary>
    private static void ManualOpeningRechecksRange(Action<bool, string> check)
    {
        var (player, chest) = ManualOpenSetup();
        player.transform.position = new Vector3(10, 0, 0);
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0, "Out-of-range manual reply opened the chest.");
    }

    /// <summary>Revoking personal chest access before a manual reply prevents opening.</summary>
    private static void ManualOpeningRechecksPersonalAccess(Action<bool, string> check)
    {
        var (_, chest) = ManualOpenSetup();
        chest.Allowed = false;
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0, "Revoked personal access was ignored.");
    }

    /// <summary>A manual reply cannot open a chest after the requesting player dies.</summary>
    private static void ManualOpeningRechecksPlayerDeath(Action<bool, string> check)
    {
        var (player, chest) = ManualOpenSetup();
        player.Dead = true;
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0, "Manual reply opened a chest for a dead player.");
    }

    /// <summary>Revoking ward access before a manual reply prevents opening.</summary>
    private static void ManualOpeningRechecksWardAccess(Action<bool, string> check)
    {
        var (_, chest) = ManualOpenSetup();
        chest.m_checkGuardStone = true;
        PrivateArea.Allowed = false;
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0, "Revoked ward access was ignored.");
    }

    /// <summary>A granted manual reply must also have transferred chest ownership.</summary>
    private static void ManualOpeningRequiresOwnership(Action<bool, string> check)
    {
        var (_, chest) = ManualOpenSetup();
        chest.m_nview.Owner = false;
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0, "Manual grant without ownership opened the chest.");
    }

    /// <summary>A lost manual reply expires so another explicit interaction can be attempted.</summary>
    private static void LostManualReplyDoesNotLockChest(Action<bool, string> check)
    {
        var (_, chest) = ManualOpenSetup();
        Time.realtimeSinceStartup += 6;
        check(!StorageCrafting.IsBusy(chest), "Lost manual reply permanently locked the chest.");
    }

    /// <summary>The destruction patch removes response guards for the destroyed container.</summary>
    private static void DestructionRemovesResponseGuards(Action<bool, string> check)
    {
        var (_, chest, _, _) = TimedOutSetup();
        chest.OnDestroyed();
        check(!StorageCrafting.ReceiveResponse(chest, true), "Destroyed container retained its guard.");
    }

    /// <summary>Containers never requested by crafting keep vanilla chest-open responses.</summary>
    private static void UntouchedChestKeepsVanillaResponse(Action<bool, string> check)
    {
        Reset();
        var chest = new Container();
        chest.Awake();
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 1, "Untouched container response was intercepted.");
    }

    /// <summary>The Cancel prefix releases pending requests while suppressing late UI-opening replies.</summary>
    private static void CancelPatchReleasesPendingRequest(Action<bool, string> check)
    {
        var (player, chest, _, gui) = Setup();
        chest.m_nview.Owner = false;
        gui.DoCrafting(player);
        gui.OnCraftCancelPressed();
        CraftFromStoragePlugin.Instance.Advance();
        chest.RPC_OpenResponse(1, true);
        check(
            !StorageCrafting.IsPending && !StorageCrafting.IsBusy(chest) && chest.Opened == 0,
            "Cancel leaked a request or allowed a late reply to open the chest.");
    }

    private static (Player Player, Container Chest, Recipe Recipe, InventoryGui Gui) Setup()
    {
        var player = Reset();
        var chest = new Container();
        chest.transform.position = new Vector3(1, 0, 0);
        chest.Inventory.Items.Add(Item("wood", 4));
        chest.Awake();
        var recipe = RecipeFor("wood", 2);
        var gui = Gui(recipe, _ => throw new InvalidOperationException("Unexpected craft in pending/rejected scenario."));

        return (player, chest, recipe, gui);
    }

    private static (Player Player, Container Chest, Recipe Recipe, InventoryGui Gui) TimedOutSetup()
    {
        var state = Setup();
        state.Chest.m_nview.Owner = false;
        state.Gui.DoCrafting(state.Player);
        Time.realtimeSinceStartup = 6;
        CraftFromStoragePlugin.Instance.Advance();

        return state;
    }

    private static (Player Player, Container Chest) ManualOpenSetup()
    {
        var (player, chest, _, _) = TimedOutSetup();
        chest.m_nview.Owner = true;
        chest.Interact(player, false, false);

        return (player, chest);
    }
}