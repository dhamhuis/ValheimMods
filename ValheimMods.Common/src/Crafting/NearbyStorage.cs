using System.Collections.Generic;
using UnityEngine;

namespace ValheimMods.Common.Crafting;

internal static class NearbyStorage
{
    private static readonly ContainerRegistry _registry = new();
    private static List<Container> _cached;
    private static int _frame = -1;
    private static Player _player;
    private static Vector3 _position;
    private static float _radius;

    internal static void Initialize()
    {
        Clear();
        _registry.Initialize();
    }

    internal static void Add(Container container)
    {
        _registry.Add(container);
        _frame = -1;
    }

    internal static void Remove(Container container)
    {
        _registry.Remove(container);
        StorageCrafting.Forget(container);
        _frame = -1;
    }

    internal static void Clear()
    {
        _registry.Clear();
        _cached = null;
        _player = null;
        _frame = -1;
    }

    internal static bool IsAccessible(Container container, Player player, bool allowReserved = false)
    {
        return ContainerRegistry.IsAccessible(container, player, RecipeStorage.RadiusFor(player), allowReserved);
    }

    internal static List<Container> Find(Player player, bool refresh = false)
    {
        float radius = RecipeStorage.RadiusFor(player);

        if (!refresh && _cached != null && _frame == Time.frameCount
            && _player == player && _position == player.transform.position && _radius == radius)
        {
            return _cached;
        }

        var result = new List<Container>();
        _registry.PruneDestroyed(StorageCrafting.Forget);

        foreach (var container in _registry.Containers)
        {
            if (!ContainerRegistry.IsAccessible(container, player, radius))
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
        _radius = radius;
        _frame = Time.frameCount;

        return result;
    }
}