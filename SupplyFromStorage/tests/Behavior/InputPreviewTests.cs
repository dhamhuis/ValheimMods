using System;
using UnityEngine;
using ValheimMods.Common.InputSelection;
using ValheimMods.Testing;

namespace SupplyFromStorage.Tests;

internal static class InputPreviewTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(ClosedPreviewDoesNotBlockInput);
        runner.Run(OpenPreviewBlocksUntilClosedAndReleased);
        runner.Run(RepeatedCloseDoesNotExtendReleaseGuard);
        runner.Run(ReopeningDoesNotInheritAnOldRelease);
        runner.Run(CountsUseExactPrefabIdentityWithoutMutation);
        runner.Run(UnavailableItemsAreNotSelectable);
        runner.Run(InvalidCountRequestsFailExplicitly);
    }

    /// <summary>A registered but unopened preview leaves game input unchanged.</summary>
    private static void ClosedPreviewDoesNotBlockInput(Action<bool, string> check)
    {
        var session = new InputPreviewSession();
        session.UpdateRelease(10, true);
        check(!session.IsOpen && !session.BlocksInput, "An unopened preview blocked gameplay.");
    }

    /// <summary>Closing cannot leak same-frame clicks or held use/attack inputs into gameplay.</summary>
    private static void OpenPreviewBlocksUntilClosedAndReleased(Action<bool, string> check)
    {
        var session = new InputPreviewSession();
        session.Open(10);
        check(!session.CanProcessInput(10) && session.CanProcessInput(11),
            "The opening key could immediately close the preview in the same frame.");
        session.UpdateRelease(11, false);
        check(session.IsOpen && session.BlocksInput && session.OpenedFrame == 10, "Open state lost input capture.");
        session.Close(12);
        session.UpdateRelease(12, false);
        check(!session.IsOpen && session.BlocksInput, "Closing leaked same-frame input.");
        session.UpdateRelease(13, true);
        check(session.BlocksInput, "Held controls were released into gameplay.");
        session.UpdateRelease(14, false);
        check(!session.BlocksInput, "Released controls remained blocked.");
    }

    /// <summary>Repeated cancellation calls do not move the release deadline indefinitely.</summary>
    private static void RepeatedCloseDoesNotExtendReleaseGuard(Action<bool, string> check)
    {
        var session = new InputPreviewSession();
        session.Open(1);
        session.Close(2);
        session.Close(3);
        session.UpdateRelease(3, false);
        check(!session.BlocksInput, "Repeated close extended the input guard.");
    }

    /// <summary>A new open session keeps capture even after an earlier session was released.</summary>
    private static void ReopeningDoesNotInheritAnOldRelease(Action<bool, string> check)
    {
        var session = new InputPreviewSession();
        session.Open(1);
        session.Close(2);
        session.UpdateRelease(3, false);
        session.Open(4);
        session.UpdateRelease(5, false);
        check(session.IsOpen && session.BlocksInput && session.OpenedFrame == 4, "Reopening inherited stale release state.");
    }

    /// <summary>Same-name impostors, invalid items and empty stacks do not inflate an exact-type count.</summary>
    private static void CountsUseExactPrefabIdentityWithoutMutation(Action<bool, string> check)
    {
        var inventory = new Inventory();
        inventory.Items.Add(new ItemDrop.ItemData { m_dropPrefab = new GameObject("Wood"), m_stack = 3 });
        inventory.Items.Add(new ItemDrop.ItemData { m_dropPrefab = new GameObject("Wood"), m_stack = 2 });
        inventory.Items.Add(new ItemDrop.ItemData { m_dropPrefab = new GameObject("FineWood"), m_stack = 7 });
        inventory.Items.Add(new ItemDrop.ItemData { m_dropPrefab = new GameObject("Wood"), m_stack = 0 });
        inventory.Items.Add(new ItemDrop.ItemData { m_dropPrefab = null, m_stack = 20 });
        inventory.Items.Add(null);
        int count = InputPreviewItem.Count(inventory, "Wood");
        check(count == 5, "Preview count did not use exact prefab identities and positive stacks.");
        check(inventory.Changes == 0 && inventory.Items.Count == 6 && inventory.Items[0].m_stack == 3,
            "Reading the preview mutated the inventory.");
    }

    /// <summary>Accepted types with no eligible source remain displayable but not available for selection.</summary>
    private static void UnavailableItemsAreNotSelectable(Action<bool, string> check)
    {
        var item = new ItemDrop();
        check(!new InputPreviewItem(item, 0, 0).IsAvailable, "A zero-count type was available.");
        check(new InputPreviewItem(item, 1, 0).IsAvailable, "Inventory availability was ignored.");
        check(new InputPreviewItem(item, 0, 1).IsAvailable, "Storage availability was ignored.");
    }

    /// <summary>Invalid counter inputs are errors, not misleading empty inventories.</summary>
    private static void InvalidCountRequestsFailExplicitly(Action<bool, string> check)
    {
        bool rejected = false;

        try
        {
            InputPreviewItem.Count(null, "Wood");
        }
        catch (ArgumentException)
        {
            rejected = true;
        }

        check(rejected, "A missing inventory produced a success-shaped empty count.");
    }
}