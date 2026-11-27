using System.Collections.Generic;

namespace ValheimMods.Common.Placement;

internal static class PlacementStorageContext
{
    private static Scope _current;

    internal static Scope Current => _current;

    internal static ScopeToken BeginPlacement(Player player)
    {
        if (!PlacementStorage.TryGetClient(player, out var client) || player.PlacementCostDisabled)
        {
            return null;
        }

        if (_current != null && _current.Player == player)
        {
            return null;
        }

        var previous = _current;
        _current = new Scope(player, client);

        return new ScopeToken(_current, previous, false, true);
    }

    internal static ScopeToken BeginRequirements(Player player)
    {
        if (!PlacementStorage.TryGetClient(player, out var client) || player.PlacementCostDisabled)
        {
            return null;
        }

        if (_current != null && _current.Player == player)
        {
            _current.RequirementCheckDepth++;

            return new ScopeToken(_current, null, true, false);
        }

        var previous = _current;
        _current = new Scope(player, client);
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
        private readonly float _radius;
        private readonly HashSet<Container> _reservedSet = new();
        private readonly List<Container> _reserved = new();

        internal Scope(Player player, PlacementStorage client)
        {
            Player = player;
            Client = client;
            _radius = client.Radius;
            Containers = PlacementStorage.Registry.Find(player, _radius);
        }

        internal Player Player { get; }

        internal PlacementStorage Client { get; }

        internal List<Container> Containers { get; }

        internal PlacementResourcePlan Plan { get; private set; }

        internal bool SuppressCountAugmentation { get; set; }

        internal int RequirementCheckDepth { get; set; }

        internal bool IsAccessible(Container container)
        {
            return PlacementStorage.Registry.IsAccessible(container, Player, _radius, _reservedSet.Contains(container));
        }

        internal bool TryReserve(PlacementResourcePlan plan)
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