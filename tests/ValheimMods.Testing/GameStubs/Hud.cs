using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Hud
{
    private int _updateIndex;

    public BuildUi m_buildUi = new();

    public List<Piece> Pieces { get; } = new();

    public List<bool> Available { get; } = new();

    public UnityEngine.GameObject[] m_requirementItems = [];

    // Mirrors the game's build tooltip: Cultivator plants use NoCraftCost and therefore pass craft=true. Later slots
    // (the game's crafting-station slot) are filled without SetupRequirement.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetupPieceInfo(Piece piece)
    {
        for (int i = 0; i < m_requirementItems.Length && i < piece.m_resources.Length; i++)
        {
            InventoryGui.SetupRequirement(
                m_requirementItems[i].transform,
                piece.m_resources[i],
                Player.m_localPlayer,
                piece.FreeBuildKey() == GlobalKeys.NoCraftCost,
                0,
                1);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void UpdateBuild(Player player, bool forceUpdateAllBuildStatuses)
    {
        if (!m_buildUi.gameObject.activeSelf || Pieces.Count == 0)
        {
            return;
        }

        _updateIndex %= Pieces.Count;
        Available[_updateIndex] = player.HaveRequirements(Pieces[_updateIndex], Player.RequirementMode.CanBuild);
        _updateIndex++;

        if (forceUpdateAllBuildStatuses)
        {
            for (int i = 0; i < Pieces.Count; i++)
            {
                Available[i] = player.HaveRequirements(Pieces[i], Player.RequirementMode.CanBuild);
            }

            _updateIndex = 0;
        }
    }
}