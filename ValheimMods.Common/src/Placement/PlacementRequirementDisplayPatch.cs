using HarmonyLib;
using UnityEngine;

namespace ValheimMods.Common.Placement;

[HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
internal static class PlacementRequirementDisplayPatch
{
    [HarmonyPrefix]
    private static void ClearStorageAmount(Transform elementRoot) => RequirementAmountText.Clear(elementRoot);

    [HarmonyPostfix]
    private static void ShowStorageAmount(Transform elementRoot, Piece.Requirement req, Player player, bool __result)
    {
        var context = PlacementPieceInfoPatch.DisplayScope;

        if (!__result || context == null || context != PlacementStorageContext.Current || context.Player != player
            || req == null || req.m_resItem == null)
        {
            return;
        }

        string name = req.m_resItem.m_itemData.m_shared.m_name;
        int carried;
        int stored = 0;
        bool suppressed = context.SuppressCountAugmentation;
        context.SuppressCountAugmentation = true;

        try
        {
            carried = player.GetInventory().CountItems(name, -1, true);

            foreach (var container in context.Containers)
            {
                var inventory = container.GetInventory();

                if (inventory != null && context.IsAccessible(container))
                {
                    stored = checked(stored + inventory.CountItems(name, -1, true));
                }
            }
        }
        finally
        {
            context.SuppressCountAugmentation = suppressed;
        }

        RequirementAmountText.Apply(elementRoot, carried, stored, true, true);
    }
}