using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ValheimMods.Common.Placement;

public sealed class PlacementStorage : IDisposable
{
    private const string HarmonyOwner = "hoose.valheim.sharedplacementstorage";

    private static readonly Dictionary<string, PlacementStorage> _clients = new(StringComparer.Ordinal);
    private static Harmony _harmony;
    private readonly string _toolPrefab;
    private readonly Func<float> _radius;
    private readonly Action<string> _warning;
    private readonly Action<string> _error;

    private PlacementStorage(string toolPrefab, Func<float> radius, Action<string> warning, Action<string> error)
    {
        _toolPrefab = toolPrefab;
        _radius = radius;
        _warning = warning;
        _error = error;
    }

    internal static PlacementStorageRegistry Registry { get; } = new();

    internal bool IsCultivator => _toolPrefab == "Cultivator";

    internal float Radius => StorageRange.Clamp(_radius());

    public static PlacementStorage Register(string toolPrefab, Func<float> radius, Action<string> warning, Action<string> error)
    {
        if (string.IsNullOrWhiteSpace(toolPrefab))
        {
            throw new ArgumentException("A placement tool prefab name is required.", nameof(toolPrefab));
        }

        if (radius == null || warning == null || error == null)
        {
            throw new ArgumentNullException(nameof(radius), "Placement-storage callbacks must not be null.");
        }

        if (_clients.ContainsKey(toolPrefab))
        {
            throw new InvalidOperationException($"A placement-storage client is already registered for {toolPrefab}.");
        }

        if (_clients.Count == 0)
        {
            var countMethod = AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems), [typeof(string), typeof(int), typeof(bool)]);
            var legacyPatches = Harmony.GetPatchInfo(countMethod);

            if (legacyPatches != null && legacyPatches.Owners.Contains("hoose.valheim.buildfromstorage"))
            {
                throw new InvalidOperationException("An older BuildFromStorage is installed. Update BuildFromStorage and all ValheimMods.Common.dll copies before enabling FarmFromStorage.");
            }

            var harmony = new Harmony(HarmonyOwner);

            try
            {
                var patchTypes = typeof(PlacementStorage).Assembly.GetTypes().Where(type =>
                    type.Namespace == typeof(PlacementStorage).Namespace
                    && type.IsDefined(typeof(HarmonyPatch), false)).ToArray();

                foreach (var type in patchTypes)
                {
                    harmony.CreateClassProcessor(type).Patch();
                }

                var patchMethods = patchTypes.SelectMany(type =>
                    type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)).Where(method =>
                    method.IsDefined(typeof(HarmonyPrefix), false)
                    || method.IsDefined(typeof(HarmonyPostfix), false)
                    || method.IsDefined(typeof(HarmonyFinalizer), false));
                var registered = harmony.GetPatchedMethods().Select(Harmony.GetPatchInfo)
                    .SelectMany(info => info.Prefixes.Concat(info.Postfixes).Concat(info.Finalizers))
                    .Where(patch => patch.owner == HarmonyOwner).Select(patch => patch.PatchMethod).ToHashSet();

                foreach (var method in patchMethods)
                {
                    if (!registered.Contains(method))
                    {
                        throw new InvalidOperationException($"Shared placement patch was not registered: {method.DeclaringType.Name}.{method.Name}.");
                    }
                }

                Registry.Initialize();
                _harmony = harmony;
            }
            catch
            {
                harmony.UnpatchSelf();
                Registry.Clear();

                throw;
            }
        }

        var client = new PlacementStorage(toolPrefab, radius, warning, error);
        _clients.Add(toolPrefab, client);

        return client;
    }

    public void Dispose()
    {
        if (!_clients.TryGetValue(_toolPrefab, out var client) || client != this)
        {
            return;
        }

        if (PlacementStorageContext.Current?.Client == this)
        {
            PlacementStorageContext.Reset();
        }

        _clients.Remove(_toolPrefab);

        if (_clients.Count == 0)
        {
            PlacementStorageContext.Reset();
            Registry.Clear();
            _harmony.UnpatchSelf();
            _harmony = null;
        }
    }

    internal static bool TryGetClient(Player player, out PlacementStorage client)
    {
        client = null;

        if (player == null || player != Player.m_localPlayer)
        {
            return false;
        }

        var tool = player.GetRightItem();
        var prefab = tool?.m_dropPrefab;

        return prefab != null && _clients.TryGetValue(prefab.name, out client);
    }

    internal void LogWarning(string message) => _warning(message);

    internal void LogError(string message) => _error(message);
}