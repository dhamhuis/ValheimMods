using System.Collections.Generic;
using UnityEngine;
using ValheimMods.Common;

namespace SupplyFromStorage;

internal static class NearbyStorage
{
    private static readonly ContainerRegistry _registry = new();

    internal static float Radius => StorageRange.Clamp(SupplyFromStoragePlugin.StorageRadius);

    internal static void Initialize()
    {
        _registry.Initialize();
    }

    internal static void Add(Container container)
    {
        if (container != null)
        {
            _registry.Add(container);
        }
    }

    internal static void Remove(Container container)
    {
        _registry.Remove(container);
    }

    internal static void Clear()
    {
        _registry.Clear();
    }

    internal static bool IsAccessible(Container container, Player player)
    {
        return ContainerRegistry.IsAccessible(container, player, Radius);
    }

    internal static List<Container> Find(Player player)
    {
        var result = new List<Container>();

        if (player == null)
        {
            return result;
        }

        _registry.PruneDestroyed();

        foreach (var container in _registry.Containers)
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