using System;

namespace ValheimMods.Common;

public static class StorageRange
{
    public const float DefaultRadius = 20f;
    public const float MinimumRadius = 1f;
    public const float MaximumRadius = 100f;

    // Station checks only confirm the player is near the machine they used; they must not shrink with a small storage radius.
    public const float StationReach = 20f;

    public static float Clamp(float radius)
    {
        return float.IsNaN(radius) ? DefaultRadius : Math.Min(MaximumRadius, Math.Max(MinimumRadius, radius));
    }
}