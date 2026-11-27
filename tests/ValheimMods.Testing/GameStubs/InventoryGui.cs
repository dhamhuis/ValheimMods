using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class InventoryGui
{
    public static InventoryGui instance = new();
    public Recipe m_craftRecipe;
    public ItemDrop.ItemData m_craftUpgradeItem;
    public (Recipe Recipe, ItemDrop.ItemData ItemData) m_selectedRecipe;
    public bool m_multiCrafting;
    public int m_multiCraftAmount;
    public GameObject[] m_recipeRequirementList = [];

    public static bool Visible { get; set; } = true;

    public Action<Player> OnCraft { get; set; }

    public int CraftPresses { get; private set; }

    public int RepairsRemaining { get; set; }

    public int Repairs { get; private set; }

    public static bool IsVisible() => Visible;

    public static bool LastRequirementMet { get; private set; }

    // Mirrors the game: shown requirements return true, res_amount holds the required amount, and the have-count only drives flashing.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool SetupRequirement(
        Transform elementRoot,
        Piece.Requirement req,
        Player player,
        bool craft,
        int quality,
        int craftMultiplier)
    {
        int required = req.GetAmount(quality) * craftMultiplier;

        if (required <= 0)
        {
            return false;
        }

        var amount = elementRoot?.Find("res_amount")?.GetComponent<TMP_Text>();

        if (amount != null)
        {
            amount.text = required.ToString();
        }

        LastRequirementMet = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name, -1, true)
            >= required;

        return true;
    }

    public void ShowSelectedRecipeRequirements(Player player) => SetupRequirementList(1, player, true, 1);

    // Mirrors the game: shown resources use SetupRequirement and surplus slots are hidden without it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SetupRequirementList(int quality, Player player, bool allowedQuality, int amount)
    {
        var resources = m_selectedRecipe.Recipe.m_resources;

        for (int i = 0; i < m_recipeRequirementList.Length && i < resources.Length; i++)
        {
            SetupRequirement(m_recipeRequirementList[i].transform, resources[i], player, true, quality, amount);
        }
    }

    // Mirrors UpdateRecipe with nothing selected: every slot is hidden without SetupRequirementList.
    public void ShowNoRecipeSelected()
    {
        foreach (var element in m_recipeRequirementList)
        {
            HideRequirement(element.transform);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void HideRequirement(Transform elementRoot)
    {
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