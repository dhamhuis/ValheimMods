using System;
using System.Collections.Generic;
using System.Linq;
using ValheimMods.Common.InputSelection;

namespace SupplyFromStorage;

internal static class ProcessorInputSelection
{
    private static readonly string[] _smelterDisplayOrder =
    {
        "CopperOre", "TinOre", "IronOre", "SilverOre", "CopperScrap", "BronzeScrap", "IronScrap"
    };
    private static readonly string[] _smelterDefaultPriority =
    {
        "CopperOre", "CopperScrap", "TinOre", "BronzeScrap", "IronOre", "IronScrap", "SilverOre"
    };
    private static readonly string[] _blastFurnaceDefaultPriority =
    {
        "BlackMetalScrap", "FlametalOreNew", "GoldOre"
    };
    private static readonly string[] _kilnDefaultPriority =
    {
        "Wood", "FineWood", "RoundLog"
    };
    private static readonly string[] _ramDefaultPriority =
    {
        "Wood", "FineWood", "RoundLog", "Blackwood"
    };

    internal static InputTypeSession Session { get; } = new(continueAcrossTargets: true);

    internal static InputPreferences Preferences { get; set; }

    internal static string PreferenceStationType { get; set; }

    internal static string PreferenceSection(string stationType)
    {
        return stationType switch
        {
            "$piece_charcoalkiln" => "Charcoal Kiln preferences",
            "$piece_smelter" => "Smelter preferences",
            "$piece_blastfurnace" => "Blast Furnace preferences",
            "$tool_batteringram" => "Battering Ram preferences",
            _ => null
        };
    }

    internal static string DefaultPreferences(string stationType)
    {
        return string.Join(",", GetDefaultPriority(stationType)) + "|";
    }

    internal static bool Supports(string stationType) => PreferenceSection(stationType) != null;

    internal static bool IsDiscovered(Smelter station, Player player, string prefabName)
    {
        string sharedName = station.m_conversion?
            .FirstOrDefault(conversion => conversion?.m_from?.name == prefabName)?
            .m_from?.m_itemData?.m_shared?.m_name;

        return !string.IsNullOrEmpty(sharedName) && player.IsMaterialKnown(sharedName);
    }

    internal static IEnumerable<InputPreviewItem> OrderPreviewItems(string stationType, IEnumerable<InputPreviewItem> items)
    {
        var displayOrder = stationType == "$piece_smelter" ? _smelterDisplayOrder : GetDefaultPriority(stationType);

        if (displayOrder.Length == 0)
        {
            return items;
        }

        return items.OrderBy(entry =>
        {
            int index = Array.IndexOf(displayOrder, entry.Item.name);

            return index < 0 ? int.MaxValue : index;
        });
    }

    internal static bool TryHandle(Smelter station, Player player, out bool result)
    {
        result = false;

        if (!SupplyFromStoragePlugin.InputSelectionEnabled || !Supports(station?.m_name))
        {
            return false;
        }

        if (!Session.CanInsert || !ReferenceEquals(Session.Context, station))
        {
            return true;
        }

        if (Preferences == null || PreferenceStationType != station.m_name)
        {
            Notify(player, "Station preferences are invalid or unavailable. Automatic insertion is blocked; check the log/config.");

            return true;
        }

        var sources = NearbyStorage.Find(player);
        var available = (station.m_conversion ?? Enumerable.Empty<Smelter.ItemConversion>())
            .Where(conversion => conversion?.m_from != null)
            .Select(conversion => conversion.m_from.name)
            .Where(name => !string.IsNullOrEmpty(name) && station.IsItemAllowed(name)
                && IsDiscovered(station, player, name))
            .Where(name => InputPreviewItem.Count(player.GetInventory(), name) > 0
                || sources.Any(source => NearbyStorage.IsAccessible(source, player) && source.IsOwner()
                    && InputPreviewItem.Count(source.GetInventory(), name) > 0))
            .ToList();
        string chosen = Session.Resolve(Preferences, available);

        if (chosen == null)
        {
            // Match vanilla when nothing accepted exists; only explain when Never auto-add blocked real items.
            Notify(player, available.Count == 0 ? "$msg_noprocessableitems"
                : $"Items set to Never auto-add ({SupplyFromStoragePlugin.InputPickerKeyName})");

            return true;
        }

        if (!IsDiscovered(station, player, chosen))
        {
            Notify(player, "Selected input is not discovered or no longer accepted. Release use before trying again.");

            return true;
        }

        var transfer = SupplyTransfer.TransferSelectedInput(station, player, sources, chosen);
        result = transfer == SupplyTransfer.TransferResult.Transferred;

        if (!result)
        {
            string message = transfer == SupplyTransfer.TransferResult.Full ? "$msg_itsfull"
                : transfer == SupplyTransfer.TransferResult.NoItems
                    ? $"No {GetInputDisplayName(station, chosen)} available. Release use before trying again."
                : "Station input stopped: station or source is unavailable. Release use before trying again.";
            Notify(player, message);
        }

        return true;
    }

    private static string[] GetDefaultPriority(string stationType)
    {
        return stationType switch
        {
            "$piece_smelter" => _smelterDefaultPriority,
            "$piece_blastfurnace" => _blastFurnaceDefaultPriority,
            "$piece_charcoalkiln" => _kilnDefaultPriority,
            "$tool_batteringram" => _ramDefaultPriority,
            _ => Array.Empty<string>()
        };
    }

    private static string GetInputDisplayName(Smelter station, string prefabName)
    {
        string localizationKey = station.m_conversion?
            .FirstOrDefault(conversion => conversion?.m_from?.name == prefabName)?
            .m_from?.m_itemData?.m_shared?.m_name;

        return string.IsNullOrEmpty(localizationKey) ? "selected item" : Localization.instance.Localize(localizationKey);
    }

    private static void Notify(Player player, string message)
    {
        if (Session.Stop())
        {
            player.Message(MessageHud.MessageType.Center, message, 0, null, false);
        }
    }
}