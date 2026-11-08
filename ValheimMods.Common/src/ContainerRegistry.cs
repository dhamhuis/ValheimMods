using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimMods.Common;

public sealed class ContainerRegistry
{
    private readonly HashSet<Container> _containers = new();

    public IEnumerable<Container> Containers => _containers;

    public void Initialize()
    {
        Clear();

        foreach (var container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
        {
            Add(container);
        }
    }

    public void Add(Container container)
    {
        if (container != null)
        {
            _containers.Add(container);
        }
    }

    public void Remove(Container container)
    {
        _containers.Remove(container);
    }

    public void Clear()
    {
        _containers.Clear();
    }

    public void PruneDestroyed(Action<Container> onDestroyed = null)
    {
        _containers.RemoveWhere(container =>
        {
            if (container != null)
            {
                return false;
            }

            onDestroyed?.Invoke(container);

            return true;
        });
    }

    public static bool IsAccessible(Container container, Player player, float radius, bool allowReserved = false)
    {
        return container != null && container.isActiveAndEnabled && player != null
            && container.m_nview != null && container.m_nview.IsValid()
            && container.GetInventory() != null
            && (container.transform.position - player.transform.position).sqrMagnitude <= radius * radius
            && (allowReserved || !container.IsInUse())
            && (container.IsOwner() || container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse, 0) == 0)
            && (container.m_wagon == null || !container.m_wagon.InUse())
            && container.CheckAccess(player.GetPlayerID())
            && (!container.m_checkGuardStone
                || PrivateArea.CheckAccess(container.transform.position, 0f, false, false));
    }
}