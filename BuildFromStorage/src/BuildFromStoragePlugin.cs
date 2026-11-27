using BepInEx;
using BepInEx.Configuration;
using ValheimMods.Common;
using ValheimMods.Common.Placement;

namespace BuildFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class BuildFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "BuildFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.buildfromstorage";
    public const string PluginVersion = "1.0.1";

    private const string StorageRadiusDescription =
        "How far, in meters, BuildFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100. Only closed storage currently owned by your client is used.";

    private static ConfigEntry<float> _storageRadius;
    private PlacementStorage _placementStorage;

    internal static float StorageRadius => StorageRange.Clamp(_storageRadius?.Value ?? StorageRange.DefaultRadius);

    private void Awake()
    {
        _storageRadius = Config.Bind(
            "Storage",
            "Storage radius",
            StorageRange.DefaultRadius,
            new ConfigDescription(
                StorageRadiusDescription,
                new AcceptableValueRange<float>(StorageRange.MinimumRadius, StorageRange.MaximumRadius)));

        _placementStorage = PlacementStorage.Register("Hammer", () => StorageRadius, Logger.LogWarning, Logger.LogError);
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded; Hammer building uses nearby owned storage ({StorageRadius:0.#}m radius).");
    }

    private void OnDestroy()
    {
        _placementStorage?.Dispose();
    }
}