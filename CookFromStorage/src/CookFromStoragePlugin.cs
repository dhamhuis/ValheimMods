using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ValheimMods.Common;
using ValheimMods.Common.Crafting;

namespace CookFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class CookFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "CookFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.cookfromstorage";
    public const string PluginVersion = "1.0.0";

    private const string StorageRadiusDescription =
        "How far, in meters, CookFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100.\n"
        + "Applies to food recipes, cooking stations, the Stone Oven and the Fermenter. Only storage you could normally open is used.";

    private const string AutomaticIngredientsDescription =
        "Use accessible storage within 'Storage radius' for food and brewing ingredients you do not have in your inventory.\n"
        + "Covers:\n"
        + "- Recipes at the Cauldron, Food Preparation Table and Mead Ketill (storage counts toward requirements and is consumed when you craft).\n"
        + "- Raw food for the Cooking Station, Iron Cooking Station and Stone Oven: one item per E press.\n"
        + "- Mead bases for an empty Fermenter: one per E press.\n"
        + "Your inventory is always used first. Frost Foundry casts are never loaded from storage. Fuel is handled by SupplyFromStorage, not this setting.";

    private static ConfigEntry<bool> _automaticIngredients;
    private static ConfigEntry<float> _storageRadius;
    private Harmony _harmony;
    private RecipeStorage _recipeStorage;

    internal static ManualLogSource Log { get; private set; }

    internal static bool AutomaticIngredientsEnabled => _automaticIngredients?.Value == true;

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
        _automaticIngredients = Config.Bind(
            "Automation",
            "Automatic ingredients",
            true,
            AutomaticIngredientsDescription);
        NearbyStorage.Initialize();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(CookFromStoragePlugin).Assembly);

        var target = AccessTools.Method(
            typeof(CookingStation),
            "OnInteract",
            [typeof(Humanoid)]);
        var patchInfo = target == null ? null : Harmony.GetPatchInfo(target);
        bool foodPatchRegistered = patchInfo?.Prefixes.Any(patch =>
            patch.owner == PluginGuid && patch.PatchMethod.DeclaringType == typeof(CookingStationPatch)) == true;
        bool storagePatchesRegistered = IsRegistered(typeof(Container), "Awake", typeof(ContainerCreatedPatch))
            && IsRegistered(typeof(Container), "OnDestroyed", typeof(ContainerDestroyedPatch));
        bool fermenterPatchRegistered = IsRegistered(typeof(Fermenter), "Interact", typeof(FermenterPatch));

        if (!foodPatchRegistered || !storagePatchesRegistered || !fermenterPatchRegistered)
        {
            _harmony.UnpatchSelf();
            NearbyStorage.Clear();
            Log.LogError($"{PluginName}: one or more cooking/storage patches were not registered; automation is disabled.");

            return;
        }

        try
        {
            _recipeStorage = RecipeStorage.Register(
                true,
                coroutine => StartCoroutine(coroutine),
                StopAllCoroutines,
                message => Log.LogWarning(message),
                message => Log.LogError(message),
                () => AutomaticIngredientsEnabled,
                () => StorageRadius);
        }
        catch (System.Exception exception)
        {
            _harmony.UnpatchSelf();
            NearbyStorage.Clear();
            Log.LogError($"{PluginName}: shared food recipe-storage initialization failed. {exception}");

            throw;
        }

        Log.LogInfo($"{PluginName} {PluginVersion} loaded; food recipes, cooking-station inputs and Fermenter bases use storage ({NearbyStorage.Radius:0.#}m).");
    }

    private void OnDestroy()
    {
        _recipeStorage?.Dispose();
        StopAllCoroutines();
        NearbyStorage.Clear();
        _harmony?.UnpatchSelf();

        if (ReferenceEquals(Log, Logger))
        {
            Log = null;
        }
    }

    private static bool IsRegistered(System.Type targetType, string methodName, System.Type patchType)
    {
        var target = AccessTools.Method(targetType, methodName);
        var patchInfo = target == null ? null : Harmony.GetPatchInfo(target);

        return patchInfo?.Prefixes.Concat(patchInfo.Postfixes)
            .Any(patch => patch.owner == PluginGuid && patch.PatchMethod.DeclaringType == patchType) == true;
    }
}