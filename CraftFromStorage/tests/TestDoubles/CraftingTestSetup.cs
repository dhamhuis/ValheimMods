using System;
using UnityEngine;

namespace CraftFromStorage.Tests;

internal static class CraftingTestSetup
{
    private static RecipeStorage _recipeStorage;

    internal static float Radius { get; set; } = 20f;

    internal static Player Reset()
    {
        CraftFromStoragePlugin.Instance.Clear();
        Radius = 20f;
        _recipeStorage?.Dispose();
        _recipeStorage = RecipeStorage.Register(
            false,
            CraftFromStoragePlugin.Instance.StartCoroutine,
            CraftFromStoragePlugin.Instance.Clear,
            CraftFromStoragePlugin.Log.LogWarning,
            CraftFromStoragePlugin.Log.LogError,
            () => true,
            () => Radius);
        NearbyStorage.Clear();
        StorageCrafting.Clear();
        InventoryGui.instance = new InventoryGui();
        InventoryGui.Visible = true;
        Time.frameCount++;
        Time.realtimeSinceStartup = 0;
        Game.m_worldLevel = 0;
        PrivateArea.Allowed = true;
        ZoneSystem.instance.NoCost = false;

        return Player.m_localPlayer = new Player();
    }

    internal static ItemDrop.ItemData Item(string name, int count, int quality = 1, int world = 0)
    {
        return new ItemDrop.ItemData
        {
            m_shared = new()
            {
                m_name = name,
                m_maxQuality = 3
            },

            m_stack = count,
            m_quality = quality,
            m_worldLevel = world
        };
    }

    internal static Container Chest(float x, float y, float z, params ItemDrop.ItemData[] items)
    {
        var chest = new Container();
        chest.transform.position = new Vector3(x, y, z);
        chest.Inventory.Items.AddRange(items);
        NearbyStorage.Add(chest);

        return chest;
    }

    internal static Recipe RecipeFor(string name, int count)
    {
        return new Recipe
        {
            m_resources =
            [
                new()
                {
                    m_resItem = new()
                    {
                        m_itemData = Item(name, 1)
                    },

                    m_amount = count,
                    m_amountPerLevel = count
                }
            ]
        };
    }

    internal static InventoryGui Gui(Recipe recipe, Action<Player> onCraft)
    {
        var gui = InventoryGui.instance;
        gui.m_craftRecipe = recipe;
        gui.m_selectedRecipe = (recipe, null);
        gui.OnCraft = onCraft;

        return gui;
    }
}