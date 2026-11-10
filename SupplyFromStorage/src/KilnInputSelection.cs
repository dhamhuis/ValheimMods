using System.Linq;
using ValheimMods.Common.InputSelection;

namespace SupplyFromStorage;

internal static class KilnInputSelection
{
    internal static InputTypeSession Session { get; } = new();

    internal static InputPreferences Preferences { get; set; }

    internal static bool TryHandle(Smelter station, Player player, out bool result)
    {
        result = false;

        if (!SupplyFromStoragePlugin.InputSelectionEnabled || station?.m_name != "$piece_charcoalkiln")
        {
            return false;
        }

        if (!Session.CanInsert || !ReferenceEquals(Session.Context, station))
        {
            return true;
        }

        if (Preferences == null)
        {
            Notify(player, "Kiln preferences are invalid or unavailable. Automatic insertion is blocked; check the log/config.");

            return true;
        }

        var sources = NearbyStorage.Find(player);
        var available = station.m_conversion == null ? Enumerable.Empty<string>()
            : station.m_conversion.Where(conversion => conversion?.m_from != null)
                .Select(conversion => conversion.m_from.name)
                .Where(name => !string.IsNullOrEmpty(name) && station.IsItemAllowed(name))
                .Where(name => InputPreviewItem.Count(player.GetInventory(), name) > 0
                    || sources.Any(source => NearbyStorage.IsAccessible(source, player) && source.IsOwner()
                        && InputPreviewItem.Count(source.GetInventory(), name) > 0));
        string chosen = Session.Resolve(Preferences, available);

        if (chosen == null)
        {
            Notify(player, $"No allowed kiln input is available. Use {SupplyFromStoragePlugin.InputPickerKeyName} to choose a type or edit automatic preferences.");

            return true;
        }

        var transfer = SupplyTransfer.TransferSelectedInput(station, player, sources, chosen);
        result = transfer == SupplyTransfer.TransferResult.Transferred;

        if (!result)
        {
            string message = transfer == SupplyTransfer.TransferResult.Full ? "$msg_itsfull"
                : transfer == SupplyTransfer.TransferResult.NoItems ? $"No {chosen} available. Release use before trying again."
                : "Kiln input stopped: station or source is unavailable. Release use before trying again.";
            Notify(player, message);
        }

        return true;
    }

    private static void Notify(Player player, string message)
    {
        if (Session.Stop())
        {
            player.Message(MessageHud.MessageType.Center, message, 0, null, false);
        }
    }
}