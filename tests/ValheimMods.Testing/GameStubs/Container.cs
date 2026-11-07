using System.Runtime.CompilerServices;

public class Container
{
    public UnityEngine.Transform transform = new();
    public bool isActiveAndEnabled = true;
    public bool m_inUse;
    public bool m_checkGuardStone;
    public ZNetView m_nview = new();
    public Vagon m_wagon;
    private static int _nextId;
    private readonly int _id = ++_nextId;

    public bool Allowed { get; set; } = true;

    public Inventory Inventory { get; set; } = new();

    public int Opened { get; private set; }

    public Inventory GetInventory() => Inventory;

    public bool IsInUse() => m_inUse;

    public bool IsOwner() => m_nview.Owner;

    public bool CheckAccess(long id) => Allowed;

    public bool Load() => false;

    public int GetInstanceID() => _id;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Awake()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnDestroyed()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (!hold && Allowed && (!m_checkGuardStone || PrivateArea.Allowed))
        {
            m_nview.InvokeRPC("RPC_RequestOpen", Player.m_localPlayer.GetPlayerID());
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RPC_OpenResponse(long sender, bool granted)
    {
        if (granted)
        {
            Opened++;
        }
    }
}