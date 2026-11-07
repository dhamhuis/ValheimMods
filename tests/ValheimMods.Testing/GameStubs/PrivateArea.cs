public static class PrivateArea
{
    public static bool Allowed { get; set; } = true;

    public static bool CheckAccess(UnityEngine.Vector3 position, float radius, bool flash, bool ward) => Allowed;
}