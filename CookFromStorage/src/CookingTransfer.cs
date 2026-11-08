using System;
using System.Collections.Generic;
using System.Linq;
using ValheimMods.Common;

namespace CookFromStorage;

internal static class CookingTransfer
{
    internal static bool TryHandleFoodRequest(
        CookingStation station,
        Humanoid user,
        out bool handledResult)
    {
        handledResult = false;

        if (!CookFromStoragePlugin.AutomaticIngredientsEnabled
            || user != Player.m_localPlayer || user is not Player player
            || player.GetInventory() == null || station == null
            || station.m_name == "$piece_frostfoundry"
            || station.m_conversion == null)
        {
            return false;
        }

        if (!CanUse(station, player))
        {
            bool networkValid = station.m_nview != null && station.m_nview.IsValid();
            CookFromStoragePlugin.Log.LogWarning(
                $"Storage cooking skipped for {station.name}: active={station.isActiveAndEnabled}, "
                + $"networkValid={networkValid}, "
                + $"locallyOwned={networkValid && station.m_nview.IsOwner()}, "
                + $"distance={Math.Sqrt((station.transform.position - player.transform.position).sqrMagnitude):F1}m.");

            return false;
        }

        if (station.HaveDoneItem() || station.FindCookableItem(player.GetInventory()) != null)
        {
            return false;
        }

        var sources = NearbyStorage.Find(player);
        var result = TransferOneIngredient(station, player, sources);

        if (result == TransferResult.NeedsFire || result == TransferResult.Full)
        {
            string message = result == TransferResult.NeedsFire ? "$msg_needfire" : "$msg_nocookroom";
            player.Message(MessageHud.MessageType.Center, message, 0, null, false);
            CookFromStoragePlugin.Log.LogInfo(
                $"Storage ingredient available for {station.name}, but {(result == TransferResult.NeedsFire ? "no lit fire was detected" : "all cooking slots are full")}.");

            return true;
        }

        if (result == TransferResult.NoItems)
        {
            string acceptedNames = string.Join(", ", station.m_conversion
                .Where(conversion => conversion?.m_from != null)
                .Select(conversion => conversion.m_from.m_itemData?.m_shared?.m_name));
            CookFromStoragePlugin.Log.LogInfo(
                $"No accepted storage ingredient for {station.name} in {sources.Count} accessible locally owned containers within {NearbyStorage.Radius:0.#}m. "
                + $"Accepted item names: {acceptedNames}.");
        }

        if (result != TransferResult.Transferred)
        {
            return false;
        }

        handledResult = true;

        return true;
    }

    internal static TransferResult TransferOneIngredient(
        CookingStation station,
        Player player,
        IReadOnlyList<Container> sources)
    {
        if (!CanUse(station, player) || station.m_conversion == null)
        {
            return TransferResult.NotReady;
        }

        foreach (var container in sources)
        {
            if (!StorageInputTransfer.TryGetInventory(container, player, out var inventory))
            {
                continue;
            }

            foreach (var item in inventory.GetAllItems().ToArray())
            {
                if (item?.m_shared == null || item.m_stack <= 0)
                {
                    continue;
                }

                foreach (var conversion in station.m_conversion)
                {
                    var acceptedItem = conversion?.m_from;
                    string acceptedName = acceptedItem?.m_itemData?.m_shared?.m_name;
                    string prefabName = acceptedItem?.name;

                    if (string.IsNullOrEmpty(acceptedName) || string.IsNullOrEmpty(prefabName)
                        || item.m_shared.m_name != acceptedName || !station.IsItemAllowed(prefabName))
                    {
                        continue;
                    }

                    if (station.m_requireFire && !station.IsFireLit())
                    {
                        return TransferResult.NeedsFire;
                    }

                    if (station.GetFreeSlot() < 0)
                    {
                        return TransferResult.Full;
                    }

                    return ConsumeOne(container, inventory, player, station, item, prefabName);
                }
            }
        }

        return TransferResult.NoItems;
    }

    private static TransferResult ConsumeOne(
        Container container,
        Inventory inventory,
        Player player,
        CookingStation station,
        ItemDrop.ItemData item,
        string prefabName)
    {
        if (!StorageInputTransfer.TryGetInventory(container, player, out var currentInventory)
            || currentInventory != inventory || !inventory.ContainsItem(item) || item.m_stack <= 0
            || !CanUse(station, player) || station.GetFreeSlot() < 0
            || (station.m_requireFire && !station.IsFireLit()) || !station.IsItemAllowed(prefabName))
        {
            CookFromStoragePlugin.Log.LogWarning("Storage or cooking station changed before transfer; no ingredient was added.");

            return TransferResult.Failed;
        }

        int freeSlotsBefore = station.GetFreeSlot();
        bool transferred = StorageInputTransfer.ConsumeOne(
            container,
            inventory,
            player,
            item,
            "Cooking station",
            () => CanUse(station, player) && station.GetFreeSlot() == freeSlotsBefore
                && (!station.m_requireFire || station.IsFireLit()) && station.IsItemAllowed(prefabName),
            () => station.RPC_AddItem(player.GetPlayerID(), prefabName, false),
            () =>
            {
                int freeSlotAfter = station.GetFreeSlot();

                return freeSlotAfter == -1 || freeSlotAfter > freeSlotsBefore;
            });

        return transferred ? TransferResult.Transferred : TransferResult.Failed;
    }

    private static bool CanUse(CookingStation station, Player player)
    {
        return station != null && station.isActiveAndEnabled && station.m_nview != null
            && station.m_name != "$piece_frostfoundry"
            && station.m_nview.IsValid() && station.m_nview.IsOwner()
            && player != null
            && (station.transform.position - player.transform.position).sqrMagnitude <= StorageRange.StationReach * StorageRange.StationReach;
    }

    internal enum TransferResult
    {
        Transferred,
        NoItems,
        NotReady,
        NeedsFire,
        Full,
        Failed
    }
}