using System;

namespace CookFromStorage;

internal static class StorageInputTransfer
{
    internal static bool TryGetInventory(Container container, Player player, out Inventory inventory)
    {
        inventory = null;

        if (!NearbyStorage.IsAccessible(container, player) || !container.IsOwner())
        {
            return false;
        }

        inventory = container.GetInventory();

        return inventory != null;
    }

    internal static bool ConsumeOne(
        Container container,
        Inventory inventory,
        Player player,
        ItemDrop.ItemData item,
        string stationName,
        Func<bool> canInsert,
        Action insert,
        Func<bool> wasInserted)
    {
        bool validSource = container == null ? player.GetInventory() == inventory
            : TryGetInventory(container, player, out var currentInventory) && currentInventory == inventory;

        if (!validSource || !inventory.ContainsItem(item) || item.m_stack <= 0
            || !canInsert())
        {
            CookFromStoragePlugin.Log.LogWarning($"Source inventory or {stationName} changed before transfer; no ingredient was added.");

            return false;
        }

        int originalStack = item.m_stack;
        int originalCount = CountItems(inventory, item.m_shared.m_name);

        if (container != null)
        {
            container.m_inUse = true;
        }

        try
        {
            if (!inventory.RemoveItem(item, 1))
            {
                CookFromStoragePlugin.Log.LogWarning("Source ingredient changed before transfer; no station input was added.");

                return false;
            }

            Exception transferFailure = null;

            try
            {
                insert();
            }
            catch (Exception exception)
            {
                transferFailure = exception;
            }

            bool committed;

            try
            {
                committed = wasInserted();
            }
            catch (Exception exception)
            {
                CookFromStoragePlugin.Log.LogError(
                    $"Could not confirm {stationName} transfer; source ingredient remains consumed to avoid duplicate insertion. {exception}");

                return false;
            }

            if (committed)
            {
                if (transferFailure != null)
                {
                    CookFromStoragePlugin.Log.LogError(
                        $"{stationName} accepted the source ingredient, but its transfer callback threw after insertion. {transferFailure}");
                }

                return true;
            }

            bool restored;

            try
            {
                restored = RestoreItem(inventory, item, originalStack, originalCount);
            }
            catch (Exception exception)
            {
                CookFromStoragePlugin.Log.LogError($"{stationName} rejected an ingredient and source restoration threw. {exception}");

                throw;
            }

            CookFromStoragePlugin.Log.LogError(
                $"{stationName} rejected a source ingredient; source restoration {(restored ? "succeeded" : "failed")}."
                + (transferFailure == null ? string.Empty : $" Transfer callback error: {transferFailure}"));

            return false;
        }
        finally
        {
            if (container != null)
            {
                container.m_inUse = false;
            }
        }
    }

    private static bool RestoreItem(Inventory inventory, ItemDrop.ItemData item, int originalStack, int originalCount)
    {
        item.m_stack = originalStack;

        if (!inventory.ContainsItem(item) && !inventory.AddItem(item))
        {
            return false;
        }

        return CountItems(inventory, item.m_shared.m_name) >= originalCount;
    }

    private static int CountItems(Inventory inventory, string itemName)
    {
        int count = 0;

        foreach (var item in inventory.GetAllItems())
        {
            if (item?.m_shared != null && item.m_shared.m_name == itemName)
            {
                count = checked(count + item.m_stack);
            }
        }

        return count;
    }
}