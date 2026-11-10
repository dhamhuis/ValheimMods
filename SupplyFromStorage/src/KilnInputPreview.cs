using System.Collections.Generic;
using ValheimMods.Common;
using ValheimMods.Common.InputSelection;

namespace SupplyFromStorage;

internal static class KilnInputPreview
{
    internal static Smelter GetStation(Player player)
    {
        if (player == null || player.GetInventory() == null)
        {
            return null;
        }

        var hover = player.GetHoverObject();
        var station = hover != null ? hover.GetComponentInParent<Smelter>() : null;

        if (station == null || station.m_name != "$piece_charcoalkiln" || !station.isActiveAndEnabled
            || station.m_nview == null || !station.m_nview.IsValid() || !station.m_nview.IsOwner()
            || station.m_addOreSwitch == null || hover.GetComponentInParent<Switch>() != station.m_addOreSwitch
            || station.m_conversion == null
            || (station.transform.position - player.transform.position).sqrMagnitude > StorageRange.StationReach * StorageRange.StationReach
            || !PrivateArea.CheckAccess(station.transform.position, 0f, false, false))
        {
            return null;
        }

        return station;
    }

    internal static InputPreviewSnapshot Read(Player player)
    {
        var station = GetStation(player);

        if (station == null)
        {
            return null;
        }

        var sources = NearbyStorage.Find(player);
        var items = new List<InputPreviewItem>();
        var seen = new HashSet<string>();

        foreach (var conversion in station.m_conversion)
        {
            var item = conversion?.m_from;

            if (item?.m_itemData?.m_shared == null || string.IsNullOrEmpty(item.name)
                || !station.IsItemAllowed(item.name) || !seen.Add(item.name))
            {
                continue;
            }

            int stored = 0;

            foreach (var source in sources)
            {
                if (NearbyStorage.IsAccessible(source, player) && source.IsOwner())
                {
                    stored += InputPreviewItem.Count(source.GetInventory(), item.name);
                }
            }

            items.Add(new InputPreviewItem(item, InputPreviewItem.Count(player.GetInventory(), item.name), stored));
        }

        return items.Count > 0 ? new InputPreviewSnapshot(station, station.m_name, items) : null;
    }
}