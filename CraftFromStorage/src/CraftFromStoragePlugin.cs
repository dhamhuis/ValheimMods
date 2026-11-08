using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ValheimMods.Common;
using ValheimMods.Common.Crafting;

namespace CraftFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class CraftFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "CraftFromStorage";
    public const string PluginAuthor = "Hoose";

    // Changing this GUID renames the config file and breaks dependents; keep it stable after release.
    public const string PluginGuid = "hoose.valheim.craftfromstorage";
    public const string PluginVersion = "1.0.0";

    private const string StorageRadiusDescription =
        "How far, in meters, CraftFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100.\n"
        + "Applies to recipes and upgrades at non-food crafting stations such as the Workbench, Forge, Stonecutter, Artisan Table, Black Forge and Galdr Table.\n"
        + "Cauldron, Food Preparation Table and Mead Ketill recipes are handled by CookFromStorage and use its own setting. Only storage you could normally open is used.";

    private static ConfigEntry<float> _storageRadius;
    private RecipeStorage _recipeStorage;

    internal static CraftFromStoragePlugin Instance { get; private set; }

    internal static ManualLogSource Log { get; private set; }

    internal static float StorageRadius => StorageRange.Clamp(_storageRadius?.Value ?? StorageRange.DefaultRadius);

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        _storageRadius = Config.Bind(
            "Storage",
            "Storage radius",
            StorageRange.DefaultRadius,
            new ConfigDescription(
                StorageRadiusDescription,
                new AcceptableValueRange<float>(StorageRange.MinimumRadius, StorageRange.MaximumRadius)));

        try
        {
            _recipeStorage = RecipeStorage.Register(
                false,
                coroutine => StartCoroutine(coroutine),
                StopAllCoroutines,
                message => Log.LogWarning(message),
                message => Log.LogError(message),
                () => true,
                () => StorageRadius);
        }
        catch (System.Exception exception)
        {
            Log.LogError($"{PluginName}: shared recipe-storage initialization failed. {exception}");

            throw;
        }

        Log.LogInfo($"{PluginName} {PluginVersion} loaded; non-food station crafting patches verified ({StorageRadius:0.#}m storage radius).");
    }

    private void OnDestroy()
    {
        _recipeStorage?.Dispose();
        StopAllCoroutines();

        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
            Log = null;
        }
    }
}