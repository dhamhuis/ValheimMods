using System;
using System.Runtime.CompilerServices;

public class Player : Humanoid
{
    public static Player m_localPlayer;
    public UnityEngine.Transform transform = new();
    public float m_maxInteractDistance = 4f;

    public Inventory Inventory { get; set; } = new();

    public CraftingStation Station { get; set; } = new();

    public bool Dead { get; set; }

    public bool NoCost { get; set; }

    public Action OnRequirementCheck { get; set; }

    public Inventory GetInventory() => Inventory;

    public CraftingStation GetCurrentCraftingStation() => Station;

    public long GetPlayerID() => 1;

    public bool IsDead() => Dead;

    public bool NoCostCheat() => NoCost;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool HaveRequirementItems(Recipe piece, bool discover, int quality, int multiplier)
    {
        OnRequirementCheck?.Invoke();

        foreach (var requirement in piece.m_resources)
        {
            if (Inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, -1, true)
                < requirement.GetAmount(quality) * multiplier)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ItemDrop.ItemData GetFirstRequiredItem(
        Inventory inventory,
        Recipe recipe,
        int qualityLevel,
        out int amount,
        out int extraAmount,
        int craftMultiplier)
    {
        amount = 0;
        extraAmount = 0;

        return null;
    }
}