using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using ValheimMods.Common;
using ValheimMods.Common.Crafting;
using ValheimMods.Common.Placement;
using ValheimMods.Testing;

namespace FarmFromStorage.Tests;

internal static class PlantingTests
{
    private static readonly List<string> _warnings = new();
    private static readonly List<string> _errors = new();
    private static float _radius;

    internal static void Run(TestRunner runner)
    {
        runner.Run(LegacyBuildPatchesAreRejected);
        using var farm = RegisterFarm();

        try
        {
            runner.Run(AnyCultivatorResourceCanComeFromStorage);
            runner.Run(InventoryAndNearestStorageSupplyOnlyTheRemainingCost);
            runner.Run(InsufficientAndDuplicateRequirementsDoNotSpendItems);
            runner.Run(FailedPlacementReleasesStorageWithoutConsumption);
            runner.Run(AccessBusyAndOwnershipFiltersArePreserved);
            runner.Run(RangeIsPlayerCenteredAndConfigurable);
            runner.Run(StorageCountsDoNotLeakToOtherQueries);
            runner.Run(FirstMenuOpeningRefreshesAllAvailability);
            runner.Run(MenuRefreshIsScopedToOpeningAndFarmTool);
            runner.Run(MenuRefreshPreservesVanillaAndRemoteUpdates);
            runner.Run(OtherToolsAndRemotePlayersRemainVanilla);
            runner.Run(TooltipShowsCarriedAndStoredCounts);
            runner.Run(FreeAndNoPlacementCostModesDoNotUseStorage);
            runner.Run(StorageChangesBeforePlacementBlockThePlan);
            runner.Run(UnexpectedConsumptionFailureIsReportedAndReleased);
            runner.Run(ContainerLifecycleUpdatesTheRegistry);
            runner.Run(WorldLevelMatchingIsPreserved);
            runner.Run(HammerAndCultivatorUseIndependentClients);
            runner.Run(RecipeAndPlacementCountsStayIndependent);
            runner.Run(UnloadingFarmLeavesHammerWorking);
        }
        finally
        {
            PlacementStorageContext.Reset();
        }

        farm.Dispose();
        runner.Run(UnloadingHammerLeavesFarmWorking);
    }

