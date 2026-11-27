using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimMods.Common.Placement;
using ValheimMods.Testing;

namespace BuildFromStorage.Tests;

internal static class PiecePlacementTests
{
    private static readonly List<string> _warnings = new();
    private static readonly List<string> _errors = new();

    internal static void Run(TestRunner runner)
    {
        using var client = PlacementStorage.Register("Hammer", () => 20f, _warnings.Add, _errors.Add);

        try
        {
            runner.Run(StorageMaterialsAreScopedAndConsumedInventoryFirst);
            runner.Run(UnownedAndOutOfRangeStorageCannotMeetCosts);
            runner.Run(DuplicateRequirementsCannotDoubleSpendStorage);
            runner.Run(NoPlacementCostDoesNotConsumeStorage);
            runner.Run(FreeBuildDoesNotReserveOrConsumeStorage);
        }
        finally
        {
            PlacementStorageContext.Reset();
        }
    }

    /// <summary>Build costs include storage only in requirement scopes and consume inventory before nearest storage.</summary>
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

    /// <summary>Remote-owned and out-of-range storage cannot satisfy Hammer costs.</summary>
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

    /// <summary>Duplicate resources are allocated once and insufficient totals block placement without spending.</summary>
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
        check(_warnings.Count > 0, "The blocked placement did not report the resource-plan failure.");
    }

    /// <summary>No-placement-cost mode leaves storage contents and reservations unchanged.</summary>
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

    /// <summary>Free-build mode preserves storage while allowing placement.</summary>
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
        PlacementStorageContext.Reset();
        PlacementStorage.Registry.Clear();
        _warnings.Clear();
        _errors.Clear();
        Time.frameCount++;
        ZoneSystem.instance.NoCost = false;

        var player = Player.m_localPlayer = new Player();
        player.RightItem = new ItemDrop.ItemData();
        player.RightItem.m_dropPrefab.name = "Hammer";
        var piece = new Piece();
        player.SelectedPiece = piece;
        var near = new Container();
        var far = new Container();
        near.transform.position = new Vector3(5f, 0f, 0f);
        far.transform.position = new Vector3(10f, 0f, 0f);
        PlacementStorage.Registry.Add(near);
        PlacementStorage.Registry.Add(far);

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