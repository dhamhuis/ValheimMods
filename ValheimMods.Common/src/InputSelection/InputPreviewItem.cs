using System;

namespace ValheimMods.Common.InputSelection;

public sealed class InputPreviewItem
{
    public InputPreviewItem(ItemDrop item, int inventoryCount, int storageCount)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        InventoryCount = inventoryCount;
        StorageCount = storageCount;
    }

    public ItemDrop Item { get; }

    public int InventoryCount { get; }

    public int StorageCount { get; }

    public bool IsAvailable => InventoryCount > 0 || StorageCount > 0;

    public static int Count(Inventory inventory, string prefabName)
    {
        if (inventory == null || string.IsNullOrEmpty(prefabName))
        {
            throw new ArgumentException("Preview counts require an inventory and a prefab identifier.");
        }

        int count = 0;

        foreach (var item in inventory.GetAllItems())
        {
            if (item?.m_dropPrefab != null && item.m_dropPrefab.name == prefabName && item.m_stack > 0)
            {
                count += item.m_stack;
            }
        }

        return count;
    }
}