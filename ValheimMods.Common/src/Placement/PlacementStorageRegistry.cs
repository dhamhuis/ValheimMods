using System.Collections.Generic;
using UnityEngine;

namespace ValheimMods.Common.Placement;

internal sealed class PlacementStorageRegistry
{
    private readonly ContainerRegistry _registry = new();
    private List<Container> _cached;
    private int _frame = -1;
    private Player _player;
    private Vector3 _position;
    private float _radius;

    internal void Initialize()
    {
        Clear();
        _registry.Initialize();
    }

    internal void Add(Container container)
    {
        _registry.Add(container);
        Invalidate();
    }

    internal void Remove(Container container)
    {
        _registry.Remove(container);
        Invalidate();
    }

    internal void Clear()
    {
        _registry.Clear();
        _cached = null;
        _player = null;
        _frame = -1;
    }

    internal bool IsAccessible(Container container, Player player, float radius, bool allowReserved)
    {
        return container != null && container.IsOwner()
            && ContainerRegistry.IsAccessible(container, player, radius, allowReserved);
    }

    internal List<Container> Find(Player player, float radius)
    {
        radius = StorageRange.Clamp(radius);

        if (_cached != null && _frame == Time.frameCount && _player == player
            && _position == player.transform.position && _radius == radius)
        {
            return _cached;
        }

        var result = new List<Container>();
        _registry.PruneDestroyed();

        foreach (var container in _registry.Containers)
        {
            if (!IsAccessible(container, player, radius, false))
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

    private void Invalidate()
    {
        _frame = -1;
    }
}