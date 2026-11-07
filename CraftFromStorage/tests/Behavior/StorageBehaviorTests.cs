using System;
using System.Linq;
using UnityEngine;
using ValheimMods.Testing;
using static CraftFromStorage.Tests.CraftingTestSetup;

namespace CraftFromStorage.Tests;

internal static class StorageBehaviorTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(DiscoveryUsesInclusiveSphere);
        runner.Run(PersonalPermissionsExcludeStorage);
        runner.Run(WardsExcludeStorage);
        runner.Run(LocalBusyFlagExcludesStorage);
        runner.Run(RemoteBusyFlagExcludesStorage);
        runner.Run(MovingCartExcludesStorage);
        runner.Run(InvalidNetworkViewExcludesStorage);
        runner.Run(IngredientsUseInventoryThenNearestStorage);
        runner.Run(RequirementCountsStayScoped);
        runner.Run(NestedContextsRestoreOuterPlan);
        runner.Run(HandCraftingIsExcluded);
        runner.Run(MissingStationIsExcluded);
        runner.Run(RemotePlayerIsExcluded);
        runner.Run(AlternativeIngredientsRespectQualityAndWorldLevel);
        runner.Run(DuplicateRequirementsCannotDoubleSpend);
        runner.Run(MultiCraftRequiresMultipliedCost);
        runner.Run(UpgradeUsesQualityAndMultiplier);
        runner.Run(OwnershipHandshakeReservesAndConsumes);
        runner.Run(DeniedOwnershipDoesNotConsume);
        runner.Run(TimeoutSuppressesLateResponse);
        runner.Run(CancellationReleasesEarlierReservations);
        runner.Run(MovingOutOfRangeCancels);
        runner.Run(CraftingExceptionReleasesState);
        runner.Run(OrdinaryResponsesRemainVanilla);
        runner.Run(SufficientInventoryDoesNotRequestStorage);
        runner.Run(VanillaRejectionDoesNotConsume);
        runner.Run(IngredientsAreReplannedAfterOwnership);
        runner.Run(RecipeSelectionChangeCancels);
        runner.Run(InactiveStorageIsExcluded);
        runner.Run(RegularStationExcludesUpgraderResources);
        runner.Run(InvalidatedReservedChestCancels);
        runner.Run(InvalidatedWaitingChestCancels);
    }

    /// <summary>Discovery includes the 20m boundary but rejects points outside the 3D sphere.</summary>
    private static void DiscoveryUsesInclusiveSphere(Action<bool, string> check)
    {
        var player = Reset();
        var boundary = Chest(0, 20, 0, Item("wood", 2));
        Chest(0, 20.001f, 0, Item("wood", 100));
        Chest(15, 15, 0, Item("wood", 100));
        check(NearbyStorage.Find(player).SequenceEqual([boundary]), "Discovery did not use an inclusive sphere.");
    }

    /// <summary>Personal chest permissions exclude otherwise nearby storage.</summary>
    private static void PersonalPermissionsExcludeStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        chest.Allowed = false;
        check(!NearbyStorage.IsAccessible(chest, player), "Personal access denial was ignored.");
    }

    /// <summary>Ward-protected storage is unavailable when ward access is denied.</summary>
    private static void WardsExcludeStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        chest.m_checkGuardStone = true;
        PrivateArea.Allowed = false;
        check(!NearbyStorage.IsAccessible(chest, player), "Ward denial was ignored.");
    }

    /// <summary>A locally open chest cannot supply crafting ingredients.</summary>
    private static void LocalBusyFlagExcludesStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        chest.m_inUse = true;
        check(!NearbyStorage.IsAccessible(chest, player), "Locally busy chest was accessible.");
    }

    /// <summary>A remote owner's replicated busy flag excludes its chest.</summary>
    private static void RemoteBusyFlagExcludesStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        chest.m_nview.Owner = false;
        chest.m_nview.Zdo.Busy = 1;
        check(!NearbyStorage.IsAccessible(chest, player), "Remotely busy chest was accessible.");
    }

    /// <summary>A cart in use cannot supply ingredients.</summary>
    private static void MovingCartExcludesStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        chest.m_wagon = new Vagon
        {
            Busy = true
        };

        check(!NearbyStorage.IsAccessible(chest, player), "Moving cart was accessible.");
    }

    /// <summary>Invalid network views exclude their containers from crafting.</summary>
    private static void InvalidNetworkViewExcludesStorage(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        chest.m_nview.Valid = false;
        check(!NearbyStorage.IsAccessible(chest, player), "Invalid network view was accepted.");
    }

    /// <summary>Allocation combines inventories and consumes player stacks before nearest chests.</summary>
    private static void IngredientsUseInventoryThenNearestStorage(Action<bool, string> check)
    {
        var player = Reset();
        player.Inventory.Items.Add(Item("wood", 2));
        var far = Chest(10, 0, 0, Item("wood", 8));
        var near = Chest(2, 0, 0, Item("wood", 3));
        var recipe = RecipeFor("wood", 6);

        using (var context = CraftingContext.Enter(player, recipe))
        {
            var plan = IngredientPlan.Create(context, recipe, 1, 1);
            check(plan != null && context.CountStorage("wood", 1, true) == 11, "Combined ingredient count was incorrect.");
            check(plan.StorageSources(context).SequenceEqual([near, far]), "Sources were not nearest-first.");
            plan.Consume("wood", 6, -1, true);
            check(
                player.Inventory.Items.Count == 0 && near.Inventory.Items.Count == 0 && far.Inventory.Items[0].m_stack == 7,
                "Consumption did not use inventory then nearest storage.");
        }

        check(CraftingContext.Current == null, "Disposed context remained current.");
    }

    /// <summary>Additional counts apply only inside explicit requirement scopes.</summary>
    private static void RequirementCountsStayScoped(Action<bool, string> check)
    {
        var player = Reset();
        var recipe = RecipeFor("wood", 1);

        using var transaction = new CraftingContext(player, NearbyStorage.Find(player));
        check(!transaction.IncludeStorageCounts, "Transaction globally enabled extra counts.");

        using (var context = CraftingContext.Enter(player, recipe))
        {
            check(context.IncludeStorageCounts, "Requirement scope did not enable extra counts.");
        }

        check(!CraftingContext.Current.IncludeStorageCounts, "Requirement scope leaked extra counts.");
    }

    /// <summary>Nested contexts inherit reservations and plans, then restore their parent.</summary>
    private static void NestedContextsRestoreOuterPlan(Action<bool, string> check)
    {
        var player = Reset();
        var recipe = RecipeFor("wood", 1);
        Chest(1, 0, 0, Item("wood", 2));

        using var outer = CraftingContext.Enter(player, recipe);
        outer.Reserved = true;
        outer.Plan = IngredientPlan.Create(outer, recipe, 1, 1);

        using (var inner = CraftingContext.Enter(player, recipe))
        {
            check(inner.Plan == outer.Plan && inner.Reserved, "Nested context lost plan or reservation.");
        }

        check(CraftingContext.Current == outer, "Nested disposal did not restore parent.");
    }

    /// <summary>Recipes without a crafting station do not enter storage-count scopes.</summary>
    private static void HandCraftingIsExcluded(Action<bool, string> check)
    {
        var player = Reset();
        var recipe = RecipeFor("wood", 1);
        recipe.m_craftingStation = null;
        check(CraftingContext.Enter(player, recipe) == null, "Hand crafting entered storage scope.");
    }

    /// <summary>A player without a current station cannot use storage crafting.</summary>
    private static void MissingStationIsExcluded(Action<bool, string> check)
    {
        var player = Reset();
        player.Station = null;
        check(CraftingContext.Enter(player, RecipeFor("wood", 1)) == null, "Missing station was accepted.");
    }

    /// <summary>Storage counts are never enabled for a remote player.</summary>
    private static void RemotePlayerIsExcluded(Action<bool, string> check)
    {
        Reset();
        check(CraftingContext.Enter(new Player(), RecipeFor("wood", 1)) == null, "Remote player entered storage scope.");
    }

    /// <summary>Alternative recipes choose sufficient same-quality, world-matching stacks only.</summary>
    private static void AlternativeIngredientsRespectQualityAndWorldLevel(Action<bool, string> check)
    {
        var player = Reset();
        Game.m_worldLevel = 2;
        Chest(1, 0, 0, Item("ore", 4, 1, 1), Item("ore", 3, 2, 2), Item("ore", 5, 1, 2));
        var recipe = RecipeFor("ore", 5);
        recipe.m_requireOnlyOneIngredient = true;

        using var context = CraftingContext.Enter(player, recipe);
        check(context.CountStorage("ore", 1, true) == 5 && context.CountStorage("ore", 2, true) == 3, "Quality/world filters failed.");
        var first = context.FirstIngredient(recipe, 1, 1, out int amount, out _);
        check(first.m_quality == 1 && amount == 5, "Alternative ingredient selection was incorrect.");
        var plan = IngredientPlan.Create(context, recipe, 1, 1);
        plan.Consume("ore", 5, 1, true);
        check(context.CountStorage("ore", 1, true) == 0 && context.CountStorage("ore", 2, true) == 3, "Other qualities were consumed.");
    }

    /// <summary>Duplicate requirements cannot allocate the same stack twice or mutate a failed plan.</summary>
    private static void DuplicateRequirementsCannotDoubleSpend(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("wood", 5));
        var recipe = RecipeFor("wood", 3);
        recipe.m_resources = [recipe.m_resources[0], recipe.m_resources[0]];

        using var context = CraftingContext.Enter(player, recipe);
        check(
            IngredientPlan.Create(context, recipe, 1, 1) == null && chest.Inventory.Items[0].m_stack == 5,
            "Duplicate requirements double-spent or mutated a stack.");
    }

    /// <summary>Multi-craft planning rejects ingredients below the multiplied requirement.</summary>
    private static void MultiCraftRequiresMultipliedCost(Action<bool, string> check)
    {
        var player = Reset();
        Chest(1, 0, 0, Item("wood", 5));

        using var context = CraftingContext.Enter(player, RecipeFor("wood", 2));
        check(IngredientPlan.Create(context, RecipeFor("wood", 2), 1, 3) == null, "Multi-craft ignored its multiplier.");
    }

    /// <summary>Upgrade planning uses the target quality cost and multi-craft multiplier.</summary>
    private static void UpgradeUsesQualityAndMultiplier(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("wood", 5));
        var recipe = RecipeFor("wood", 2);

        using var context = CraftingContext.Enter(player, recipe);
        var plan = IngredientPlan.Create(context, recipe, 2, 2);
        plan.Consume("wood", 4, -1, true);
        check(chest.Inventory.Items[0].m_stack == 1, "Upgrade cost/multiplier was incorrect.");
    }

    /// <summary>Remote ownership must arrive before reserved storage can be consumed.</summary>
    private static void OwnershipHandshakeReservesAndConsumes(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("wood", 4));
        chest.m_nview.Owner = false;
        bool crafted = false;
        var gui = Gui(RecipeFor("wood", 2), _ =>
        {
            check(chest.m_inUse && StorageCrafting.IsExecuting, "Craft ran without reservation or execution scope.");
            CraftingContext.Current.Plan.Consume("wood", 2, -1, true);
            crafted = true;
        });

        var craft = StorageCrafting.Craft(gui, player);
        check(craft.MoveNext() && StorageCrafting.HasRequest(chest) && !crafted, "Craft did not wait for ownership.");
        chest.m_nview.Owner = true;
        check(StorageCrafting.ReceiveResponse(chest, true), "Ownership response was not intercepted.");
        check(
            !craft.MoveNext() && crafted && !chest.m_inUse && !StorageCrafting.IsPending && chest.Inventory.Items[0].m_stack == 2,
            "Craft did not consume and release its reservation.");
    }

    /// <summary>An ownership denial cancels without producing output or spending ingredients.</summary>
    private static void DeniedOwnershipDoesNotConsume(Action<bool, string> check)
    {
        var (player, chest, gui) = PendingSetup();
        var craft = StorageCrafting.Craft(gui, player);
        craft.MoveNext();
        StorageCrafting.ReceiveResponse(chest, false);
        check(!craft.MoveNext() && chest.Inventory.Items[0].m_stack == 4, "Denied ownership spent ingredients.");
    }

    /// <summary>Ownership timeout preserves ingredients and suppresses a late chest-open response.</summary>
    private static void TimeoutSuppressesLateResponse(Action<bool, string> check)
    {
        var (player, chest, gui) = PendingSetup();
        var craft = StorageCrafting.Craft(gui, player);
        craft.MoveNext();
        Time.realtimeSinceStartup = 6;
        check(!craft.MoveNext() && chest.Inventory.Items[0].m_stack == 4, "Timeout spent ingredients.");
        check(StorageCrafting.ReceiveResponse(chest, true) && !StorageCrafting.HasRequest(chest), "Late response was not swallowed.");
    }

    /// <summary>Cancellation releases earlier reservations while a later source awaits ownership.</summary>
    private static void CancellationReleasesEarlierReservations(Action<bool, string> check)
    {
        var player = Reset();
        var locked = Chest(1, 0, 0, Item("wood", 1));
        var waiting = Chest(2, 0, 0, Item("wood", 1));
        waiting.m_nview.Owner = false;
        var gui = Gui(RecipeFor("wood", 2), UnexpectedCraft);
        var craft = StorageCrafting.Craft(gui, player);
        craft.MoveNext();
        check(locked.m_inUse, "Earlier chest was not reserved.");
        StorageCrafting.Cancel();
        check(!craft.MoveNext() && !locked.m_inUse && locked.Inventory.Items[0].m_stack == 1, "Cancel leaked a reservation or spent ingredients.");
    }

    /// <summary>Storage range is rechecked after ownership arrives.</summary>
    private static void MovingOutOfRangeCancels(Action<bool, string> check)
    {
        var (player, chest, gui) = PendingSetup();
        var craft = StorageCrafting.Craft(gui, player);
        craft.MoveNext();
        player.transform.position = new Vector3(30, 0, 0);
        chest.m_nview.Owner = true;
        StorageCrafting.ReceiveResponse(chest, true);
        check(!craft.MoveNext() && chest.Inventory.Items[0].m_stack == 4, "Out-of-range storage was consumed.");
    }

    /// <summary>Exceptions during vanilla crafting release reservations and the current context.</summary>
    private static void CraftingExceptionReleasesState(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("wood", 4));
        var gui = Gui(RecipeFor("wood", 2), UnexpectedCraft);
        var craft = StorageCrafting.Craft(gui, player);
        bool failed = false;

        try
        {
            craft.MoveNext();
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }

        check(
            failed && !chest.m_inUse && !StorageCrafting.IsPending && CraftingContext.Current == null,
            "Exception did not propagate or leaked crafting state.");
    }

    /// <summary>Responses for chests never requested by crafting remain vanilla.</summary>
    private static void OrdinaryResponsesRemainVanilla(Action<bool, string> check)
    {
        Reset();
        var chest = Chest(1, 0, 0);
        check(!StorageCrafting.ReceiveResponse(chest, true), "Ordinary response was intercepted.");
    }

    /// <summary>Enough player ingredients avoid ownership requests and storage debits entirely.</summary>
    private static void SufficientInventoryDoesNotRequestStorage(Action<bool, string> check)
    {
        var (player, chest, _) = PendingSetup();
        player.Inventory.Items.Add(Item("wood", 2));
        chest.m_nview.OnRequest = (_, _) => throw new InvalidOperationException("Unneeded chest requested.");
        var gui = Gui(RecipeFor("wood", 2), _ => CraftingContext.Current.Plan.Consume("wood", 2, -1, true));
        check(!StorageCrafting.Craft(gui, player).MoveNext() && chest.Inventory.Items[0].m_stack == 4, "Inventory-only craft debited storage.");
    }

    /// <summary>A vanilla rejection before removal leaves reserved ingredients untouched.</summary>
    private static void VanillaRejectionDoesNotConsume(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("wood", 4));
        var gui = Gui(RecipeFor("wood", 2), _ =>
        {
        });

        check(
            !StorageCrafting.Craft(gui, player).MoveNext() && !chest.m_inUse && chest.Inventory.Items[0].m_stack == 4,
            "Vanilla rejection consumed ingredients or leaked reservation.");
    }

    /// <summary>Disappearing ingredients after an ownership request invalidate the refreshed plan.</summary>
    private static void IngredientsAreReplannedAfterOwnership(Action<bool, string> check)
    {
        var (player, chest, gui) = PendingSetup();
        var craft = StorageCrafting.Craft(gui, player);
        craft.MoveNext();
        chest.Inventory.Items.Clear();
        chest.m_nview.Owner = true;
        StorageCrafting.ReceiveResponse(chest, true);
        check(!craft.MoveNext() && !chest.m_inUse, "Changed ingredients were not replanned.");
    }

    /// <summary>Changing the selected recipe while awaiting ownership cancels the original craft.</summary>
    private static void RecipeSelectionChangeCancels(Action<bool, string> check)
    {
        var (player, chest, gui) = PendingSetup();
        var craft = StorageCrafting.Craft(gui, player);
        craft.MoveNext();
        gui.m_selectedRecipe = (RecipeFor("stone", 1), null);
        check(!craft.MoveNext() && chest.Inventory.Items[0].m_stack == 4, "Changed selection did not cancel.");
    }

    /// <summary>Inactive containers do not appear in discovered storage.</summary>
    private static void InactiveStorageIsExcluded(Action<bool, string> check)
    {
        var player = Reset();
        var chest = Chest(1, 0, 0);
        chest.isActiveAndEnabled = false;
        check(NearbyStorage.Find(player).Count == 0, "Inactive container was discovered.");
    }

    /// <summary>Regular stations do not allocate special upgrader-only resources.</summary>
    private static void RegularStationExcludesUpgraderResources(Action<bool, string> check)
    {
        var player = Reset();
        Chest(1, 0, 0, Item("wood", 4));
        var recipe = RecipeFor("wood", 2);
        recipe.m_resources[0].m_upgraderResource = true;

        using var context = CraftingContext.Enter(player, recipe);
        check(IngredientPlan.Create(context, recipe, 1, 1).StorageSources(context).Count == 0, "Upgrader-only ingredient was allocated.");
    }

    /// <summary>Invalidating an earlier reserved chest cancels safely after another source responds.</summary>
    private static void InvalidatedReservedChestCancels(Action<bool, string> check)
    {
        CheckInvalidatedChest(check, false);
    }

    /// <summary>Invalidating the chest currently awaiting ownership cancels without dereferencing it.</summary>
    private static void InvalidatedWaitingChestCancels(Action<bool, string> check)
    {
        CheckInvalidatedChest(check, true);
    }

    private static void CheckInvalidatedChest(Action<bool, string> check, bool waitingChest)
    {
        var player = Reset();
        var reserved = Chest(1, 0, 0, Item("wood", 1));
        var waiting = Chest(2, 0, 0, Item("wood", 1));
        waiting.m_nview.Owner = false;
        var gui = Gui(RecipeFor("wood", 2), UnexpectedCraft);
        var craft = StorageCrafting.Craft(gui, player);
        check(craft.MoveNext() && reserved.m_inUse, "Regression did not reach ownership wait.");
        var invalid = waitingChest ? waiting : reserved;
        invalid.m_nview = null;
        StorageCrafting.ReceiveResponse(waiting, true);

        if (!waitingChest)
        {
            waiting.m_nview.Owner = true;
        }

        check(!craft.MoveNext() && !reserved.m_inUse && !StorageCrafting.IsPending, "Invalidated chest did not cancel cleanly.");
    }

    private static (Player Player, Container Chest, InventoryGui Gui) PendingSetup()
    {
        var player = Reset();
        var chest = Chest(1, 0, 0, Item("wood", 4));
        chest.m_nview.Owner = false;

        return (player, chest, Gui(RecipeFor("wood", 2), UnexpectedCraft));
    }

    private static void UnexpectedCraft(Player player)
    {
        throw new InvalidOperationException("Craft must not execute in this scenario.");
    }
}