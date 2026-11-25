using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ValheimMods.Common;

namespace BuildFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class BuildFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "BuildFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.buildfromstorage";
    public const string PluginVersion = "1.0.0";

    private const string StorageRadiusDescription =
        "How far, in meters, BuildFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100. Only closed storage currently owned by your client is used.";

    private static ConfigEntry<float> _storageRadius;
    private Harmony _harmony;

    internal static BuildStorageRegistry StorageRegistry { get; } = new();

    internal static ManualLogSource Log { get; private set; }

    internal static float StorageRadius => StorageRange.Clamp(_storageRadius?.Value ?? StorageRange.DefaultRadius);

    private void Awake()
    {
        Log = Logger;
        _storageRadius = Config.Bind(
            "Storage",
            "Storage radius",
            StorageRange.DefaultRadius,
            new ConfigDescription(
                StorageRadiusDescription,
                new AcceptableValueRange<float>(StorageRange.MinimumRadius, StorageRange.MaximumRadius)));

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(BuildFromStoragePlugin).Assembly);

        if (!PatchesRegistered())
        {
            _harmony.UnpatchSelf();
            Log.LogError("BuildFromStorage's build patches were not registered; storage building is disabled.");

            return;
        }

        StorageRegistry.Initialize();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded; Hammer building uses nearby owned storage ({StorageRadius:0.#}m radius).");
    }

    private void OnDestroy()
    {
        BuildStorageContext.Reset();
        StorageRegistry.Clear();
        _harmony?.UnpatchSelf();

        if (ReferenceEquals(Log, Logger))
        {
            Log = null;
        }
    }

    internal static void LogWarning(string message) => Log.LogWarning(message);

    internal static void LogError(string message) => Log.LogError(message);

    private static bool PatchesRegistered()
    {
        var patches = new (MethodBase Target, Type Patch)[]
        {
            (AccessTools.Method(typeof(Player), nameof(Player.UpdatePlacement), [typeof(bool), typeof(float)]), typeof(BuildPlacementContextPatch)),
            (AccessTools.Method(typeof(Player), nameof(Player.HaveRequirements), [typeof(Piece), typeof(Player.RequirementMode)]), typeof(BuildRequirementPatch)),
            (AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems), [typeof(string), typeof(int), typeof(bool)]), typeof(BuildCostCountPatch)),
            (AccessTools.Method(typeof(Player), nameof(Player.TryPlacePiece), [typeof(Piece)]), typeof(BuildCostPlanPatch)),
            (AccessTools.Method(typeof(Player), nameof(Player.ConsumeResources), [typeof(Piece.Requirement[]), typeof(int), typeof(int), typeof(int)]), typeof(BuildCostConsumePatch)),
            (AccessTools.Method(typeof(Container), nameof(Container.Awake)), typeof(ContainerCreatedPatch)),
            (AccessTools.Method(typeof(Container), nameof(Container.OnDestroyed)), typeof(ContainerDestroyedPatch))
        };

        return patches.All(patch =>
        {
            var info = patch.Target == null ? null : Harmony.GetPatchInfo(patch.Target);

            return info != null && info.Prefixes.Concat(info.Postfixes).Concat(info.Finalizers).Any(entry =>
                entry.owner == PluginGuid && entry.PatchMethod.DeclaringType == patch.Patch);
        });
    }
}