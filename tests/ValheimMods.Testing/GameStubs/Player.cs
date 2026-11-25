using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Player : Humanoid
{
    public enum RequirementMode
    {
        CanBuild,
        IsKnown,
        CanAlmostBuild
    }

    public static Player m_localPlayer;
    public UnityEngine.Transform transform = new();
    public float m_maxInteractDistance = 4f;

    public Inventory Inventory { get; set; } = new();

    public HashSet<string> KnownMaterials { get; } = new(StringComparer.Ordinal);

    public CraftingStation Station { get; set; } = new();

    public bool Dead { get; set; }

    public bool NoCost { get; set; }

    public bool NoPlacementCost { get; set; }

    public bool PlacementAllowed { get; set; } = true;

    public int PlacedPieces { get; private set; }

    public Piece SelectedPiece { get; set; }

    public bool PlacementCostDisabled => NoPlacementCost;

    public Action OnRequirementCheck { get; set; }

    public Action OnPlacementStart { get; set; }

    public UnityEngine.GameObject HoverObject { get; set; }

    public UnityEngine.GameObject GetHoverObject() => HoverObject;

    public Inventory GetInventory() => Inventory;

    public bool IsMaterialKnown(string sharedName) => KnownMaterials.Contains(sharedName);

    public CraftingStation GetCurrentCraftingStation() => Station;

    public long GetPlayerID() => 1;

    public bool IsDead() => Dead;

    public bool NoCostCheat() => NoCost;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool HaveRequirements(Piece piece, RequirementMode mode)
    {
        if (ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
        {
            return true;
        }

        if (mode != RequirementMode.CanBuild)
        {
            return true;
        }

        foreach (var requirement in piece.m_resources)
        {
            if (requirement.m_resItem == null || requirement.m_amount <= 0)
            {
                continue;
            }

            string name = requirement.m_resItem.m_itemData.m_shared.m_name;

            if (Inventory.CountItems(name, -1, true) < requirement.GetAmount(0))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryPlacePiece(Piece piece)
    {
        if (PlacementAllowed)
        {
            PlacedPieces++;
        }

        return PlacementAllowed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
    {
        var station = GetCurrentCraftingStation();

        foreach (var requirement in requirements)
        {
            if ((station != null && station.m_upgrader != requirement.m_upgraderResource)
                || (station == null && requirement.m_upgraderResource) || requirement.m_resItem == null)
            {
                continue;
            }

            int amount = requirement.GetAmount(qualityLevel) * multiplier;

            if (amount > 0)
            {
                string name = requirement.m_resItem.m_itemData.m_shared.m_name;
                Inventory.RemoveItem(name, amount, itemQuality, true);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UpdatePlacement(bool takeInput, float dt)
    {
        if (!takeInput || SelectedPiece == null)
        {
            return;
        }

        OnPlacementStart?.Invoke();

        if (!PlacementCostDisabled && !HaveRequirements(SelectedPiece, RequirementMode.CanBuild))
        {
            return;
        }

        if (!TryPlacePiece(SelectedPiece))
        {
            return;
        }

        if (!ZoneSystem.instance.GetGlobalKey(SelectedPiece.FreeBuildKey()))
        {
            ConsumeResources(SelectedPiece.m_resources, 0, -1, 1);
        }
    }

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