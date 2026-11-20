using System.Linq;
using UnityEngine;
using ValheimMods.Common.InputSelection;

namespace CookFromStorage;

internal static class CookingInputPreview
{
    internal static Component GetStation(Player player)
    {
        if (player == null || player.GetInventory() == null || !CookFromStoragePlugin.InputSelectionEnabled)
        {
            return null;
        }

        var hover = player.GetHoverObject();

        if (hover == null)
        {
            return null;
        }

        Component station = hover.GetComponentInParent<Fermenter>();

        if (station == null)
        {
            var cooking = hover.GetComponentInParent<CookingStation>();
            var hoveredSwitch = hover.GetComponentInParent<Switch>();

            if (cooking == null || hoveredSwitch != cooking.m_addFoodSwitch)
            {
                return null;
            }

            station = cooking;
        }

        return CookingInputSelection.Supports(station) && CookingInputSelection.CanUse(station, player) ? station : null;
    }

    internal static InputPreviewSnapshot Read(Player player)
    {
        var station = GetStation(player);

        if (station == null)
        {
            return null;
        }

        var sources = NearbyStorage.Find(player);
        var items = CookingInputSelection.Inputs(station)
            .Where(item => player.IsMaterialKnown(item.m_itemData.m_shared.m_name))
            .Select(item => new InputPreviewItem(
                item,
                InputPreviewItem.Count(player.GetInventory(), item.name),
                sources.Where(source => StorageInputTransfer.TryGetInventory(source, player, out _))
                    .Sum(source => InputPreviewItem.Count(source.GetInventory(), item.name)))).ToArray();

        return new InputPreviewSnapshot(station, CookingInputSelection.StationType(station), items);
    }
}