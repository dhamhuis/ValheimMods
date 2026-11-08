using System.Linq;
using ValheimMods.Common;

namespace CookFromStorage;

internal static class FermenterTransfer
{
    internal static bool TryHandleRequest(Fermenter station, Humanoid user, bool hold)
    {
        if (hold || !CookFromStoragePlugin.AutomaticIngredientsEnabled
            || user != Player.m_localPlayer || user is not Player player
            || player.GetInventory() == null || !CanUse(station, player)
            || station.GetStatus() != Fermenter.Status.Empty
            || station.FindCookableItem(player.GetInventory()) != null
            || !PrivateArea.CheckAccess(station.transform.position, 0f, false, false))
        {
            return false;
        }

        station.UpdateCover(0f, true);

        if (!station.m_hasRoof || station.m_exposed)
        {
            return false;
        }

        foreach (var container in NearbyStorage.Find(player))
        {
            if (!StorageInputTransfer.TryGetInventory(container, player, out var inventory))
            {
                continue;
            }

            foreach (var item in inventory.GetAllItems().ToArray())
            {
                if (item?.m_shared == null || item.m_stack <= 0 || item.m_dropPrefab == null
                    || string.IsNullOrEmpty(item.m_dropPrefab.name) || !station.IsItemAllowed(item))
                {
                    continue;
                }

                int nameHash = item.m_dropPrefab.name.GetStableHashCode();

                return StorageInputTransfer.ConsumeOne(
                    container,
                    inventory,
                    player,
                    item,
                    "Fermenter",
                    () => CanUse(station, player) && station.GetStatus() == Fermenter.Status.Empty
                        && station.m_hasRoof && !station.m_exposed && station.IsItemAllowed(item)
                        && PrivateArea.CheckAccess(station.transform.position, 0f, false, false),
                    () => station.RPC_AddItem(player.GetPlayerID(), nameHash, item.m_cheated),
                    () => station.GetContent() == nameHash);
            }
        }

        return false;
    }

    private static bool CanUse(Fermenter station, Player player)
    {
        return station != null && station.isActiveAndEnabled && station.m_nview != null
            && station.m_nview.IsValid() && station.m_nview.IsOwner()
            && station.m_conversion != null && player != null
            && (station.transform.position - player.transform.position).sqrMagnitude <= StorageRange.StationReach * StorageRange.StationReach;
    }
}