    /// <summary>Arbitrary Cultivator piece requirements are supported without a hardcoded seed whitelist.</summary>
    private static void AnyCultivatorResourceCanComeFromStorage(Action<bool, string> check)
    {
        foreach (string resource in new[] { "CarrotSeeds", "TurnipSeeds", "OnionSeeds", "Carrot", "Barley", "Flax", "PineCone", "CustomSeed" })
        {
            var setup = CreateSetup();
            setup.Piece.m_resources = [Requirement(resource, 1)];
            AddItem(setup.Near.Inventory, resource, 2);

            check(setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), $"{resource} did not enable planting.");
            setup.Player.UpdatePlacement(true, 0f);

            check(setup.Player.PlacedPieces == 1, $"{resource} was not planted.");
            check(Count(setup.Near.Inventory, resource) == 1, $"{resource} was not consumed exactly once.");
            check(setup.Player.Inventory.Items.Count == 0, "Storage materials were moved into player inventory.");
        }
    }

    /// <summary>Inventory wins before storage, and storage is allocated nearest first for each material.</summary>
    private static void InventoryAndNearestStorageSupplyOnlyTheRemainingCost(Action<bool, string> check)
    {
        var setup = CreateSetup();
        setup.Piece.m_resources = [Requirement("Seed", 6), Requirement("CustomFertilizer", 2)];
        AddItem(setup.Player.Inventory, "Seed", 2);
        AddItem(setup.Near.Inventory, "Seed", 3);
        AddItem(setup.Far.Inventory, "Seed", 4);
        AddItem(setup.Player.Inventory, "CustomFertilizer", 1);
        AddItem(setup.Far.Inventory, "CustomFertilizer", 3);

        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1, "The multi-material plant was not placed.");
        check(Count(setup.Player.Inventory, "Seed") == 0 && Count(setup.Near.Inventory, "Seed") == 0, "Inventory or nearest storage was not used first.");
        check(Count(setup.Far.Inventory, "Seed") == 3 && Count(setup.Far.Inventory, "CustomFertilizer") == 2, "Storage supplied more than the remaining cost.");
        check(!setup.Near.m_inUse && !setup.Far.m_inUse, "A storage reservation leaked.");
    }

    /// <summary>Missing materials and duplicate requirements cannot produce a free plant or partial spending.</summary>
    private static void InsufficientAndDuplicateRequirementsDoNotSpendItems(Action<bool, string> check)
    {
        var setup = CreateSetup();
        setup.Piece.m_resources = [Requirement("Seed", 2), Requirement("Seed", 2)];
        AddItem(setup.Player.Inventory, "Seed", 1);
        AddItem(setup.Near.Inventory, "Seed", 2);

        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 0, "Duplicate requirements double-spent the same seed.");
        check(Count(setup.Player.Inventory, "Seed") == 1 && Count(setup.Near.Inventory, "Seed") == 2, "The failed plan spent materials.");
        check(_warnings.Count == 1, "A blocked resource plan was not reported.");
        setup.Piece.m_resources = [Requirement("MissingSeed", 1)];
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 0 && Count(setup.Near.Inventory, "Seed") == 2, "Missing seeds changed world or storage state.");
    }

    /// <summary>Vanilla terrain or other placement rejection leaves materials untouched and releases reservations.</summary>
    private static void FailedPlacementReleasesStorageWithoutConsumption(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 3);
        setup.Player.PlacementAllowed = false;

        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 0 && Count(setup.Near.Inventory, "Seed") == 3, "Failed placement consumed materials.");
        check(!setup.Near.m_inUse && PlacementStorageContext.Current == null, "Failed placement leaked a scope or reservation.");
    }

    /// <summary>Closed, accessible, valid, locally owned storage is required; denied and busy sources are skipped.</summary>
    private static void AccessBusyAndOwnershipFiltersArePreserved(Action<bool, string> check)
    {
        var exclusions = new Action<Container>[]
        {
            container => container.m_nview.Owner = false,
            container => container.m_inUse = true,
            container => container.Allowed = false,
            container => container.isActiveAndEnabled = false,
            container => container.m_nview.Valid = false,
            container =>
            {
                container.m_checkGuardStone = true;
                PrivateArea.Allowed = false;
            }
        };

        foreach (var exclude in exclusions)
        {
            var setup = CreateSetup();
            AddItem(setup.Near.Inventory, "Seed", 2);
            exclude(setup.Near);

            check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Ineligible storage satisfied a planting cost.");
            setup.Player.UpdatePlacement(true, 0f);

            check(setup.Player.PlacedPieces == 0 && Count(setup.Near.Inventory, "Seed") == 2, "Ineligible storage was consumed.");
        }
    }

    /// <summary>The exact radius boundary is inclusive and distance includes vertical separation from the player.</summary>
    private static void RangeIsPlayerCenteredAndConfigurable(Action<bool, string> check)
    {
        var setup = CreateSetup();
        _radius = 5f;
        AddItem(setup.Near.Inventory, "Seed", 3);
        setup.Player.transform.position = new Vector3(40f, 0f, 0f);
        setup.Near.transform.position = new Vector3(40f, 5.01f, 0f);

        check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Storage outside the sphere satisfied the cost.");
        setup.Near.transform.position = new Vector3(40f, 5f, 0f);
        Time.frameCount++;

        check(setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Storage on the radius boundary was excluded.");
        setup.Player.UpdatePlacement(true, 0f);

        check(Count(setup.Near.Inventory, "Seed") == 2, "In-range planting did not use the configured radius.");
    }

    /// <summary>Counts are inflated only inside CanBuild checks of the local player's inventory.</summary>
    private static void StorageCountsDoNotLeakToOtherQueries(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 4);
        var otherInventory = new Inventory();
        AddItem(otherInventory, "Seed", 2);

        check(Count(setup.Player.Inventory, "Seed") == 0, "An unscoped inventory count included storage.");
        setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.IsKnown);
        setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanAlmostBuild);

        check(PlacementStorageContext.Current == null, "Discovery checks created a storage scope.");
        var token = PlacementStorageContext.BeginRequirements(setup.Player);

        try
        {
            check(Count(setup.Player.Inventory, "Seed") == 4, "The scoped count did not include storage.");
            check(Count(otherInventory, "Seed") == 2 && Count(setup.Near.Inventory, "Seed") == 4, "Storage leaked into another inventory's query.");
        }
        finally
        {
            PlacementStorageContext.End(token);
        }

        check(Count(setup.Player.Inventory, "Seed") == 0, "The scope was not restored.");
    }

    /// <summary>Opening the menu immediately shows inventory and eligible storage availability for every plant.</summary>
    private static void FirstMenuOpeningRefreshesAllAvailability(Action<bool, string> check)
    {
        var setup = CreateSetup();
        var hud = new Hud();
        hud.Pieces.AddRange([
            new Piece { m_resources = [Requirement("InventorySeed", 1)] },
            new Piece { m_resources = [Requirement("StorageSeed", 1)] },
            new Piece { m_resources = [Requirement("MissingSeed", 1)] }]);
        hud.Available.AddRange([true, true, true]);
        AddItem(setup.Player.Inventory, "InventorySeed", 1);
        AddItem(setup.Near.Inventory, "StorageSeed", 1);
        hud.m_buildUi.gameObject.activeSelf = true;

        hud.UpdateBuild(setup.Player, false);

        check(hud.Available[0] && hud.Available[1] && !hud.Available[2], "First menu opening did not immediately reflect all resource counts.");
        hud.m_buildUi.gameObject.activeSelf = false;
        hud.UpdateBuild(setup.Player, false);
        setup.Near.Inventory.RemoveItem("StorageSeed", 1, -1, true);
        AddItem(setup.Near.Inventory, "MissingSeed", 1);
        hud.m_buildUi.gameObject.activeSelf = true;
        hud.UpdateBuild(setup.Player, false);

        check(hud.Available[0] && !hud.Available[1] && hud.Available[2], "Reopening did not refresh changed storage availability.");
    }

    /// <summary>Only the first frame for a supported Cultivator menu forces a full refresh.</summary>
    private static void MenuRefreshIsScopedToOpeningAndFarmTool(Action<bool, string> check)
    {
        using var hammer = PlacementStorage.Register("Hammer", () => 20f, _warnings.Add, _errors.Add);
        var setup = CreateSetup();
        var hud = new Hud();
        hud.Pieces.AddRange([
            new Piece { m_resources = [Requirement("MissingOne", 1)] },
            new Piece { m_resources = [Requirement("MissingTwo", 1)] }]);
        hud.Available.AddRange([true, true]);
        hud.m_buildUi.gameObject.activeSelf = true;
        hud.UpdateBuild(setup.Player, false);
        hud.Available[1] = true;
        hud.UpdateBuild(setup.Player, false);

        check(hud.Available[1], "The full menu was unnecessarily refreshed on every frame.");
        hud.m_buildUi.gameObject.activeSelf = false;
        hud.UpdateBuild(setup.Player, false);
        setup.Player.RightItem = Tool("Hammer");
        hud.Available[0] = true;
        hud.Available[1] = true;
        hud.m_buildUi.gameObject.activeSelf = true;
        hud.UpdateBuild(setup.Player, false);

        check(hud.Available[0] != hud.Available[1], "The farming refresh changed a non-Cultivator menu.");
        setup.Player.RightItem = Tool("Cultivator");
        hud.UpdateBuild(setup.Player, false);

        check(!hud.Available[0] && !hud.Available[1], "Switching to the Cultivator did not refresh the open menu.");
    }

    /// <summary>Remote players retain incremental updates and an existing vanilla full-refresh request is preserved.</summary>
    private static void MenuRefreshPreservesVanillaAndRemoteUpdates(Action<bool, string> check)
    {
        var setup = CreateSetup();
        var remote = new Player { RightItem = Tool("Cultivator") };
        var hud = new Hud();
        hud.Pieces.AddRange([
            new Piece { m_resources = [Requirement("MissingOne", 1)] },
            new Piece { m_resources = [Requirement("MissingTwo", 1)] }]);
        hud.Available.AddRange([true, true]);
        hud.m_buildUi.gameObject.activeSelf = true;
        hud.UpdateBuild(remote, false);

        check(!hud.Available[0] && hud.Available[1], "The farming refresh changed a remote player's menu.");
        hud.UpdateBuild(setup.Player, false);
        hud.Available[0] = true;
        hud.Available[1] = true;
        hud.UpdateBuild(setup.Player, true);

        check(!hud.Available[0] && !hud.Available[1], "An existing vanilla full-refresh request was lost.");
    }

    /// <summary>FarmFromStorage alone does not supply Hammer costs, unknown tools, or remote player requirements.</summary>
    private static void OtherToolsAndRemotePlayersRemainVanilla(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 4);

        foreach (string tool in new[] { "Hammer", "Hoe", "CustomTool" })
        {
            setup.Player.RightItem.m_dropPrefab.name = tool;
            setup.Player.UpdatePlacement(true, 0f);

            check(setup.Player.PlacedPieces == 0, $"{tool} used farming storage.");
        }

        setup.Player.RightItem = null;
        check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "An unequipped player used farming storage.");
        setup.Player.RightItem = Tool("Cultivator");
        Player.m_localPlayer = new Player();

        check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "A remote player used farming storage.");
        check(Count(setup.Near.Inventory, "Seed") == 4, "Unrelated actions changed storage.");
    }

    /// <summary>The Cultivator tooltip shows carried and accessible stored counts, and the Hammer stays vanilla.</summary>
    private static void TooltipShowsCarriedAndStoredCounts(Action<bool, string> check)
    {
        var setup = CreateSetup();
        setup.Piece.m_resources = [Requirement("Seed", 2)];
        AddItem(setup.Player.Inventory, "Seed", 1);
        AddItem(setup.Near.Inventory, "Seed", 3);
        AddItem(setup.Far.Inventory, "Seed", 4);
        setup.Far.m_inUse = true;
        var hud = new Hud();
        var amount = new TMP_Text { enableAutoSizing = true, fontSize = 14f, overflowMode = TextOverflowModes.Truncate };
        var element = new Transform();
        var amountRect = new RectTransform { anchoredPosition = new Vector2(0f, -10f), rect = new Rect(0f, 0f, 40f, 16f) };
        element.AddChild(amountRect, "res_amount", amount);
        var name = new TMP_Text { fontSize = 16f, textWrappingMode = TextWrappingModes.Normal };
        element.AddChild("res_name", name);
        var stationSlot = new Transform();
        stationSlot.AddChild(new RectTransform(), "res_amount", new TMP_Text());
        hud.m_requirementItems = [element.gameObject, stationSlot.gameObject];

        hud.SetupPieceInfo(setup.Piece);
        var label = RequirementAmountText.FindLabel(element);
        var labelRect = (RectTransform)element.Find(RequirementAmountText.RaisedCountsLabelName);
        check(amount.text == "2", $"Cultivator required amount was '{amount.text}'.");
        check(label?.text == "1:3", $"Cultivator counts were '{label?.text}'.");
        check(
            !label.enableAutoSizing && label.fontSize == 14f && label.overflowMode == TextOverflowModes.Overflow
                && label.textWrappingMode == TextWrappingModes.NoWrap && label.color.b == 0f && labelRect.anchoredPosition.y == -10f
                && amountRect.anchoredPosition.y == 6f,
            "Counts label did not take the required amount's place as a fixed-size, unclipped yellow line with the amount raised.");
        check(
            name.textWrappingMode == TextWrappingModes.NoWrap && name.enableAutoSizing && name.fontSizeMax == 16f
                && name.fontSizeMin == 16f * RequirementAmountText.MinNameScale,
            "Item name was not kept on one shrinking line while the amount is raised.");
        check(InventoryGui.LastRequirementMet, "Cultivator tooltip availability ignored storage.");
        check(PlacementStorageContext.Current == null && PlacementPieceInfoPatch.DisplayScope == null, "Tooltip scope leaked.");
        check(Count(setup.Player.Inventory, "Seed") == 1, "Tooltip counts leaked into inventory queries.");

        setup.Piece.m_resources = [Requirement("Seed", 2), Requirement("Seed", 1)];
        hud.SetupPieceInfo(setup.Piece);
        check(RequirementAmountText.FindLabel(stationSlot)?.text == "1:3", "Second requirement slot showed no counts.");
        setup.Piece.m_resources = [Requirement("Seed", 2)];
        hud.SetupPieceInfo(setup.Piece);
        check(
            RequirementAmountText.FindLabel(stationSlot).text.Length == 0,
            "A slot reused for the crafting station kept the previous piece's counts.");

        setup.Near.Inventory.Items.Clear();
        hud.SetupPieceInfo(setup.Piece);
        check(label.text == "1:", $"Tooltip without stored items showed '{label.text}'.");

        setup.Player.Inventory.Items.Clear();
        hud.SetupPieceInfo(setup.Piece);
        check(
            amount.text == "2" && label.text.Length == 0 && !InventoryGui.LastRequirementMet && amountRect.anchoredPosition.y == -10f
                && name.textWrappingMode == TextWrappingModes.Normal && !name.enableAutoSizing && name.fontSize == 16f,
            $"Tooltip without any items showed '{label.text}' instead of vanilla.");

        AddItem(setup.Player.Inventory, "Seed", 1);
        hud.SetupPieceInfo(setup.Piece);
        setup.Player.RightItem.m_dropPrefab.name = "Hammer";
        hud.SetupPieceInfo(setup.Piece);
        check(
            amount.text == "2" && label.text.Length == 0,
            $"Hammer tooltip without BuildFromStorage kept Cultivator counts '{label.text}'.");
    }

    /// <summary>Free-build and no-placement-cost modes neither reserve nor consume storage.</summary>
    private static void FreeAndNoPlacementCostModesDoNotUseStorage(Action<bool, string> check)
    {
        foreach (bool noPlacementCost in new[] { false, true })
        {
            var setup = CreateSetup();
            AddItem(setup.Near.Inventory, "Seed", 2);
            setup.Player.NoPlacementCost = noPlacementCost;
            ZoneSystem.instance.NoCost = !noPlacementCost;

            setup.Player.UpdatePlacement(true, 0f);

            check(setup.Player.PlacedPieces == 1, "No-cost mode was blocked.");
            check(Count(setup.Near.Inventory, "Seed") == 2 && !setup.Near.m_inUse, "No-cost mode touched storage.");
        }
    }

    /// <summary>Loss of ownership after availability checks is revalidated before the plant is placed.</summary>
    private static void StorageChangesBeforePlacementBlockThePlan(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 2);
        var token = PlacementStorageContext.BeginPlacement(setup.Player);

        try
        {
            check(setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Initial availability failed.");
            setup.Near.m_nview.Owner = false;

            check(!setup.Player.TryPlacePiece(setup.Piece), "Changed ownership still allowed planting.");
            check(setup.Player.PlacedPieces == 0 && Count(setup.Near.Inventory, "Seed") == 2, "A stale storage plan spent seeds.");
            check(_warnings.Count == 1, "The changed source was not reported.");
        }
        finally
        {
            PlacementStorageContext.End(token);
        }
    }

    /// <summary>An unexpected storage change during consumption is logged, propagated, and cleaned up.</summary>
    private static void UnexpectedConsumptionFailureIsReportedAndReleased(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 2);
        var token = PlacementStorageContext.BeginPlacement(setup.Player);

        try
        {
            check(setup.Player.TryPlacePiece(setup.Piece), "The failure scenario did not place a plant.");
            setup.Near.m_nview.Owner = false;
            bool threw = false;

            try
            {
                setup.Player.ConsumeResources(setup.Piece.m_resources, 0, -1, 1);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            check(threw && _errors.Count == 1, "Unexpected storage failure was hidden.");
            check(Count(setup.Near.Inventory, "Seed") == 2, "The unowned source was still consumed.");
        }
        finally
        {
            PlacementStorageContext.End(token);
        }

        check(!setup.Near.m_inUse && PlacementStorageContext.Current == null, "Failure cleanup leaked a reservation.");
    }

    /// <summary>Containers created and destroyed while the mod is loaded are added and removed from discovery.</summary>
    private static void ContainerLifecycleUpdatesTheRegistry(Action<bool, string> check)
    {
        var setup = CreateSetup();
        var created = new Container();
        AddItem(created.Inventory, "Seed", 3);
        created.Awake();

        check(setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "A newly created chest was not registered.");
        created.OnDestroyed();

        check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "A removed chest remained eligible.");
    }

    /// <summary>Stored items follow vanilla world-level matching rather than counting unusable items.</summary>
    private static void WorldLevelMatchingIsPreserved(Action<bool, string> check)
    {
        var setup = CreateSetup();
        Game.m_worldLevel = 2;
        var seed = AddItem(setup.Near.Inventory, "Seed", 3);
        seed.m_worldLevel = 1;

        check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "A lower world-level item met the cost.");
        seed.m_worldLevel = 2;
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1 && Count(setup.Near.Inventory, "Seed") == 2, "Matching items were not consumed.");
    }

    /// <summary>Both clients share one patch set but route counts, costs, and radius to the equipped tool.</summary>
    private static void HammerAndCultivatorUseIndependentClients(Action<bool, string> check)
    {
        using var hammer = PlacementStorage.Register("Hammer", () => 3f, _warnings.Add, _errors.Add);
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 5);
        setup.Player.RightItem = Tool("Hammer");

        check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Hammer used the farming radius.");
        setup.Player.RightItem = Tool("Cultivator");
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1 && Count(setup.Near.Inventory, "Seed") == 4, "Both clients caused duplicate farming consumption.");
        setup.Player.RightItem = Tool("Hammer");
        setup.Near.transform.position = new Vector3(2f, 0f, 0f);
        Time.frameCount++;
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 2 && Count(setup.Near.Inventory, "Seed") == 3, "Both clients caused duplicate building consumption.");
    }

    /// <summary>Removing the farming client does not unpatch the remaining Hammer client.</summary>
    private static void UnloadingFarmLeavesHammerWorking(Action<bool, string> check)
    {
        using var hammer = PlacementStorage.Register("Hammer", () => 20f, _warnings.Add, _errors.Add);
        var setup = CreateSetup();
        check(PlacementStorage.TryGetClient(setup.Player, out var farm), "The farming client was not active.");
        farm.Dispose();
        AddItem(setup.Near.Inventory, "Seed", 4);

        setup.Player.UpdatePlacement(true, 0f);
        check(setup.Player.PlacedPieces == 0, "An unloaded farm client still supplied planting.");
        setup.Player.RightItem = Tool("Hammer");
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1 && Count(setup.Near.Inventory, "Seed") == 3, "Unloading farm disabled Hammer storage.");
    }

    /// <summary>Shared recipe and placement patches can coexist without inflating one another's resource counts.</summary>
    private static void RecipeAndPlacementCountsStayIndependent(Action<bool, string> check)
    {
        using var recipes = RecipeStorage.Register(
            true,
            _ => throw new InvalidOperationException("This count-only scenario must not start crafting."),
            StopCoroutines,
            _warnings.Add,
            _errors.Add,
            () => true,
            () => 20f);
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 3);
        NearbyStorage.Add(setup.Near);
        var station = new CraftingStation { m_name = "$piece_cauldron" };
        setup.Player.Station = station;
        var recipe = new Recipe { m_craftingStation = station, m_resources = [Requirement("Seed", 1)] };

        check(setup.Player.HaveRequirementItems(recipe, false, 1, 1), "Farming patches disabled recipe availability.");

        using (CraftingContext.Enter(setup.Player, recipe))
        {
            check(Count(setup.Player.Inventory, "Seed") == 3, "Farming double-counted storage during a recipe query.");
        }

        var token = PlacementStorageContext.BeginRequirements(setup.Player);

        try
        {
            check(Count(setup.Player.Inventory, "Seed") == 3, "Recipe patches double-counted planting storage.");
        }
        finally
        {
            PlacementStorageContext.End(token);
        }

        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1 && Count(setup.Near.Inventory, "Seed") == 2, "Recipe patches interfered with planting consumption.");
        check(Count(setup.Player.Inventory, "Seed") == 0, "Recipe or planting context leaked into ordinary counts.");
    }

    private static void StopCoroutines()
    {
    }

    /// <summary>Removing Hammer first leaves farming active and final unloading restores vanilla counts.</summary>
    private static void UnloadingHammerLeavesFarmWorking(Action<bool, string> check)
    {
        var hammer = PlacementStorage.Register("Hammer", () => 20f, _warnings.Add, _errors.Add);
        using var farm = RegisterFarm();
        hammer.Dispose();
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Seed", 4);
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1 && Count(setup.Near.Inventory, "Seed") == 3, "Unloading Hammer disabled farming.");
        farm.Dispose();
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1 && !setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Last-client disposal left storage patches active.");
    }

    /// <summary>Legacy BuildFromStorage patches are rejected instead of double-counting and double-consuming.</summary>
    private static void LegacyBuildPatchesAreRejected(Action<bool, string> check)
    {
        var legacy = new Harmony("hoose.valheim.buildfromstorage");

        try
        {
            var target = AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems), [typeof(string), typeof(int), typeof(bool)]);
            var prefix = typeof(PlantingTests).GetMethod(nameof(LegacyPrefix), BindingFlags.Static | BindingFlags.NonPublic);
            legacy.Patch(target, prefix: new HarmonyMethod(prefix));
            bool rejected = false;

            try
            {
                using var client = RegisterFarm();
            }
            catch (InvalidOperationException exception)
            {
                rejected = exception.Message.Contains("older BuildFromStorage", StringComparison.Ordinal);
            }

            check(rejected, "Legacy BuildFromStorage was not explicitly rejected.");
        }
        finally
        {
            legacy.UnpatchSelf();
        }
    }

    private static void LegacyPrefix()
    {
    }

    private static PlacementStorage RegisterFarm() => PlacementStorage.Register("Cultivator", () => _radius, _warnings.Add, _errors.Add);

    private static SetupResult CreateSetup()
    {
        PlacementStorageContext.Reset();
        PlacementStorage.Registry.Clear();
        _warnings.Clear();
        _errors.Clear();
        _radius = 20f;
        Time.frameCount++;
        Game.m_worldLevel = 0;
        PrivateArea.Allowed = true;
        ZoneSystem.instance.NoCost = false;
        var player = Player.m_localPlayer = new Player { RightItem = Tool("Cultivator"), Station = null };
        var piece = new Piece { m_resources = [Requirement("Seed", 1)] };
        player.SelectedPiece = piece;
        var near = new Container();
        var far = new Container();
        near.transform.position = new Vector3(5f, 0f, 0f);
        far.transform.position = new Vector3(10f, 0f, 0f);
        PlacementStorage.Registry.Add(near);
        PlacementStorage.Registry.Add(far);

        return new SetupResult(player, piece, near, far);
    }

    private static ItemDrop.ItemData Tool(string prefabName)
    {
        var item = new ItemDrop.ItemData();
        item.m_dropPrefab.name = prefabName;

        return item;
    }

    private static Piece.Requirement Requirement(string name, int amount)
    {
        var item = new ItemDrop();
        item.m_itemData.m_shared.m_name = name;

        return new Piece.Requirement { m_resItem = item, m_amount = amount };
    }

    private static ItemDrop.ItemData AddItem(Inventory inventory, string name, int amount)
    {
        var item = new ItemDrop.ItemData { m_stack = amount, m_worldLevel = Game.m_worldLevel };
        item.m_shared.m_name = name;
        inventory.AddItem(item);

        return item;
    }

    private static int Count(Inventory inventory, string name) => inventory.CountItems(name, -1, true);

    private sealed class SetupResult
    {
        internal SetupResult(Player player, Piece piece, Container near, Container far)
        {
            Player = player;
            Piece = piece;
            Near = near;
            Far = far;
        }

        internal Player Player { get; }

        internal Piece Piece { get; }

        internal Container Near { get; }

        internal Container Far { get; }
    }
}