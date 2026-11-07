using System.Collections.Generic;
using UnityEngine;

namespace SupplyFromStorage;

internal static class NearbyStorage
{
    internal const float Radius = 20f;

    private static readonly HashSet<Container> _containers = new();

    internal static void Initialize()
    {
        Clear();

        foreach (var container in Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
        {
            Add(container);
        }
    }

    internal static void Add(Container container)
    {
        if (container != null)
        {
            _containers.Add(container);
        }
    }

    internal static void Remove(Container container)
    {
        _containers.Remove(container);
    }

    internal static void Clear()
    {
        _containers.Clear();
    }

    internal static bool IsAccessible(Container container, Player player)
    {
        return container != null && container.isActiveAndEnabled && player != null
            && container.m_nview != null && container.m_nview.IsValid()
            && container.GetInventory() != null
            && (container.transform.position - player.transform.position).sqrMagnitude <= Radius * Radius
            && !container.IsInUse()
            && (container.IsOwner() || container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse, 0) == 0)
            && (container.m_wagon == null || !container.m_wagon.InUse())
            && container.CheckAccess(player.GetPlayerID())
            && (!container.m_checkGuardStone
                || PrivateArea.CheckAccess(container.transform.position, 0f, false, false));
    }

    internal static List<Container> Find(Player player)
    {
        var result = new List<Container>();

        if (player == null)
        {
            return result;
        }

        _containers.RemoveWhere(container => container == null);

        foreach (var container in _containers)
        {
            if (!IsAccessible(container, player) || !container.IsOwner())
            {
                continue;
            }

            container.Load();

            if (IsAccessible(container, player) && container.IsOwner())
            {
                result.Add(container);
            }
        }

        Vector3 position = player.transform.position;
        result.Sort((left, right) =>
        {
            int distance = (left.transform.position - position).sqrMagnitude
                .CompareTo((right.transform.position - position).sqrMagnitude);

            return distance != 0 ? distance : left.GetInstanceID().CompareTo(right.GetInstanceID());
        });

        return result;
    }
}
