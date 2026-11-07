public class ZoneSystem
{
    public static ZoneSystem instance = new();

    public bool NoCost { get; set; }

    public bool GetGlobalKey(GlobalKeys key) => NoCost;
}