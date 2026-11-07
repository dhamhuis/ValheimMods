using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Inventory
{
    public List<ItemDrop.ItemData> Items { get; } = [];

    public int Changes { get; private set; }

    public List<ItemDrop.ItemData> GetAllItems() => Items;

    public bool ContainsItem(ItemDrop.ItemData item) => Items.Contains(item);

    public bool AddItem(ItemDrop.ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        if (!Items.Contains(item))
        {
            Items.Add(item);
        }

        Changes++;

        return true;
    }

    public bool RemoveItem(ItemDrop.ItemData item, int amount)
    {
        if (!Items.Contains(item) || item.m_stack < amount)
        {
            return false;
        }

        item.m_stack -= amount;
        Changes++;

        if (item.m_stack == 0)
        {
            Items.Remove(item);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int CountItems(string name, int quality, bool matchWorldLevel)
    {
        int count = 0;

        foreach (var item in Items)
        {
            if ((name == null || item.m_shared.m_name == name) && (quality < 0 || item.m_quality == quality)
                && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel))
            {
                count += item.m_stack;
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RemoveItem(string name, int amount, int itemQuality, bool worldLevelBased)
    {
        foreach (var item in Items.ToArray())
        {
            if (item.m_shared.m_name != name || (itemQuality >= 0 && item.m_quality != itemQuality)
                || (worldLevelBased && item.m_worldLevel < Game.m_worldLevel))
            {
                continue;
            }

            int take = System.Math.Min(amount, item.m_stack);
            RemoveItem(item, take);
            amount -= take;

            if (amount == 0)
            {
                break;
            }
        }
    }
}