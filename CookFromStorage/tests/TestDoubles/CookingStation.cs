using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CookingStation
{
    public Transform transform = new();
    public bool isActiveAndEnabled = true;
    public ZNetView m_nview = new();
    public Transform[] m_slots = [new Transform(), new Transform()];
    public List<ItemConversion> m_conversion = new();
    public bool m_requireFire;
    public Switch m_addFoodSwitch;
    public bool FireLit = true;
    public bool DoneItem;
    public bool RejectInput;
    public string m_name = "$piece_cookingstation";

    public string name { get; set; } = "CookingStation";

    public List<string> AddedItems { get; } = new();

    public bool HaveDoneItem() => DoneItem;

    public ItemDrop.ItemData FindCookableItem(Inventory inventory)
    {
        foreach (var conversion in m_conversion)
        {
            string itemName = conversion?.m_from?.m_itemData?.m_shared?.m_name;
            var item = inventory.GetAllItems().FirstOrDefault(candidate =>
                candidate?.m_shared?.m_name == itemName && candidate.m_stack > 0);

            if (item != null)
            {
                return item;
            }
        }

        return null;
    }

    public int GetFreeSlot()
    {
        int nextSlot = AddedItems.Count;

        return nextSlot < m_slots.Length ? nextSlot : -1;
    }

    public bool IsFireLit() => FireLit;

    public bool IsItemAllowed(string itemName)
    {
        return m_conversion.Any(conversion => conversion?.m_from?.name == itemName);
    }

    public void RPC_AddItem(long sender, string itemName, bool cheated)
    {
        if (!m_nview.IsOwner() || RejectInput || !IsItemAllowed(itemName) || GetFreeSlot() < 0)
        {
            return;
        }

        AddedItems.Add(itemName);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool OnAddFoodSwitch(Switch caller, Humanoid user, ItemDrop.ItemData item)
    {
        if (item != null)
        {
            return AddPlayerItem(user, item);
        }

        return OnInteract(user);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || m_addFoodSwitch != null)
        {
            return false;
        }

        return OnInteract(user);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool OnInteract(Humanoid user)
    {
        if (DoneItem)
        {
            DoneItem = false;
            return true;
        }

        if (user is not Player player)
        {
            return false;
        }

        var ingredient = FindCookableItem(player.GetInventory());
        if (ingredient == null)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_nocookableitems", 0, null, false);
            return false;
        }

        return AddPlayerItem(user, ingredient);
    }

    private bool AddPlayerItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (user is not Player player || (m_requireFire && !FireLit) || GetFreeSlot() < 0
            || !IsItemAllowed(item.m_dropPrefab.name))
        {
            return false;
        }

        player.GetInventory().RemoveItem(item, 1);
        RPC_AddItem(player.GetPlayerID(), item.m_dropPrefab.name, false);

        return true;
    }

    public sealed class ItemConversion
    {
        public ItemDrop m_from;
        public ItemDrop m_to;
    }
}