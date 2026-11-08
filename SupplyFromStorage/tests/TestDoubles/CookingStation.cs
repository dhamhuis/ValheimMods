using System.Runtime.CompilerServices;
using UnityEngine;

public class CookingStation
{
    public Transform transform = new();
    public bool isActiveAndEnabled = true;
    public ZNetView m_nview = new();
    public bool m_useFuel = true;
    public StationItem m_fuelItem;
    public int m_maxFuel = 10;
    public float Fuel { get; set; }
    public int UpdateCalls { get; private set; }

    public float GetFuel() => Fuel;

    public void RPC_AddFuel(long sender)
    {
        if (m_nview.Owner)
        {
            Fuel += 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool OnAddFuelSwitch(Switch sw, Humanoid user, ItemDrop.ItemData item)
    {
        if (!m_useFuel || m_fuelItem?.m_itemData?.m_shared == null
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
    public void Update() => UpdateCalls++;
}