using System.Runtime.CompilerServices;
using UnityEngine;

public class Fireplace
{
    public Transform transform = new();
    public bool isActiveAndEnabled = true;
    public ZNetView m_nview = new();
    public bool m_canRefill = true;
    public bool m_infiniteFuel;
    public bool m_canTurnOff;
    public StationItem m_fuelItem;
    public float m_maxFuel = 10;
    public float m_lastUseTime;
    public float m_holdRepeatInterval = 0.5f;
    public bool AcceptFuel { get; set; } = true;

    public float Fuel
    {
        get => m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0);
        set => m_nview.GetZDO().Set(ZDOVars.s_fuel, value);
    }

    public int UpdateCalls { get; private set; }

    public void RPC_AddFuel(long sender)
    {
        if (m_nview.Owner && AcceptFuel && System.Math.Ceiling(Fuel) < m_maxFuel)
        {
            Fuel = System.Math.Min(Fuel + 1, m_maxFuel);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold && Time.time - m_lastUseTime < m_holdRepeatInterval)
        {
            return false;
        }

        m_lastUseTime = Time.time;

        if (m_canTurnOff && !hold && !alt && Fuel > 0)
        {
            return true;
        }

        if (!m_canRefill || m_infiniteFuel || m_fuelItem?.m_itemData?.m_shared == null
            || System.Math.Ceiling(Fuel) >= m_maxFuel || user is not Player player)
        {
            return false;
        }

        var item = player.Inventory.GetAllItems()
            .Find(candidate => candidate?.m_shared?.m_name == m_fuelItem.m_itemData.m_shared.m_name
                && candidate.m_stack > 0);

        if (item == null || !player.Inventory.RemoveItem(item, 1))
        {
            return false;
        }

        RPC_AddFuel(player.GetPlayerID());

        return Fuel > 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UpdateFireplace() => UpdateCalls++;
}
