using System.Collections.Generic;
using UnityEngine;

namespace BuildFromStorage;

internal static class BuildStorageContext
{
    private static Scope _current;

    internal static Scope Current => _current;

    internal static ScopeToken BeginPlacement(Player player, BuildStorageRegistry registry, float radius)
    {
        if (player == null || player != Player.m_localPlayer || player.PlacementCostDisabled)
        {
            return null;
        }

        if (_current != null && _current.Player == player)
        {
            return null;
        }

        var previous = _current;
        _current = new Scope(player, registry, radius);

        return new ScopeToken(_current, previous, false, true);
    }

    internal static ScopeToken BeginRequirements(Player player, BuildStorageRegistry registry, float radius)
    {
        if (player == null || player != Player.m_localPlayer || player.PlacementCostDisabled)
        {
            return null;
        }

        if (_current != null && _current.Player == player)
        {
            _current.RequirementCheckDepth++;

            return new ScopeToken(_current, null, true, false);
        }

        var previous = _current;
        _current = new Scope(player, registry, radius);
        _current.RequirementCheckDepth++;

        return new ScopeToken(_current, previous, true, true);
    }

    internal static void End(ScopeToken token)
    {
        if (token == null)
        {
            return;
        }

        if (token.RequirementCheck)
        {
            token.Scope.RequirementCheckDepth--;
        }

        if (token.OwnsScope)
        {
            token.Scope.Release();
            _current = token.Previous;
        }
    }

    internal static void Reset()
    {
        _current?.Release();
        _current = null;
    }

    internal sealed class Scope
    {
        private readonly BuildStorageRegistry _registry;
        private readonly float _radius;
        private readonly HashSet<Container> _reservedSet = new();
        private readonly List<Container> _reserved = new();

        internal Scope(Player player, BuildStorageRegistry registry, float radius)
        {
            Player = player;
            _registry = registry;
            _radius = radius;
            Containers = registry.Find(player, radius);
        }

        internal Player Player { get; }

        internal List<Container> Containers { get; }

        internal BuildResourcePlan Plan { get; private set; }

        internal bool SuppressCountAugmentation { get; set; }

        internal int RequirementCheckDepth { get; set; }

        internal bool IsAccessible(Container container)
        {
            return _registry.IsAccessible(container, Player, _radius, _reservedSet.Contains(container));
        }

        internal bool TryReserve(BuildResourcePlan plan)
        {
            if (!plan.ValidateStorage(this))
            {
                return false;
            }

            foreach (var container in plan.StorageContainers)
            {
                if (!IsAccessible(container))
                {
                    return false;
                }
            }

            foreach (var container in plan.StorageContainers)
            {
                if (_reservedSet.Add(container))
                {
                    _reserved.Add(container);
                    container.m_inUse = true;
                }
            }

            Plan = plan;

            return true;
        }

        internal void ConsumeStorage()
        {
            Plan?.ConsumeStorage(this);
        }

        internal void Release()
        {
            foreach (var container in _reserved)
            {
                if (container != null)
                {
                    container.m_inUse = false;
                }
            }

            _reserved.Clear();
            _reservedSet.Clear();
        }
    }

    internal sealed class ScopeToken
    {
        internal ScopeToken(Scope scope, Scope previous, bool requirementCheck, bool ownsScope)
        {
            Scope = scope;
            Previous = previous;
            RequirementCheck = requirementCheck;
            OwnsScope = ownsScope;
        }

        internal Scope Scope { get; }

        internal Scope Previous { get; }

        internal bool RequirementCheck { get; }

        internal bool OwnsScope { get; }
    }
}