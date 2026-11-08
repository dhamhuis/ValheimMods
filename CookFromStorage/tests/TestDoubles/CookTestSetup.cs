using UnityEngine;

namespace CookFromStorage.Tests;

internal static class CookTestSetup
{
    internal static Player Reset()
    {
        NearbyStorage.Clear();
        CookFromStoragePlugin.AutomaticIngredientsEnabled = true;
        CookFromStoragePlugin.StorageRadius = 20f;
        PrivateArea.Allowed = true;

        var player = Player.m_localPlayer = new Player();
        player.transform.position = new Vector3(0, 0, 0);

        return player;
    }

    internal static ItemDrop.ItemData Item(string itemName, string prefabName, int count)
    {
        return new ItemDrop.ItemData
        {
            m_shared = new()
            {
                m_name = itemName
            },
            m_dropPrefab = new GameObject(prefabName),
            m_stack = count
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

    internal static CookingStation Station(params CookingStation.ItemConversion[] conversions)
    {
        var station = new CookingStation();
        station.transform.position = new Vector3(1, 0, 0);
        station.m_conversion.AddRange(conversions);

        return station;
    }

    internal static CookingStation.ItemConversion Conversion(string prefabName, string itemName)
    {
        var drop = new ItemDrop
        {
            name = prefabName,
            m_itemData = Item(itemName, prefabName, 1)
        };

        return new CookingStation.ItemConversion
        {
            m_from = drop
        };
    }
}