using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CraftFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class CraftFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "CraftFromStorage";
    public const string PluginAuthor = "dhamhuis";
    public const string PluginGuid = PluginAuthor + ".valheim.craftfromstorage";
    public const string PluginVersion = "1.0.0";

    private Harmony _harmony;

    internal static CraftFromStoragePlugin Instance { get; private set; }

    internal static ManualLogSource Log { get; private set; }

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(CraftFromStoragePlugin).Assembly);
        var patchMethods = typeof(CraftFromStoragePlugin).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            .Where(method => method.IsDefined(typeof(HarmonyPrefix), false)
                || method.IsDefined(typeof(HarmonyPostfix), false)
                || method.IsDefined(typeof(HarmonyFinalizer), false));

        foreach (var patchMethod in patchMethods)
        {
            bool registered = _harmony.GetPatchedMethods().Any(target =>
            {
                var info = Harmony.GetPatchInfo(target);

                return info.Prefixes.Concat(info.Postfixes).Concat(info.Finalizers)
                    .Any(patch => patch.owner == PluginGuid && patch.PatchMethod == patchMethod);
            });

            if (!registered)
            {
                _harmony.UnpatchSelf();
                Log.LogError($"{PluginName}: {patchMethod.DeclaringType.Name}.{patchMethod.Name} was not registered; storage crafting is disabled.");

                return;
            }
        }

        NearbyStorage.Initialize();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded; station crafting patches verified (20m storage radius).");
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        StorageCrafting.Clear();
        _harmony?.UnpatchSelf();
        NearbyStorage.Clear();

        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
            Log = null;
        }
    }
}