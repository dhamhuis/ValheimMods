using System;
using System.Collections.Generic;

namespace ValheimMods.Common.Crafting;

internal sealed class IngredientPlan
{
    private readonly List<Ingredient> _ingredients = new();

    internal static IngredientPlan Create(CraftingContext context, Recipe recipe, int quality, int multiplier)
    {
        var plan = new IngredientPlan();

        if (recipe.m_requireOnlyOneIngredient)
        {
            var item = context.FirstIngredient(recipe, quality, multiplier, out int amount, out _);

            if (item == null)
            {
                return null;
            }

            plan._ingredients.Add(new Ingredient
            {
                Name = item.m_shared.m_name,
                Quality = item.m_quality,
                Amount = amount
            });
        }
        else
        {
            var station = context.LocalPlayer.GetCurrentCraftingStation();

            foreach (var requirement in recipe.m_resources)
            {
                if (requirement.m_resItem == null || station.m_upgrader != requirement.m_upgraderResource)
                {
                    continue;
                }

                int amount = checked(requirement.GetAmount(quality) * multiplier);

                if (amount > 0)
                {
                    plan._ingredients.Add(new Ingredient
                    {
                        Name = requirement.m_resItem.m_itemData.m_shared.m_name,
                        Quality = -1,
                        Amount = amount
                    });
                }
            }
        }

        var allocated = new Dictionary<ItemDrop.ItemData, int>();

        foreach (var ingredient in plan._ingredients)
        {
            int remaining = ingredient.Amount;

            foreach (var inventory in context.Inventories())
            {
                foreach (var item in inventory.GetAllItems())
                {
                    if (!CraftingContext.Matches(item, ingredient.Name, ingredient.Quality, true))
                    {
                        continue;
                    }

                    allocated.TryGetValue(item, out int used);
                    int take = Math.Min(remaining, item.m_stack - used);

                    if (take <= 0)
                    {
                        continue;
                    }

                    ingredient.Portions.Add(new Portion
                    {
                        Inventory = inventory,
                        Item = item,
                        Amount = take
                    });

                    allocated[item] = used + take;
                    remaining -= take;
                }
            }

            if (remaining > 0)
            {
                return null;
            }
        }

        return plan;
    }

    internal List<Container> StorageSources(CraftingContext context)
    {
        var inventories = new HashSet<Inventory>();

        foreach (var ingredient in _ingredients)
        {
            foreach (var portion in ingredient.Portions)
            {
                inventories.Add(portion.Inventory);
            }
        }

        return context.Containers.FindAll(container => inventories.Contains(container.GetInventory()));
    }

    internal void Consume(string name, int amount, int quality, bool worldLevel)
    {
        var ingredient = _ingredients.Find(entry =>
            !entry.Consumed && entry.Name == name && entry.Amount == amount && entry.Quality == quality);

        if (ingredient == null || !worldLevel)
        {
            throw new InvalidOperationException($"Unexpected crafting consumption: {name}, amount {amount}, quality {quality}.");
        }

        foreach (var portion in ingredient.Portions)
        {
            if (!portion.Inventory.ContainsItem(portion.Item) || portion.Item.m_stack < portion.Amount)
            {
                throw new InvalidOperationException($"Reserved crafting ingredient changed: {name}.");
            }
        }

        foreach (var portion in ingredient.Portions)
        {
            if (!portion.Inventory.RemoveItem(portion.Item, portion.Amount))
            {
                throw new InvalidOperationException($"Could not consume reserved crafting ingredient: {name}.");
            }
        }

        ingredient.Consumed = true;
    }

    private sealed class Portion
    {
        internal Inventory Inventory { get; set; }

        internal ItemDrop.ItemData Item { get; set; }

        internal int Amount { get; set; }
    }

    private sealed class Ingredient
    {
        internal string Name { get; set; }

        internal int Quality { get; set; }

        internal int Amount { get; set; }

        internal bool Consumed { get; set; }

        internal List<Portion> Portions { get; } = new();
    }
}