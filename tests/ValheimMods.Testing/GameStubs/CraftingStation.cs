using System.Runtime.CompilerServices;

public class CraftingStation
{
    public bool m_upgrader;
    public string m_name = "$piece_workbench";

    public bool Usable { get; set; } = true;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Interact(Humanoid user, bool repeat, bool alt)
    {
        if (!repeat && Usable && user == Player.m_localPlayer)
        {
            Player.m_localPlayer.Station = this;
        }

        return false;
    }
}