using System;
using System.Collections.Generic;

namespace ValheimMods.Common.Crafting;

internal sealed class CraftingContext : IDisposable
{
    private readonly CraftingContext _previous;

    internal CraftingContext(Player player, List<Container> containers)
    {
        LocalPlayer = player;
        Containers = containers;
        _previous = Current;
        Current = this;
    }

    internal static CraftingContext Current { get; private set; }

    internal Player LocalPlayer { get; }

    internal List<Container> Containers { get; }

    internal IngredientPlan Plan { get; set; }

    internal bool Reserved { get; set; }

    internal bool IncludeStorageCounts { get; set; }

    public void Dispose()
    {
        Current = _previous;
    }

    internal static bool IsStationRecipe(Player player, Recipe recipe)
    {
        return player != null && player == Player.m_localPlayer && recipe != null
            && recipe.m_craftingStation != null && player.GetCurrentCraftingStation() != null
            && RecipeStorage.Supports(player, recipe);
    }

    internal static CraftingContext Enter(Player player, Recipe recipe)
    {
        if (!IsStationRecipe(player, recipe))
        {
            return null;
        }

        var previous = Current;
        var containers = previous?.LocalPlayer == player ? previous.Containers : NearbyStorage.Find(player);

        return new CraftingContext(player, containers)
        {
            Reserved = previous?.Reserved == true,
            Plan = previous?.Plan,
            IncludeStorageCounts = true
        };
    }

    internal static bool Matches(ItemDrop.ItemData item, string name, int quality, bool worldLevel)
    {
        return item.m_stack > 0 && item.m_shared.m_name == name
            && (quality < 0 || item.m_quality == quality)
            && (!worldLevel || item.m_worldLevel >= Game.m_worldLevel);
    }

    internal IEnumerable<Inventory> Inventories()
    {
        yield return LocalPlayer.GetInventory();

        foreach (var container in Containers)
        {
            if (NearbyStorage.IsAccessible(container, LocalPlayer, Reserved))
            {
                yield return container.GetInventory();
            }
        }
    }

    internal int CountStorage(string name, int quality, bool worldLevel)
    {
        int count = 0;

        foreach (var inventory in Inventories())
        {
            if (inventory == LocalPlayer.GetInventory())
            {
                continue;
            }

            foreach (var item in inventory.GetAllItems())
            {
                if (Matches(item, name, quality, worldLevel))
                {
                    count = checked(count + item.m_stack);
                }
            }
        }

        return count;
    }

    internal ItemDrop.ItemData FirstIngredient(
        Recipe recipe,
        int qualityLevel,
        int multiplier,
        out int amount,
        out int extraAmount)
    {
        var station = LocalPlayer.GetCurrentCraftingStation();

        foreach (var requirement in recipe.m_resources)
        {
            if (requirement.m_resItem == null || station.m_upgrader != requirement.m_upgraderResource)
            {
                continue;
            }

            int required = checked(requirement.GetAmount(qualityLevel) * multiplier);

            if (required <= 0)
            {
                continue;
            }

            string name = requirement.m_resItem.m_itemData.m_shared.m_name;

            for (int quality = 0; quality <= requirement.m_resItem.m_itemData.m_shared.m_maxQuality; quality++)
            {
                int count = 0;
                ItemDrop.ItemData candidate = null;

                foreach (var inventory in Inventories())
                {
                    foreach (var item in inventory.GetAllItems())
                    {
                        if (Matches(item, name, quality, true))
                        {
                            candidate ??= item;
                            count = checked(count + item.m_stack);
                        }
                    }
                }

                if (count >= required)
                {
                    amount = required;
                    extraAmount = requirement.m_extraAmountOnlyOneIngredient;

                    return candidate;
                }
            }
        }

        amount = 0;
        extraAmount = 0;

        return null;
    }
}