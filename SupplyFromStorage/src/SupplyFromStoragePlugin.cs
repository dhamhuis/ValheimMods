using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ValheimMods.Common;

namespace SupplyFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class SupplyFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "SupplyFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.supplyfromstorage";
    public const string PluginVersion = "1.0.1";

    private const string StorageRadiusDescription =
        "How far, in meters, SupplyFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100.\n"
        + "Applies to both 'Automatic fuel' and 'Automatic processing inputs'. Only storage you could normally open is used.";

    private const string AutomaticFuelDescription =
        "When you press E on a machine's fuel slot and have no accepted fuel in your inventory, add one accepted fuel item from accessible storage within 'Storage radius'.\n"
        + "Covers:\n"
        + "- Campfires, bonfires, hearths, fireplaces, braziers, torches and other fires.\n"
        + "- Cooking-station and Stone Oven fuel, and the Frost Foundry's liquid frost. Never cooking ingredients or Foundry casts.\n"
        + "- Smelter and Blast Furnace fuel (coal).\n"
        + "- Hot Tub (wood) and Frost Kiln (ice).\n"
        + "Your inventory is always used first. Disabling this does not affect 'Automatic processing inputs'.";

    private const string AutomaticProcessingInputsDescription =
        "When you press E on a machine's input slot and have no accepted input in your inventory, add one accepted item from accessible storage within 'Storage radius'.\n"
        + "Covers:\n"
        + "- Ore and scrap for the Smelter and Blast Furnace.\n"
        + "- Wood for the Charcoal Kiln.\n"
        + "- Barley for the Windmill and flax for the Spinning Wheel.\n"
        + "- Eitr Refinery inputs.\n"
        + "- Wood, Fine Wood, Core Wood or Ashwood for the Battering Ram engine.\n"
        + "Each machine's own accepted-item list decides what can be added. Your inventory is always used first. Disabling this does not affect 'Automatic fuel'.";

    private Harmony _harmony;
    private static ConfigEntry<bool> _automaticFuel;
    private static ConfigEntry<bool> _automaticProcessingInputs;
    private static ConfigEntry<float> _storageRadius;

    internal static ManualLogSource Log { get; private set; }

    internal static bool FuelAutomationEnabled => _automaticFuel?.Value == true;

    internal static bool ProcessingAutomationEnabled => _automaticProcessingInputs?.Value == true;

    internal static float StorageRadius => _storageRadius?.Value ?? StorageRange.DefaultRadius;

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
        _automaticFuel = Config.Bind(
            "Automation",
            "Automatic fuel",
            true,
            AutomaticFuelDescription);
        _automaticProcessingInputs = Config.Bind(
            "Automation",
            "Automatic processing inputs",
            true,
            AutomaticProcessingInputsDescription);

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

        Log.LogInfo($"{PluginName} {PluginVersion} loaded; E interactions add one accepted fuel or processing input from nearby accessible storage ({NearbyStorage.Radius:0.#}m).");
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