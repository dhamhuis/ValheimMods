using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AutoRepair;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class AutoRepairPlugin : BaseUnityPlugin
{
    public const string PluginName = "AutoRepair";
    public const string PluginAuthor = "Hoose";

    // Changing this GUID renames the config file and breaks dependents; keep it stable after release.
    public const string PluginGuid = "hoose.valheim.autorepair";
    public const string PluginVersion = "1.0.0";

    private Harmony _harmony;

    internal static ManualLogSource Log { get; private set; }

    private void Awake()
    {
        Log = Logger;
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(AutoRepairPlugin).Assembly);

        var target = AccessTools.Method(
            typeof(CraftingStation),
            nameof(CraftingStation.Interact),
            [typeof(Humanoid), typeof(bool), typeof(bool)]);

        var patchInfo = target == null ? null : Harmony.GetPatchInfo(target);
        bool registered = patchInfo?.Postfixes.Any(patch =>
            patch.owner == PluginGuid
            && patch.PatchMethod.DeclaringType == typeof(CraftingStationRepairPatch)) == true;

        if (!registered)
        {
            _harmony.UnpatchSelf();
            Log.LogError("AutoRepair's crafting station patch was not registered; auto repair is disabled.");

            return;
        }

        Log.LogInfo($"{PluginName} {PluginVersion} loaded; repair patch verified.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();

        if (ReferenceEquals(Log, Logger))
        {
            Log = null;
        }
    }
}