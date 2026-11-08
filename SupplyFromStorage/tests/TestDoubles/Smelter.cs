using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Smelter
{
    public sealed class ItemConversion
    {
        public StationItem m_from;

        public StationItem m_to;
    }

    public Transform transform = new();
    public bool isActiveAndEnabled = true;
    public ZNetView m_nview = new();
    public StationItem m_fuelItem;
    public int m_maxOre = 10;
    public int m_maxFuel = 10;
    public List<ItemConversion> m_conversion = new();
    public float Fuel { get; set; }
    public int QueueSize { get; set; }
    public int UpdateCalls { get; private set; }
    public string LastInput { get; private set; }

    public float GetFuel() => Fuel;

    public int GetQueueSize() => QueueSize;

    public bool IsItemAllowed(string name)
    {
        return m_conversion.Exists(conversion => conversion.m_from != null && conversion.m_from.name == name);
    }

    public void RPC_AddFuel(long sender)
    {
        if (m_nview.Owner)
        {
            Fuel += 1;
        }
    }

    public void RPC_AddOre(long sender, string name, bool cheated)
    {
        if (m_nview.Owner && IsItemAllowed(name))
        {
            QueueSize++;
            LastInput = name;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool OnAddFuel(Switch sw, Humanoid user, ItemDrop.ItemData item)
    {
        if (m_fuelItem?.m_itemData?.m_shared == null
            || user is not Player player)
        {
            return false;
        }

        if (item != null && item.m_shared.m_name != m_fuelItem.m_itemData.m_shared.m_name)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_wrongitem", 0, null, false);

            return false;
        }

        if (Fuel > m_maxFuel - 1)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null, false);

            return false;
        }

        item ??= player.Inventory.GetAllItems()
            .Find(candidate => candidate?.m_shared?.m_name == m_fuelItem.m_itemData.m_shared.m_name
                && candidate.m_stack > 0);

        if (item == null || !player.Inventory.RemoveItem(item, 1))
        {
            user.Message(MessageHud.MessageType.Center,
                "$msg_donthaveany " + m_fuelItem.m_itemData.m_shared.m_name, 0, null, false);

            return false;
        }

        user.Message(MessageHud.MessageType.Center, "$msg_added " + m_fuelItem.m_itemData.m_shared.m_name, 0, null, false);
        RPC_AddFuel(player.GetPlayerID());

        return Fuel > 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool OnAddOre(Switch sw, Humanoid user, ItemDrop.ItemData item)
    {
        if (user is not Player player)
        {
            return false;
        }

        if (item == null)
        {
            foreach (var conversionCandidate in m_conversion)
            {
                string itemName = conversionCandidate?.m_from?.m_itemData?.m_shared?.m_name;
                item = player.Inventory.GetAllItems()
                    .Find(candidate => !string.IsNullOrEmpty(itemName)
                        && candidate?.m_shared?.m_name == itemName && candidate.m_stack > 0);

                if (item != null)
                {
                    break;
                }
            }

            if (item == null)
            {
                user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems", 0, null, false);
                return false;
            }
        }

        var conversion = item?.m_shared == null
            ? null
            : m_conversion.Find(candidate => candidate.m_from?.m_itemData?.m_shared?.m_name == item.m_shared.m_name);

        if (conversion?.m_from == null || !IsItemAllowed(conversion.m_from.name)
            || !player.Inventory.RemoveItem(item, 1))
        {
            return false;
        }

        if (GetQueueSize() >= m_maxOre)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null, false);
            return false;
        }

        user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name, 0, null, false);

        RPC_AddOre(player.GetPlayerID(), conversion.m_from.name, false);

        return GetQueueSize() > 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UpdateSmelter() => UpdateCalls++;
}