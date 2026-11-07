using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SupplyFromStorage;

internal static class SupplyTransfer
{
    private const float IdleRetryDelay = 5f;
    private const float StaleRetryDelay = 2f;

    private static readonly Dictionary<object, float> _fuelChecks = new();
    private static readonly Dictionary<object, float> _inputChecks = new();
    private static float _nextCleanup;

    internal static void Clear()
    {
        _fuelChecks.Clear();
        _inputChecks.Clear();
        _nextCleanup = 0;
    }

    private static bool HasLocalOwnership(ZNetView networkView)
    {
        return networkView.IsOwner();
    }

    internal static void FireplaceInteracted(
        Fireplace fireplace,
        Humanoid user,
        bool interactionReady,
        bool vanillaHandled)
    {
        if (!interactionReady || vanillaHandled || !SupplyFromStoragePlugin.FuelAutomationEnabled
            || user != Player.m_localPlayer || user is not Player player
            || !ReadyToCheck(_fuelChecks, fireplace))
        {
            return;
        }

        Defer(_fuelChecks, fireplace,
            CanUse(fireplace, player)
                ? TransferFireplaceFuel(fireplace, player, NearbyStorage.Find(player))
                : TransferResult.NotReady);
    }

    internal static void CookingStationFuelRequested(
        CookingStation station,
        Humanoid user,
        ItemDrop.ItemData item,
        bool vanillaHandled)
    {
        if (vanillaHandled || item != null || !SupplyFromStoragePlugin.FuelAutomationEnabled
            || user != Player.m_localPlayer || user is not Player player
            || !ReadyToCheck(_fuelChecks, station))
        {
            return;
        }

        Defer(_fuelChecks, station,
            CanUse(station, player)
                ? TransferCookingStationFuel(station, player, NearbyStorage.Find(player))
                : TransferResult.NotReady);
    }

    internal static void SmelterFuelRequested(
        Smelter smelter,
        Humanoid user,
        ItemDrop.ItemData item,
        bool vanillaHandled)
    {
        if (vanillaHandled || item != null || !SupplyFromStoragePlugin.FuelAutomationEnabled
            || user != Player.m_localPlayer || user is not Player player
            || !ReadyToCheck(_fuelChecks, smelter))
        {
            return;
        }

        Defer(_fuelChecks, smelter,
            CanUse(smelter, player)
                ? TransferSmelterFuel(smelter, player, NearbyStorage.Find(player))
                : TransferResult.NotReady);
    }

    internal static bool TryHandleSmelterInput(
        Smelter smelter,
        Humanoid user,
        ItemDrop.ItemData item,
        out bool handledResult)
    {
        handledResult = false;

        if (item != null || !SupplyFromStoragePlugin.ProcessingAutomationEnabled
            || user != Player.m_localPlayer || user is not Player player
            || HasProcessableInventoryItem(smelter, player))
        {
            return false;
        }

        if (!CanUse(smelter, player))
        {
            Defer(_inputChecks, smelter, TransferResult.NotReady);
            return false;
        }

        var sources = NearbyStorage.Find(player);
        bool hasStorageInput = HasAccessibleSmelterInput(smelter, player, sources);

        if (!ReadyToCheck(_inputChecks, smelter))
        {
            return hasStorageInput;
        }

        if (!hasStorageInput)
        {
            Defer(_inputChecks, smelter, TransferResult.NoItems);
            return false;
        }

        if (smelter.GetQueueSize() >= smelter.m_maxOre)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_itsfull", 0, null, false);
            Defer(_inputChecks, smelter, TransferResult.Full);
            return true;
        }

        var result = TransferSmelterInputs(smelter, player, sources);
        Defer(_inputChecks, smelter, result);

        if (result == TransferResult.Transferred)
        {
            handledResult = true;
            return true;
        }

        if (result == TransferResult.Failed && hasStorageInput)
        {
            return true;
        }

