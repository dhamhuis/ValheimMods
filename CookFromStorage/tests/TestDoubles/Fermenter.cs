using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Fermenter
{
    public Transform transform = new();
    public bool isActiveAndEnabled = true;
    public ZNetView m_nview = new();
    public List<ItemConversion> m_conversion = new();
    public bool m_hasRoof = true;
    public bool m_exposed;

    public enum Status
    {
        Empty,
        Fermenting,
        Exposed,
        Ready
    }

    public int Content { get; set; }

    public bool ReadyForCollection { get; set; }

    public bool RejectInput { get; set; }

    public bool ThrowAfterInsertion { get; set; }

    public bool CheatedInput { get; private set; }

    public int AddedItems { get; private set; }

    public int Collections { get; private set; }

    public int CoverUpdates { get; private set; }

    public Action OnUpdateCover { get; set; }

    public Action OnInsert { get; set; }

    public Action OnReadContent { get; set; }

    public void UpdateCover(float dt, bool forceUpdate)
    {
        CoverUpdates++;
        OnUpdateCover?.Invoke();
    }

    public Status GetStatus()
    {
        return Content == 0 ? Status.Empty : (ReadyForCollection ? Status.Ready : Status.Fermenting);
    }

    public int GetContent()
    {
        OnReadContent?.Invoke();

        return Content;
    }

    public ItemDrop.ItemData FindCookableItem(Inventory inventory)
    {
        foreach (var conversion in m_conversion)
        {
            var item = inventory.GetAllItems().FirstOrDefault(candidate =>
                candidate?.m_shared?.m_name == conversion.m_from.m_itemData.m_shared.m_name
                && candidate.m_stack > 0);

            if (item != null)
            {
                return item;
            }
        }

        return null;
    }

    public bool IsItemAllowed(ItemDrop.ItemData item) => IsItemAllowed(item.m_dropPrefab.name.GetStableHashCode());

    public bool IsItemAllowed(int nameHash)
    {
        return m_conversion.Any(conversion => conversion.m_from.name.GetStableHashCode() == nameHash);
    }

    public void RPC_AddItem(long sender, int nameHash, bool cheated)
    {
        OnInsert?.Invoke();

        if (!m_nview.IsOwner() || RejectInput || GetStatus() != Status.Empty || !IsItemAllowed(nameHash))
        {
            return;
        }

        Content = nameHash;
        CheatedInput = cheated;
        AddedItems++;

        if (ThrowAfterInsertion)
        {
            throw new InvalidOperationException("Test callback failed after insertion.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold)
        {
            return false;
        }

        UpdateCover(0f, true);

        if (!PrivateArea.CheckAccess(transform.position, 0f, true, false))
        {
            return true;
        }

        if (GetStatus() == Status.Ready)
        {
            Collections++;
            Content = 0;
            ReadyForCollection = false;

            return true;
        }

        if (GetStatus() != Status.Empty)
        {
            return false;
        }

        if (!m_hasRoof || m_exposed)
        {
            string message = !m_hasRoof ? "$piece_fermenter_needroof" : "$piece_fermenter_exposed";
            user.Message(MessageHud.MessageType.Center, message, 0, null, false);

            return false;
        }

        if (user is not Player player)
        {
            return false;
        }

        var item = FindCookableItem(player.GetInventory());

        if (item == null)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems", 0, null, false);

            return false;
        }

        AddItem(user, item);

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return PrivateArea.CheckAccess(transform.position, 0f, true, false) && AddItem(user, item);
    }

    private bool AddItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (user is not Player player || GetStatus() != Status.Empty
            || !IsItemAllowed(item) || !player.GetInventory().RemoveItem(item, 1))
        {
            return false;
        }

        RPC_AddItem(0, item.m_dropPrefab.name.GetStableHashCode(), item.m_cheated);

        return true;
    }

    public sealed class ItemConversion
    {
        public ItemDrop m_from;
        public ItemDrop m_to;
        public int m_producedItems;
    }
}