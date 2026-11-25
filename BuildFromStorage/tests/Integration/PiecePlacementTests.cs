using System;
using HarmonyLib;
using UnityEngine;
using ValheimMods.Testing;

namespace BuildFromStorage.Tests;

internal static class PiecePlacementTests
{
    internal static void Run(TestRunner runner)
    {
        var harmony = new Harmony("hoose.valheim.buildfromstorage.tests");

        try
        {
            harmony.PatchAll(typeof(PiecePlacementTests).Assembly);
            runner.Run(StorageMaterialsAreScopedAndConsumedInventoryFirst);
            runner.Run(UnownedAndOutOfRangeStorageCannotMeetCosts);
            runner.Run(DuplicateRequirementsCannotDoubleSpendStorage);
            runner.Run(NoPlacementCostDoesNotConsumeStorage);
            runner.Run(FreeBuildDoesNotReserveOrConsumeStorage);
        }
        finally
        {
            harmony.UnpatchSelf();
            BuildStorageContext.Reset();
        }
    }

    private static void StorageMaterialsAreScopedAndConsumedInventoryFirst(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Player.Inventory, "Wood", 2);
        AddItem(setup.Near.Inventory, "Wood", 3);
        AddItem(setup.Far.Inventory, "Wood", 4);
        setup.Piece.m_resources = [Requirement("Wood", 6)];

        check(setup.Player.Inventory.CountItems("Wood", -1, true) == 2, "Storage was added outside a build requirement check.");
        check(setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Nearby storage did not satisfy the Hammer requirement check.");
        check(setup.Player.Inventory.CountItems("Wood", -1, true) == 2, "Storage count leaked after the requirement check.");

        int countDuringPlacement = -1;
        setup.Player.OnPlacementStart = () => countDuringPlacement = setup.Player.Inventory.CountItems("Wood", -1, true);
        setup.Player.UpdatePlacement(true, 0f);

        check(countDuringPlacement == 2, "Storage counts leaked outside the scoped build-cost check.");
        check(setup.Player.PlacedPieces == 1, "The piece was not placed.");
        check(setup.Player.Inventory.CountItems("Wood", -1, true) == 0, "Player inventory was not consumed first.");
        check(setup.Near.GetInventory().CountItems("Wood", -1, true) == 0, "Nearest storage was not consumed first.");
        check(setup.Far.GetInventory().CountItems("Wood", -1, true) == 3, "Farther storage supplied more than the remaining cost.");
        check(!setup.Near.m_inUse && !setup.Far.m_inUse, "Storage remained reserved after placement.");
    }

    private static void UnownedAndOutOfRangeStorageCannotMeetCosts(Action<bool, string> check)
    {
        var setup = CreateSetup();
        setup.Near.m_nview.Owner = false;
        setup.Far.transform.position = new Vector3(21f, 0f, 0f);
        AddItem(setup.Near.Inventory, "Stone", 10);
        AddItem(setup.Far.Inventory, "Stone", 10);
        setup.Piece.m_resources = [Requirement("Stone", 1)];

        check(!setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "Unowned or out-of-range storage satisfied a build cost.");
        check(setup.Player.Inventory.CountItems("Stone", -1, true) == 0, "Storage was counted outside the scoped requirement check.");
    }

    private static void DuplicateRequirementsCannotDoubleSpendStorage(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Player.Inventory, "Stone", 1);
        AddItem(setup.Near.Inventory, "Stone", 2);
        setup.Piece.m_resources = [Requirement("Stone", 2), Requirement("Stone", 2)];

        check(setup.Player.HaveRequirements(setup.Piece, Player.RequirementMode.CanBuild), "The test did not reach the vanilla per-requirement check.");
        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 0, "Insufficient duplicate resources allowed a piece to be placed.");
        check(setup.Player.Inventory.CountItems("Stone", -1, true) == 1, "The failed placement consumed player materials.");
        check(setup.Near.GetInventory().CountItems("Stone", -1, true) == 2, "The failed placement consumed storage materials.");
        check(BuildFromStoragePlugin.Warnings.Count > 0, "The blocked placement did not report the resource-plan failure.");
    }

    private static void NoPlacementCostDoesNotConsumeStorage(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Wood", 5);
        setup.Piece.m_resources = [Requirement("Wood", 5)];
        setup.Player.NoPlacementCost = true;

        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1, "The no-placement-cost build was blocked.");
        check(setup.Near.GetInventory().CountItems("Wood", -1, true) == 5, "No-placement-cost mode consumed storage materials.");
        check(!setup.Near.m_inUse, "No-placement-cost mode reserved storage.");
    }

    private static void FreeBuildDoesNotReserveOrConsumeStorage(Action<bool, string> check)
    {
        var setup = CreateSetup();
        AddItem(setup.Near.Inventory, "Wood", 5);
        setup.Piece.m_resources = [Requirement("Wood", 5)];
        ZoneSystem.instance.NoCost = true;

        setup.Player.UpdatePlacement(true, 0f);

        check(setup.Player.PlacedPieces == 1, "Free-build mode blocked the piece.");
        check(setup.Near.GetInventory().CountItems("Wood", -1, true) == 5, "Free-build mode consumed storage materials.");
        check(!setup.Near.m_inUse, "Free-build mode reserved storage.");
    }

    private static SetupResult CreateSetup()
    {
        BuildStorageContext.Reset();
        BuildFromStoragePlugin.Warnings.Clear();
        BuildFromStoragePlugin.Errors.Clear();
        BuildFromStoragePlugin.StorageRadius = 20f;
        BuildFromStoragePlugin.StorageRegistry = new BuildStorageRegistry();
        Time.frameCount++;
        ZoneSystem.instance.NoCost = false;

        var player = Player.m_localPlayer = new Player();
        var piece = new Piece();
        player.SelectedPiece = piece;
        var near = new Container();
        var far = new Container();
        near.transform.position = new Vector3(5f, 0f, 0f);
        far.transform.position = new Vector3(10f, 0f, 0f);
        BuildFromStoragePlugin.StorageRegistry.Add(near);
        BuildFromStoragePlugin.StorageRegistry.Add(far);

        return new SetupResult(player, piece, near, far);
    }

    private static Piece.Requirement Requirement(string name, int amount)
    {
        var item = new ItemDrop();
        item.m_itemData.m_shared.m_name = name;

        return new Piece.Requirement
        {
            m_resItem = item,
            m_amount = amount
        };
    }

    private static void AddItem(Inventory inventory, string name, int amount)
    {
        var item = new ItemDrop.ItemData
        {
            m_stack = amount,
            m_worldLevel = Game.m_worldLevel
        };
        item.m_shared.m_name = name;
        inventory.AddItem(item);
    }

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