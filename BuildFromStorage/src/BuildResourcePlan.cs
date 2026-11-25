using System;
using System.Collections.Generic;

namespace BuildFromStorage;

internal sealed class BuildResourcePlan
{
    private readonly List<StoragePortion> _storagePortions = new();
    private bool _consumed;

    internal bool UsesStorage => _storagePortions.Count > 0;

    internal IEnumerable<Container> StorageContainers
    {
        get
        {
            var seen = new HashSet<Container>();

            foreach (var portion in _storagePortions)
            {
                if (seen.Add(portion.Container))
                {
                    yield return portion.Container;
                }
            }
        }
    }

    internal static BuildResourcePlan Create(
        BuildStorageContext.Scope context,
        Piece.Requirement[] requirements,
        int qualityLevel,
        int itemQuality,
        int multiplier)
    {
        if (requirements == null)
        {
            return null;
        }

        var plan = new BuildResourcePlan();
        var allocated = new Dictionary<Inventory, Dictionary<string, int>>();
        var playerInventory = context.Player.GetInventory();
        var station = context.Player.GetCurrentCraftingStation();
        var sources = new List<Source>();

        if (playerInventory != null)
        {
            sources.Add(new Source(playerInventory, null));
        }

        foreach (var container in context.Containers)
        {
            if (context.IsAccessible(container))
            {
                sources.Add(new Source(container.GetInventory(), container));
            }
        }

        bool wasSuppressed = context.SuppressCountAugmentation;
        context.SuppressCountAugmentation = true;

        try
        {
            foreach (var requirement in requirements)
            {
                if (requirement == null || requirement.m_resItem == null)
                {
                    continue;
                }

                if ((station != null && station.m_upgrader != requirement.m_upgraderResource)
                    || (station == null && requirement.m_upgraderResource))
                {
                    continue;
                }

                int amount = requirement.GetAmount(qualityLevel) * multiplier;

                if (amount <= 0)
                {
                    continue;
                }

                string name = requirement.m_resItem.m_itemData.m_shared.m_name;
                int remaining = amount;

                foreach (var source in sources)
                {
                    if (source.Inventory == null)
                    {
                        continue;
                    }

                    if (!allocated.TryGetValue(source.Inventory, out var itemAllocations))
                    {
                        itemAllocations = new Dictionary<string, int>();
                        allocated.Add(source.Inventory, itemAllocations);
                    }

                    itemAllocations.TryGetValue(name, out int used);
                    int available = source.Inventory.CountItems(name, itemQuality, true) - used;
                    int take = Math.Min(remaining, available);

                    if (take <= 0)
                    {
                        continue;
                    }

                    itemAllocations[name] = used + take;

                    if (source.Container != null)
                    {
                        plan.AddStorage(source.Container, source.Inventory, name, take, itemQuality);
                    }

                    remaining -= take;

                    if (remaining == 0)
                    {
                        break;
                    }
                }

                if (remaining > 0)
                {
                    return null;
                }
            }
        }
        finally
        {
            context.SuppressCountAugmentation = wasSuppressed;
        }

        return plan;
    }

    internal bool ValidateStorage(BuildStorageContext.Scope context)
    {
        var allocated = new Dictionary<Inventory, Dictionary<string, int>>();

        foreach (var portion in _storagePortions)
        {
            if (!context.IsAccessible(portion.Container) || !portion.Container.IsOwner())
            {
                return false;
            }

            if (!allocated.TryGetValue(portion.Inventory, out var itemAllocations))
            {
                itemAllocations = new Dictionary<string, int>();
                allocated.Add(portion.Inventory, itemAllocations);
            }

            itemAllocations.TryGetValue(portion.Name, out int used);
            used += portion.Amount;
            itemAllocations[portion.Name] = used;

            if (portion.Inventory.CountItems(portion.Name, portion.ItemQuality, true) < used)
            {
                return false;
            }
        }

        return true;
    }

    internal void ConsumeStorage(BuildStorageContext.Scope context)
    {
        if (!UsesStorage || _consumed)
        {
            return;
        }

        if (!ValidateStorage(context))
        {
            const string message = "Build storage changed before resource consumption; the placed piece may need manual correction.";
            BuildFromStoragePlugin.LogError(message);

            throw new InvalidOperationException(message);
        }

        foreach (var portion in _storagePortions)
        {
            int before = portion.Inventory.CountItems(portion.Name, portion.ItemQuality, true);
            portion.Inventory.RemoveItem(portion.Name, portion.Amount, portion.ItemQuality, true);
            int after = portion.Inventory.CountItems(portion.Name, portion.ItemQuality, true);

            if (before - after != portion.Amount)
            {
                const string message = "Build storage did not consume the planned resource amount.";
                BuildFromStoragePlugin.LogError(message);

                throw new InvalidOperationException(message);
            }
        }

        _consumed = true;
    }

    private void AddStorage(Container container, Inventory inventory, string name, int amount, int itemQuality)
    {
        var portion = _storagePortions.Find(entry =>
            entry.Container == container && entry.Name == name && entry.ItemQuality == itemQuality);

        if (portion == null)
        {
            _storagePortions.Add(new StoragePortion(container, inventory, name, amount, itemQuality));

            return;
        }

        portion.Amount += amount;
    }

    private sealed class Source
    {
        internal Source(Inventory inventory, Container container)
        {
            Inventory = inventory;
            Container = container;
        }

        internal Inventory Inventory { get; }

        internal Container Container { get; }
    }

    private sealed class StoragePortion
    {
        internal StoragePortion(Container container, Inventory inventory, string name, int amount, int itemQuality)
        {
            Container = container;
            Inventory = inventory;
            Name = name;
            Amount = amount;
            ItemQuality = itemQuality;
        }

        internal Container Container { get; }

        internal Inventory Inventory { get; }

        internal string Name { get; }

        internal int Amount { get; set; }

        internal int ItemQuality { get; }
    }
}