using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimMods.Common.Crafting;
using ValheimMods.Testing;
using static CookFromStorage.Tests.CookTestSetup;
using RecipeNearbyStorage = ValheimMods.Common.Crafting.NearbyStorage;

namespace CookFromStorage.Tests;

internal static class RecipeIntegrationTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(CauldronRecipesWorkWithCookingModAlone);
        runner.Run(PreparationTableRecipesWorkWithCookingModAlone);
        runner.Run(MeadKetillRecipesWorkWithCookingModAlone);
        runner.Run(CookingClientExcludesNonFoodStations);
        runner.Run(CraftingClientExcludesFoodStations);
        runner.Run(BothClientsCountAndConsumeOnlyOnce);
        runner.Run(EachClientUsesItsOwnStorageRadius);
        runner.Run(UnloadingOneClientKeepsOtherCategoryWorking);
        runner.Run(FoodRecipeOwnershipHandshakeWorksAlone);
        runner.Run(DisabledCookingClientLeavesRecipesVanilla);
        runner.Run(FoodAlternativeIngredientsUseStorage);
        runner.Run(UnloadingCookingCancelsPendingFoodOwnership);
        runner.Run(DisablingCookingCancelsPendingFoodOwnership);
    }

    /// <summary>Cauldron requirement checks, displays and consumption work without CraftFromStorage.</summary>
    private static void CauldronRecipesWorkWithCookingModAlone(Action<bool, string> check)
    {
        CheckFoodRecipe("$piece_cauldron", check);
    }

    /// <summary>Food preparation table recipes use the shared engine with only the cooking client registered.</summary>
    private static void PreparationTableRecipesWorkWithCookingModAlone(Action<bool, string> check)
    {
        CheckFoodRecipe("$piece_preptable", check);
    }

    /// <summary>Mead Ketill recipes use the cooking client without a separate crafting plugin.</summary>
    private static void MeadKetillRecipesWorkWithCookingModAlone(Action<bool, string> check)
    {
        CheckFoodRecipe("$piece_meadcauldron", check);
    }

    /// <summary>The cooking client leaves workbench, forge and unknown station recipes unchanged.</summary>
    private static void CookingClientExcludesNonFoodStations(Action<bool, string> check)
    {
        using var host = new RecipeHost(true);

        foreach (string stationName in new[] { "$piece_workbench", "$piece_forge", "$piece_modded_station" })
        {
            var (player, chest, recipe, gui) = Setup(stationName);
            check(!player.HaveRequirementItems(recipe, false, 1, 1), "Cooking counted a non-food station's storage.");
            gui.DoCrafting(player);
            check(chest.Inventory.Items[0].m_stack == 4, "Cooking redirected non-food consumption.");
        }
    }

    /// <summary>The crafting client does not handle any of the three food recipe stations.</summary>
    private static void CraftingClientExcludesFoodStations(Action<bool, string> check)
    {
        using var host = new RecipeHost(false);

        foreach (string stationName in new[] { "$piece_cauldron", "$piece_preptable", "$piece_meadcauldron" })
        {
            var (player, chest, recipe, gui) = Setup(stationName);
            check(!player.HaveRequirementItems(recipe, false, 1, 1), "Crafting counted a food station's storage.");
            gui.DoCrafting(player);
            check(chest.Inventory.Items[0].m_stack == 4, "Crafting redirected food consumption.");
        }
    }

    /// <summary>Registering both clients installs one shared patch set and does not double-count or double-consume.</summary>
    private static void BothClientsCountAndConsumeOnlyOnce(Action<bool, string> check)
    {
        using var cooking = new RecipeHost(true);
        using var crafting = new RecipeHost(false);

        foreach (string name in new[] { "$piece_cauldron", "$piece_workbench" })
        {
            var (player, chest, recipe, gui) = Setup(name);

            using (var context = CraftingContext.Enter(player, recipe))
            {
                check(player.Inventory.CountItems("$item_raw_meat", -1, true) == 4, "Storage counts were doubled.");
            }

            gui.DoCrafting(player);
            check(chest.Inventory.Items[0].m_stack == 2 && player.Inventory.Items.Count == 1,
                "Recipe execution duplicated output or consumed more than one recipe's cost.");
        }

        var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(Inventory), "CountItems"));
        check(info.Postfixes.Count == 1, "Both clients installed duplicate count patches.");
    }

    /// <summary>Food and non-food recipes use their own plugin's configured storage radius.</summary>
    private static void EachClientUsesItsOwnStorageRadius(Action<bool, string> check)
    {
        using var cooking = new RecipeHost(true, 5f);
        using var crafting = new RecipeHost(false, 15f);

        var (player, chest, recipe, gui) = Setup("$piece_cauldron");
        chest.transform.position = new Vector3(10, 0, 0);
        check(!player.HaveRequirementItems(recipe, false, 1, 1), "A food recipe used storage outside the cooking radius.");
        gui.DoCrafting(player);
        check(chest.Inventory.Items[0].m_stack == 4, "A food recipe consumed storage outside the cooking radius.");

        (player, chest, recipe, gui) = Setup("$piece_workbench");
        chest.transform.position = new Vector3(10, 0, 0);
        check(player.HaveRequirementItems(recipe, false, 1, 1), "A non-food recipe ignored storage inside the crafting radius.");
        gui.DoCrafting(player);
        check(chest.Inventory.Items[0].m_stack == 2, "A non-food recipe did not consume storage inside the crafting radius.");

        (player, chest, recipe, _) = Setup("$piece_workbench");
        chest.transform.position = new Vector3(15.01f, 0, 0);
        check(!player.HaveRequirementItems(recipe, false, 1, 1), "A non-food recipe used storage outside the crafting radius.");
    }

    /// <summary>Unloading either plugin leaves the other plugin's shared engine active and its own category excluded.</summary>
    private static void UnloadingOneClientKeepsOtherCategoryWorking(Action<bool, string> check)
    {
        using var cooking = new RecipeHost(true);
        using var crafting = new RecipeHost(false);
        cooking.Dispose();
        var (player, _, recipe, _) = Setup("$piece_workbench");
        check(player.HaveRequirementItems(recipe, false, 1, 1), "Unloading cooking disabled crafting.");
        (player, _, recipe, _) = Setup("$piece_cauldron");
        check(!player.HaveRequirementItems(recipe, false, 1, 1), "Unloaded cooking still counted food recipes.");

        using var replacement = new RecipeHost(true);
        crafting.Dispose();
        check(player.HaveRequirementItems(recipe, false, 1, 1), "Unloading crafting disabled cooking.");
    }

    /// <summary>The cooking client independently acquires storage ownership before consuming recipe ingredients.</summary>
    private static void FoodRecipeOwnershipHandshakeWorksAlone(Action<bool, string> check)
    {
        using var host = new RecipeHost(true);
        var (player, chest, _, gui) = Setup("$piece_meadcauldron");
        chest.m_nview.Owner = false;
        chest.m_nview.OnRequest = (_, _) =>
        {
            chest.m_nview.Owner = true;
            chest.RPC_OpenResponse(1, true);
        };
        gui.DoCrafting(player);
        host.Advance();
        check(chest.Inventory.Items[0].m_stack == 2 && player.Inventory.Items.Count == 1 && !chest.m_inUse,
            "Food recipe failed to acquire ownership, consume ingredients and release storage.");
    }

    /// <summary>Disabling cooking automation leaves food recipe counts and consumption unchanged.</summary>
    private static void DisabledCookingClientLeavesRecipesVanilla(Action<bool, string> check)
    {
        using var host = new RecipeHost(true);
        var (player, chest, recipe, gui) = Setup("$piece_cauldron");
        CookFromStoragePlugin.AutomaticIngredientsEnabled = false;
        check(!player.HaveRequirementItems(recipe, false, 1, 1), "Disabled cooking still counted storage.");
        gui.DoCrafting(player);
        check(chest.Inventory.Items[0].m_stack == 4, "Disabled cooking consumed storage.");
    }

    /// <summary>Food recipes using one alternative ingredient preserve the existing shared selection and quality policy.</summary>
    private static void FoodAlternativeIngredientsUseStorage(Action<bool, string> check)
    {
        using var host = new RecipeHost(true);
        var (player, _, recipe, _) = Setup("$piece_cauldron");
        recipe.m_requireOnlyOneIngredient = true;
        recipe.m_resources[0].m_resItem.m_itemData.m_shared.m_maxQuality = 3;
        var item = player.GetFirstRequiredItem(player.Inventory, recipe, 1, out int amount, out _, 1);
        check(item != null && amount == 2, "Food alternative selection did not find the stored ingredient.");
    }

    /// <summary>Unloading cooking cancels its pending transaction without disabling a still-registered crafting client.</summary>
    private static void UnloadingCookingCancelsPendingFoodOwnership(Action<bool, string> check)
    {
        using var cooking = new RecipeHost(true);
        using var crafting = new RecipeHost(false);
        var (player, chest, _, gui) = Setup("$piece_cauldron");
        chest.m_nview.Owner = false;
        gui.DoCrafting(player);
        check(StorageCrafting.IsPending && chest.Inventory.Items[0].m_stack == 4,
            "Food ownership request did not suspend before consuming ingredients.");
        cooking.Dispose();
        check(!StorageCrafting.IsPending && !chest.m_inUse && player.Inventory.Items.Count == 0,
            "Unloading cooking left its pending transaction alive.");
        chest.RPC_OpenResponse(1, true);
        check(chest.Opened == 0 && chest.Inventory.Items[0].m_stack == 4,
            "A late cancelled food request opened storage or consumed ingredients.");
        var (nextPlayer, _, recipe, _) = Setup("$piece_workbench");
        check(nextPlayer.HaveRequirementItems(recipe, false, 1, 1),
            "Cancelling food ownership disabled the other recipe client.");
    }

    /// <summary>Turning cooking automation off while ownership is pending cancels before any ingredient mutation.</summary>
    private static void DisablingCookingCancelsPendingFoodOwnership(Action<bool, string> check)
    {
        using var cooking = new RecipeHost(true);
        var (player, chest, _, gui) = Setup("$piece_cauldron");
        chest.m_nview.Owner = false;
        gui.DoCrafting(player);
        CookFromStoragePlugin.AutomaticIngredientsEnabled = false;
        cooking.Advance();
        check(!StorageCrafting.IsPending && chest.Inventory.Items[0].m_stack == 4 && player.Inventory.Items.Count == 0,
            "Disabling cooking did not cancel a pending recipe before consumption.");
    }

    private static void CheckFoodRecipe(string stationName, Action<bool, string> check)
    {
        using var host = new RecipeHost(true);
        var (player, chest, recipe, gui) = Setup(stationName);
        check(player.HaveRequirementItems(recipe, false, 1, 1), "Food recipe availability ignored storage.");
        check(InventoryGui.SetupRequirement(new Transform(), recipe.m_resources[0], player, true, 1, 1),
            "Food recipe display ignored storage.");
        check(player.Inventory.CountItems("$item_raw_meat", -1, true) == 0 && CraftingContext.Current == null,
            "Food recipe preview leaked storage counts into ordinary inventory.");
        gui.DoCrafting(player);
        check(chest.Inventory.Items[0].m_stack == 2 && player.Inventory.Items.Count == 1,
            "Food recipe failed to consume storage and produce one output.");
    }

    private static (Player Player, Container Chest, Recipe Recipe, InventoryGui Gui) Setup(string stationName)
    {
        var player = Reset();
        RecipeNearbyStorage.Clear();
        StorageCrafting.Clear();
        Time.frameCount++;
        Time.realtimeSinceStartup = 0;
        Game.m_worldLevel = 0;
        ZoneSystem.instance.NoCost = false;
        InventoryGui.Visible = true;
        player.Station = new CraftingStation { m_name = stationName };
        var chest = Chest(1, 0, 0, Item("$item_raw_meat", "RawMeat", 4));
        RecipeNearbyStorage.Add(chest);
        var recipe = new Recipe
        {
            m_craftingStation = player.Station,
            m_resources =
            [
                new()
                {
                    m_resItem = new() { m_itemData = Item("$item_raw_meat", "RawMeat", 1) },
                    m_amount = 2
                }
            ]
        };
        var gui = InventoryGui.instance = new InventoryGui
        {
            m_craftRecipe = recipe,
            m_selectedRecipe = (recipe, null),
            OnCraft = current =>
            {
                if (current.HaveRequirementItems(recipe, false, 1, 1))
                {
                    current.Inventory.RemoveItem("$item_raw_meat", 2, -1, true);
                    current.Inventory.Items.Add(Item("$item_output", "Output", 1));
                }
            }
        };

        return (player, chest, recipe, gui);
    }

    private sealed class RecipeHost : IDisposable
    {
        private readonly List<IEnumerator> _coroutines = new();
        private readonly RecipeStorage _registration;

        internal RecipeHost(bool food, float radius = 20f)
        {
            _registration = RecipeStorage.Register(
                food,
                Start,
                Stop,
                CookFromStoragePlugin.Log.LogWarning,
                CookFromStoragePlugin.Log.LogError,
                () => !food || CookFromStoragePlugin.AutomaticIngredientsEnabled,
                () => radius);
        }

        public void Dispose()
        {
            _registration.Dispose();
            Stop();
        }

        internal void Advance()
        {
            _coroutines.RemoveAll(coroutine => !coroutine.MoveNext());
        }

        private void Start(IEnumerator coroutine)
        {
            if (coroutine.MoveNext())
            {
                _coroutines.Add(coroutine);
            }
        }

        private void Stop()
        {
            foreach (var coroutine in _coroutines)
            {
                (coroutine as IDisposable)?.Dispose();
            }

            _coroutines.Clear();
        }
    }
}