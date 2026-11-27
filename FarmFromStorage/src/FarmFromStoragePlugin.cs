using BepInEx;
using BepInEx.Configuration;
using ValheimMods.Common;
using ValheimMods.Common.Placement;

namespace FarmFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
[BepInDependency("hoose.valheim.buildfromstorage", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class FarmFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "FarmFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.farmfromstorage";
    public const string PluginVersion = "1.0.0";

    private const string StorageRadiusDescription =
        "How far, in meters, FarmFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100. Only closed storage currently owned by your client is used.";

    private ConfigEntry<float> _storageRadius;
    private PlacementStorage _placementStorage;

    private float StorageRadius => StorageRange.Clamp(_storageRadius?.Value ?? StorageRange.DefaultRadius);

    private void Awake()
    {
        _storageRadius = Config.Bind(
            "Storage",
            "Storage radius",
            StorageRange.DefaultRadius,
            new ConfigDescription(
                StorageRadiusDescription,
                new AcceptableValueRange<float>(StorageRange.MinimumRadius, StorageRange.MaximumRadius)));

        _placementStorage = PlacementStorage.Register("Cultivator", () => StorageRadius, Logger.LogWarning, Logger.LogError);
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded; Cultivator planting uses nearby owned storage ({StorageRadius:0.#}m radius).");
    }

    private void OnDestroy()
    {
        _placementStorage?.Dispose();
    }
}