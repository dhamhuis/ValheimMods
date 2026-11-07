using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SupplyFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class SupplyFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "SupplyFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "dhamhuis.valheim.supplyfromstorage";
    public const string PluginVersion = "1.0.0";

    private Harmony _harmony;
    private static ConfigEntry<bool> _automaticFuel;
    private static ConfigEntry<bool> _automaticProcessingInputs;

    internal static ManualLogSource Log { get; private set; }

    internal static bool FuelAutomationEnabled => _automaticFuel?.Value == true;

    internal static bool ProcessingAutomationEnabled => _automaticProcessingInputs?.Value == true;

    private void Awake()
    {
        Log = Logger;
        _automaticFuel = Config.Bind(
            "Automation",
            "Automatic fuel",
            true,
            "Add one accepted fuel item from accessible nearby storage on each E interaction.");
        _automaticProcessingInputs = Config.Bind(
            "Automation",
            "Automatic processing inputs",
            true,
            "Add one accepted machine input from accessible nearby storage on each E interaction.");

        NearbyStorage.Initialize();
        SupplyTransfer.Clear();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(SupplyFromStoragePlugin).Assembly);

        bool registered = IsRegistered(typeof(Fireplace), "Interact", typeof(FireplaceInteractPatch))
            && IsRegistered(typeof(CookingStation), "OnAddFuelSwitch", typeof(CookingStationFuelPatch))
            && IsRegistered(typeof(Smelter), "OnAddFuel", typeof(SmelterFuelPatch))
            && IsRegistered(typeof(Smelter), "OnAddOre", typeof(SmelterInputPatch))
            && IsRegistered(typeof(Container), "Awake", typeof(ContainerCreatedPatch))
            && IsRegistered(typeof(Container), "OnDestroyed", typeof(ContainerDestroyedPatch));

        if (!registered)
        {
            _harmony.UnpatchSelf();
            Log.LogError($"{PluginName}: one or more station/storage patches were not registered; automation is disabled.");

            return;
        }

        Log.LogInfo($"{PluginName} {PluginVersion} loaded; E interactions add one accepted fuel or processing input from nearby accessible storage (20m).");
    }

    private void OnDestroy()
    {
        SupplyTransfer.Clear();
        NearbyStorage.Clear();
        _harmony?.UnpatchSelf();

        if (ReferenceEquals(Log, Logger))
        {
            Log = null;
        }
    }

    private static bool IsRegistered(Type targetType, string methodName, Type patchType)
    {
        var target = AccessTools.Method(targetType, methodName);
        var patchInfo = target == null ? null : Harmony.GetPatchInfo(target);

        return patchInfo?.Prefixes.Concat(patchInfo.Postfixes)
            .Any(patch => patch.owner == PluginGuid && patch.PatchMethod.DeclaringType == patchType) == true;
    }
}
