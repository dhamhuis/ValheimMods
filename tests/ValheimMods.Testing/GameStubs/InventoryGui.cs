using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class InventoryGui
{
    public static InventoryGui instance = new();
    public Recipe m_craftRecipe;
    public ItemDrop.ItemData m_craftUpgradeItem;
    public (Recipe Recipe, ItemDrop.ItemData ItemData) m_selectedRecipe;
    public bool m_multiCrafting;
    public int m_multiCraftAmount;

    public static bool Visible { get; set; } = true;

    public Action<Player> OnCraft { get; set; }

    public int CraftPresses { get; private set; }

    public int RepairsRemaining { get; set; }

    public int Repairs { get; private set; }

    public static bool IsVisible() => Visible;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool SetupRequirement(
        Transform element,
        Piece.Requirement requirement,
        Player player,
        bool craft,
        int quality,
        int multiplier)
    {
        return player.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, -1, true)
            >= (requirement.GetAmount(quality) * multiplier);
    }

    public bool HaveRepairableItems() => RepairsRemaining > 0;

    public void RepairOneItem()
    {
        RepairsRemaining--;
        Repairs++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DoCrafting(Player player) => OnCraft(player);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnCraftPressed() => CraftPresses++;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnCraftCancelPressed()
    {
    }
}