using System;
using System.Linq;
using UnityEngine;
using ValheimMods.Common.InputSelection;
using ValheimMods.Testing;
using static SupplyFromStorage.Tests.SupplyTestSetup;

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
        runner.Run(ProvidersShareThePickerAndPromoteDriver);
        runner.Run(SmelterDisplayUsesProgressionWithoutChangingPreferences);
        runner.Run(DisplayOrderRetainsModdedInputsAndOtherStations);
        runner.Run(PreviewShowsOnlyCurrentCharacterDiscoveries);
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

    /// <summary>Smelter tiles follow biome progression without replacing saved automatic priorities.</summary>
    private static void SmelterDisplayUsesProgressionWithoutChangingPreferences(Action<bool, string> check)
    {
        var items = new[] { "CopperOre", "IronOre", "IronScrap", "BronzeScrap", "TinOre", "SilverOre", "CopperScrap" }
            .Select(name => new InputPreviewItem(new ItemDrop { name = name }, 0, 0)).ToArray();
        var preferences = InputPreferences.Parse("IronScrap,TinOre|CopperOre");
        var ordered = ProcessorInputSelection.OrderPreviewItems("$piece_smelter", items).ToArray();
        check(ordered.Select(entry => entry.Item.name).SequenceEqual(
            new[] { "CopperOre", "TinOre", "IronOre", "SilverOre", "CopperScrap", "BronzeScrap", "IronScrap" }),
            "Smelter tiles did not follow the requested progression.");
        check(preferences.Choose(ordered.Select(entry => entry.Item.name)) == "IronScrap"
            && preferences.Serialize() == "IronScrap,TinOre|CopperOre",
            "Display ordering changed the saved automatic-input policy.");
    }

    /// <summary>Each processor uses its own display order and preserves additional inputs after the known types.</summary>
    private static void DisplayOrderRetainsModdedInputsAndOtherStations(Action<bool, string> check)
    {
        var cases = new[]
        {
            (Station: "$piece_smelter", Order: new[] { "CopperOre", "TinOre", "IronOre", "SilverOre", "CopperScrap", "BronzeScrap", "IronScrap" }),
            (Station: "$piece_blastfurnace", Order: new[] { "BlackMetalScrap", "FlametalOreNew", "GoldOre" }),
            (Station: "$piece_charcoalkiln", Order: new[] { "Wood", "FineWood", "RoundLog" }),
            (Station: "$tool_batteringram", Order: new[] { "Wood", "FineWood", "RoundLog", "Blackwood" })
        };

        foreach (var scenario in cases)
        {
            var items = new[] { "ModdedInputB" }.Concat(scenario.Order.Reverse()).Concat(new[] { "ModdedInputA" })
                .Select(name => new InputPreviewItem(new ItemDrop { name = name }, 0, 0)).ToArray();
            var ordered = ProcessorInputSelection.OrderPreviewItems(scenario.Station, items).ToArray();
            check(ordered.Select(entry => entry.Item.name).SequenceEqual(
                scenario.Order.Concat(new[] { "ModdedInputB", "ModdedInputA" })),
                $"{scenario.Station} display order was incorrect or lost additional runtime inputs.");
            check(ordered.All(items.Contains) && ordered.Length == items.Length,
                $"{scenario.Station} display ordering replaced preview entries.");
            check(ProcessorInputSelection.OrderPreviewItems("$piece_unsupported", items).SequenceEqual(items),
                "Ordering changed an unsupported station's inputs.");
        }
    }

    private static void ProvidersShareThePickerAndPromoteDriver(Action<bool, string> check)
    {
        var registry = new InputPickerProviderRegistry();
        var player = new Player();
        var cookSnapshot = new InputPreviewSnapshot(
            new Transform(),
            "cooking station",
            Array.Empty<InputPreviewItem>());
        var supply = new InputPickerProvider(
            () => true,
            () => KeyCode.F8,
            _ => null,
            null);
        var cook = new InputPickerProvider(
            () => true,
            () => KeyCode.F8,
            _ => cookSnapshot,
            new InputPickerControls(
                () => true,
                () => InputPreferences.Parse("|"),
                _ => { },
                () => null,
                _ => { },
                (_, _) => { },
                () => "F8",
                () => null,
                () => false));
        registry.Add(supply);
        registry.Add(cook);

        var target = registry.FindOpenTarget(player, key => key == KeyCode.F8, out var snapshot, out bool attempted);
        check(attempted && target == cook && snapshot == cookSnapshot,
            "The shared picker did not route the open key to the provider matching the hovered input.");
        check(registry.Driver == supply && registry.FindHintTarget(player, out var hint) == cook
            && hint == cookSnapshot, "The provider registry did not choose the matching hint source.");
        registry.Remove(supply);
        check(registry.Driver == cook, "Removing the picker driver did not promote the remaining provider.");
        registry.Remove(cook);
        check(registry.IsEmpty && registry.Driver == null, "Removing all clients retained picker providers.");
    }

    private static void PreviewShowsOnlyCurrentCharacterDiscoveries(Action<bool, string> check)
    {
        var cases = new[]
        {
            (Station: "$piece_charcoalkiln", Inputs: new[] { "Wood", "FineWood", "RoundLog" }),
            (Station: "$piece_smelter", Inputs: new[] { "CopperOre", "TinOre", "IronOre" }),
            (Station: "$piece_blastfurnace", Inputs: new[] { "BlackMetalScrap", "FlametalOreNew", "GoldOre" }),
            (Station: "$tool_batteringram", Inputs: new[] { "Wood", "FineWood", "Blackwood" })
        };

        foreach (var scenario in cases)
        {
            var player = Reset();
            player.KnownMaterials.Clear();
            var station = new Smelter { m_name = scenario.Station, m_addOreSwitch = new Switch() };

            foreach (string type in scenario.Inputs)
            {
                station.m_conversion.Add(new Smelter.ItemConversion { m_from = StationDrop(type, "$item_" + type) });
            }

            SupplyFromStoragePlugin.InputSelectionEnabled = true;
            player.HoverObject = new GameObject { ParentComponents = new Component[] { station, station.m_addOreSwitch } };
            var unknown = Item("$item_" + scenario.Inputs[1], 3);
            unknown.m_dropPrefab = new GameObject(scenario.Inputs[1]);
            Chest(1, 0, 0, unknown);
            var carried = Item(unknown.m_shared.m_name, 2);
            carried.m_dropPrefab = new GameObject(scenario.Inputs[1]);
            player.Inventory.Items.Add(carried);
            var snapshot = ProcessorInputPreview.Read(player);
            check(snapshot != null && snapshot.Items.Count == 0,
                $"{scenario.Station} exposed an undiscovered inventory/storage item.");
            player.KnownMaterials.UnionWith(new[] { "$item_" + scenario.Inputs[0], "$item_" + scenario.Inputs[2] });
            var preferences = InputPreferences.Parse(string.Join(",", scenario.Inputs) + "|" + scenario.Inputs[1]);
            snapshot = ProcessorInputPreview.Read(player);
            check(snapshot.Items.Count == 2 && snapshot.Items.All(entry => !entry.IsAvailable),
                $"{scenario.Station} hid a discovered zero-count input or exposed an unknown type.");
            var accepted = snapshot.Items.Select(entry => entry.Item.name).ToArray();
            var edited = preferences.MoveDown(scenario.Inputs[0], accepted);
            check(edited.Order(accepted).SequenceEqual(accepted.Reverse())
                && edited.IsExcluded(scenario.Inputs[1])
                && edited.Serialize().Split('|')[0].Split(',')[1] == scenario.Inputs[1],
                "Visible priority editing lost or moved a hidden preference.");
            player.KnownMaterials.Add("$item_" + scenario.Inputs[1]);
            snapshot = ProcessorInputPreview.Read(player);
            check(snapshot.Items.Count == 3 && snapshot.Items.Single(entry => entry.Item.name == scenario.Inputs[1]).StorageCount == 3
                && edited.IsExcluded(scenario.Inputs[1]), "Discovery did not reveal the input with its saved exclusion.");
            var other = new Player { HoverObject = player.HoverObject };
            check(ProcessorInputPreview.Read(other).Items.Count == 0, "One character inherited another character's discoveries.");
            check(unknown.m_stack == 3 && carried.m_stack == 2, "Preview/discovery changes consumed an input.");
        }
    }
}