        return false;
    }

    internal static TransferResult TransferFireplaceFuel(
        Fireplace fireplace,
        Player player,
        IReadOnlyList<Container> sources)
    {
        if (!CanUse(fireplace, player) || !fireplace.m_canRefill || fireplace.m_infiniteFuel
            || fireplace.m_fuelItem == null)
        {
            return TransferResult.NotReady;
        }

        string fuelName = fireplace.m_fuelItem.m_itemData?.m_shared?.m_name;

        return TransferFuel(
            fireplace,
            player,
            sources,
            fuelName,
            fireplace.m_maxFuel,
            () => ReadFireplaceFuel(fireplace),
            () => fireplace.RPC_AddFuel(player.GetPlayerID()));
    }

    internal static TransferResult TransferCookingStationFuel(
        CookingStation station,
        Player player,
        IReadOnlyList<Container> sources)
    {
        if (!CanUse(station, player) || !station.m_useFuel || station.m_fuelItem == null)
        {
            return TransferResult.NotReady;
        }

        string fuelName = station.m_fuelItem.m_itemData?.m_shared?.m_name;

        return TransferFuel(
            station,
            player,
            sources,
            fuelName,
            station.m_maxFuel,
            station.GetFuel,
            () => station.RPC_AddFuel(player.GetPlayerID()));
    }

    internal static TransferResult TransferSmelterFuel(
        Smelter smelter,
        Player player,
        IReadOnlyList<Container> sources)
    {
        if (!CanUse(smelter, player) || smelter.m_fuelItem == null)
        {
            return TransferResult.NotReady;
        }

        string fuelName = smelter.m_fuelItem.m_itemData?.m_shared?.m_name;

        return TransferFuel(
            smelter,
            player,
            sources,
            fuelName,
            smelter.m_maxFuel,
            smelter.GetFuel,
            () => smelter.RPC_AddFuel(player.GetPlayerID()));
    }

    internal static TransferResult TransferSmelterInputs(
        Smelter smelter,
        Player player,
        IReadOnlyList<Container> sources)
    {
        if (!CanUse(smelter, player) || smelter.m_maxOre <= 0 || smelter.m_conversion == null)
        {
            return TransferResult.NotReady;
        }

        var acceptedInputs = new List<AcceptedInput>();

        foreach (var conversion in smelter.m_conversion)
        {
            var acceptedItem = conversion?.m_from;

            if (acceptedItem == null || acceptedItem.m_itemData?.m_shared == null
                || string.IsNullOrEmpty(acceptedItem.name) || !smelter.IsItemAllowed(acceptedItem.name))
            {
                continue;
            }

            acceptedInputs.Add(new AcceptedInput(
                acceptedItem.name,
                acceptedItem.m_itemData.m_shared.m_name));
        }

        if (acceptedInputs.Count == 0)
        {
            return TransferResult.NoItems;
        }

        if (smelter.GetQueueSize() >= smelter.m_maxOre)
        {
            return TransferResult.Full;
        }

        foreach (var container in sources)
        {
            if (!TryGetInventory(container, player, out var inventory))
            {
                continue;
            }

            foreach (var item in inventory.GetAllItems().ToArray())
            {
                if (item?.m_shared == null || item.m_stack <= 0)
                {
                    continue;
                }

                foreach (var accepted in acceptedInputs)
                {
                    if (item.m_shared.m_name != accepted.ItemName)
                    {
                        continue;
                    }

                    int before = smelter.GetQueueSize();
                    bool moved = ConsumeOne(
                        container,
                        inventory,
                        player,
                        smelter,
                        item,
                        () => smelter.IsItemAllowed(accepted.PrefabName)
                            && smelter.GetQueueSize() < smelter.m_maxOre,
                        () => smelter.RPC_AddOre(player.GetPlayerID(), accepted.PrefabName, false),
                        () => smelter.GetQueueSize() == before + 1);

                    return moved ? TransferResult.Transferred : TransferResult.Failed;
                }
            }
        }

        return TransferResult.NoItems;
    }

    private static TransferResult TransferFuel(
        object station,
        Player player,
        IReadOnlyList<Container> sources,
        string fuelName,
        float maximumFuel,
        Func<float> getFuel,
        Action addFuel)
    {
        if (string.IsNullOrEmpty(fuelName))
        {
            return TransferResult.NotReady;
        }

        if (!HasFuelSpace(getFuel(), maximumFuel))
        {
            return TransferResult.Full;
        }

        foreach (var container in sources)
        {
            if (!TryGetInventory(container, player, out var inventory))
            {
                continue;
            }

            foreach (var item in inventory.GetAllItems().ToArray())
            {
                if (item?.m_shared == null || item.m_shared.m_name != fuelName || item.m_stack <= 0)
                {
                    continue;
                }

                float before = getFuel();
                bool moved = ConsumeOne(
                    container,
                    inventory,
                    player,
                    station,
                    item,
                    () => HasFuelSpace(getFuel(), maximumFuel),
                    addFuel,
                    () => getFuel() > before);

                return moved ? TransferResult.Transferred : TransferResult.Failed;
            }
        }

        return TransferResult.NoItems;
    }

    private static bool ConsumeOne(
        Container container,
        Inventory inventory,
        Player player,
        object station,
        ItemDrop.ItemData item,
        Func<bool> canInsert,
        Action insert,
        Func<bool> confirmed)
    {
        if (!TryGetInventory(container, player, out var currentInventory)
            || currentInventory != inventory || !inventory.ContainsItem(item) || item.m_stack <= 0
            || !CanUse(station, player) || !canInsert())
        {
            SupplyFromStoragePlugin.Log.LogWarning("Storage or machine state changed before transfer; no input was added.");

            return false;
        }

        int originalStack = item.m_stack;
        int originalCount = CountItems(inventory, item.m_shared.m_name);
        container.m_inUse = true;

        try
        {
            if (!inventory.RemoveItem(item, 1))
            {
                SupplyFromStoragePlugin.Log.LogWarning("Storage item changed before transfer; no machine input was added.");

                return false;
            }

            Exception transferFailure = null;

            try
            {
                insert();
            }
            catch (Exception exception)
            {
                transferFailure = exception;
            }

            bool committed;

            try
            {
                committed = confirmed();
            }
            catch (Exception exception)
            {
                SupplyFromStoragePlugin.Log.LogError(
                    $"Could not confirm machine transfer; storage item remains consumed to avoid duplicate insertion. {exception}");

                return false;
            }

            if (committed)
            {
                if (transferFailure != null)
                {
                    SupplyFromStoragePlugin.Log.LogError(
                        $"Machine accepted the storage item, but its transfer callback threw after insertion. {transferFailure}");
                }

                return true;
            }

            bool restored;

            try
            {
                restored = RestoreItem(inventory, item, originalStack, originalCount);
            }
            catch (Exception exception)
            {
                SupplyFromStoragePlugin.Log.LogError($"Machine rejected an item and storage restoration threw. {exception}");

                throw;
            }

            SupplyFromStoragePlugin.Log.LogError(
                $"Machine rejected a storage transfer; source item restoration {(restored ? "succeeded" : "failed")}."
                + (transferFailure == null ? string.Empty : $" Transfer callback error: {transferFailure}"));

            return false;
        }
        finally
        {
            container.m_inUse = false;
        }
    }

    private static bool RestoreItem(Inventory inventory, ItemDrop.ItemData item, int originalStack, int originalCount)
    {
        item.m_stack = originalStack;

        if (!inventory.ContainsItem(item) && !inventory.AddItem(item))
        {
            return false;
        }

        return CountItems(inventory, item.m_shared.m_name) >= originalCount;
    }

    private static int CountItems(Inventory inventory, string itemName)
    {
        int count = 0;

        foreach (var item in inventory.GetAllItems())
        {
            if (item?.m_shared != null && item.m_shared.m_name == itemName)
            {
                count = checked(count + item.m_stack);
            }
        }

        return count;
    }

    private static float ReadFireplaceFuel(Fireplace fireplace)
    {
        return fireplace.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
    }

    private static bool TryGetInventory(Container container, Player player, out Inventory inventory)
    {
        inventory = null;

        if (!NearbyStorage.IsAccessible(container, player) || !container.IsOwner())
        {
            return false;
        }

        inventory = container.GetInventory();

        return inventory != null;
    }

    private static bool HasProcessableInventoryItem(Smelter smelter, Player player)
    {
        if (smelter?.m_conversion == null || player?.GetInventory() == null)
        {
            return false;
        }

        var inventoryItems = player.GetInventory().GetAllItems();

        foreach (var conversion in smelter.m_conversion)
        {
            string itemName = conversion?.m_from?.m_itemData?.m_shared?.m_name;

            if (!string.IsNullOrEmpty(itemName)
                && inventoryItems.Any(item => item?.m_shared?.m_name == itemName && item.m_stack > 0))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAccessibleSmelterInput(
        Smelter smelter,
        Player player,
        IReadOnlyList<Container> sources)
    {
        if (smelter?.m_conversion == null)
        {
            return false;
        }

        foreach (var container in sources)
        {
            if (!TryGetInventory(container, player, out var inventory))
            {
                continue;
            }

            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || item.m_stack <= 0)
                {
                    continue;
                }

                foreach (var conversion in smelter.m_conversion)
                {
                    var acceptedItem = conversion?.m_from;

                    if (acceptedItem?.m_itemData?.m_shared != null
                        && acceptedItem.m_itemData.m_shared.m_name == item.m_shared.m_name
                        && !string.IsNullOrEmpty(acceptedItem.name)
                        && smelter.IsItemAllowed(acceptedItem.name))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool CanUse(Smelter smelter, Player player)
    {
        return smelter != null && smelter.isActiveAndEnabled && smelter.m_nview != null
            && smelter.m_nview.IsValid() && HasLocalOwnership(smelter.m_nview) && InRange(smelter.transform.position, player);
    }

    private static bool CanUse(CookingStation station, Player player)
    {
        return station != null && station.isActiveAndEnabled && station.m_nview != null
            && station.m_nview.IsValid() && HasLocalOwnership(station.m_nview) && InRange(station.transform.position, player);
    }

    private static bool CanUse(Fireplace fireplace, Player player)
    {
        return fireplace != null && fireplace.isActiveAndEnabled && fireplace.m_nview != null
            && fireplace.m_nview.IsValid() && HasLocalOwnership(fireplace.m_nview) && InRange(fireplace.transform.position, player);
    }

    private static bool CanUse(object station, Player player)
    {
        return station is Smelter smelter ? CanUse(smelter, player)
            : station is CookingStation cookingStation ? CanUse(cookingStation, player)
            : station is Fireplace fireplace && CanUse(fireplace, player);
    }

    private static bool InRange(Vector3 position, Player player)
    {
        return player != null && (position - player.transform.position).sqrMagnitude <= NearbyStorage.Radius * NearbyStorage.Radius;
    }

    private static bool HasFuelSpace(float fuel, float maximum)
    {
        return Math.Ceiling(fuel) < maximum;
    }

    private static bool ReadyToCheck(Dictionary<object, float> checks, object station)
    {
        if (station == null)
        {
            return false;
        }

        float now = Time.realtimeSinceStartup;

        if (now >= _nextCleanup)
        {
            foreach (var pair in checks.Where(pair => pair.Value <= now).ToArray())
            {
                checks.Remove(pair.Key);
            }

            _nextCleanup = now + IdleRetryDelay;
        }

        return !checks.TryGetValue(station, out float retryAt) || now >= retryAt;
    }

    private static void Defer(Dictionary<object, float> checks, object station, TransferResult result)
    {
        if (result == TransferResult.Transferred)
        {
            checks.Remove(station);
            return;
        }

        float delay = result == TransferResult.Failed ? StaleRetryDelay : IdleRetryDelay;
        checks[station] = Time.realtimeSinceStartup + delay;
    }

    private sealed class AcceptedInput
    {
        internal AcceptedInput(string prefabName, string itemName)
        {
            PrefabName = prefabName;
            ItemName = itemName;
        }

        internal string PrefabName { get; }

        internal string ItemName { get; }
    }

    internal enum TransferResult
    {
        Transferred,
        NoItems,
        Full,
        NotReady,
        Failed
    }
}
