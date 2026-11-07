using System;
using UnityEngine;

namespace SupplyFromStorage.Tests;

internal static class SupplyTestSetup
{
    internal static Player Reset()
    {
        NearbyStorage.Clear();
        SupplyTransfer.Clear();
        SupplyFromStoragePlugin.FuelAutomationEnabled = true;
        SupplyFromStoragePlugin.ProcessingAutomationEnabled = true;
        PrivateArea.Allowed = true;
        Time.time = 0;
        Time.realtimeSinceStartup = 0;

        var player = Player.m_localPlayer = new Player();
        player.transform.position = new Vector3(0, 0, 0);

        return player;
    }

    internal static ItemDrop.ItemData Item(string name, int count)
    {
        return new ItemDrop.ItemData
        {
            m_shared = new()
            {
                m_name = name
            },
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

    internal static StationItem StationDrop(string prefabName, string itemName)
    {
        return new StationItem
        {
            name = prefabName,
            m_itemData = Item(itemName, 1)
        };
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
