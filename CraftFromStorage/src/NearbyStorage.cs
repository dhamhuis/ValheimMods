using System.Collections.Generic;
using UnityEngine;

namespace CraftFromStorage;

internal static class NearbyStorage
{
    internal const float Radius = 20f;

    private static readonly HashSet<Container> _containers = new();
    private static List<Container> _cached;
    private static int _frame = -1;
    private static Player _player;
    private static Vector3 _position;

    internal static void Initialize()
    {
        Clear();

        foreach (var container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
        {
            Add(container);
        }
    }

    internal static void Add(Container container)
    {
        _containers.Add(container);
        _frame = -1;
    }

    internal static void Remove(Container container)
    {
        _containers.Remove(container);
        StorageCrafting.Forget(container);
        _frame = -1;
    }

    internal static void Clear()
    {
        _containers.Clear();
        _cached = null;
        _player = null;
        _frame = -1;
    }

    internal static bool IsAccessible(Container container, Player player, bool allowReserved = false)
    {
        return container != null && container.isActiveAndEnabled && player != null
            && container.m_nview != null && container.m_nview.IsValid()
            && container.GetInventory() != null
            && (container.transform.position - player.transform.position).sqrMagnitude <= (Radius * Radius)
            && (allowReserved || !container.IsInUse())
            && (container.IsOwner() || container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse, 0) == 0)
            && (container.m_wagon == null || !container.m_wagon.InUse())
            && container.CheckAccess(player.GetPlayerID())
            && (!container.m_checkGuardStone
                || PrivateArea.CheckAccess(container.transform.position, 0f, false, false));
    }

    internal static List<Container> Find(Player player, bool refresh = false)
    {
        if (!refresh && _cached != null && _frame == Time.frameCount
            && _player == player && _position == player.transform.position)
        {
            return _cached;
        }

        var result = new List<Container>();
        _containers.RemoveWhere(container =>
        {
            if (container != null)
            {
                return false;
            }

            StorageCrafting.Forget(container);

            return true;
        });

        foreach (var container in _containers)
        {
            if (!IsAccessible(container, player))
            {
                continue;
            }

            container.Load();
            result.Add(container);
        }

        Vector3 position = player.transform.position;
        result.Sort((left, right) =>
        {
            int distance = (left.transform.position - position).sqrMagnitude
                .CompareTo((right.transform.position - position).sqrMagnitude);

            return distance != 0 ? distance : left.GetInstanceID().CompareTo(right.GetInstanceID());
        });

        _cached = result;
        _player = player;
        _position = position;
        _frame = Time.frameCount;

        return result;
    }
}