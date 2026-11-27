namespace ValheimMods.Common.Placement;

internal sealed class PlacementPieceInfoState
{
    internal PlacementPieceInfoState(PlacementStorageContext.ScopeToken token, PlacementStorageContext.Scope previousDisplayScope)
    {
        Token = token;
        PreviousDisplayScope = previousDisplayScope;
    }

    internal PlacementStorageContext.ScopeToken Token { get; }

    internal PlacementStorageContext.Scope PreviousDisplayScope { get; }
}