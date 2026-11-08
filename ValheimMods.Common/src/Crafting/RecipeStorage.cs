using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ValheimMods.Common.Crafting;

public sealed class RecipeStorage : IDisposable
{
    private const string HarmonyOwner = "hoose.valheim.sharedrecipestorage";

    private static readonly Dictionary<bool, RecipeStorage> _clients = new();
    private static Harmony _harmony;
    private static RecipeStorage _active;
    private readonly Action<IEnumerator> _startCoroutine;
    private readonly Action _stopCoroutines;
    private readonly Action<string> _warning;
    private readonly Action<string> _error;
    private readonly Func<bool> _enabled;
    private readonly Func<float> _radius;
    private readonly bool _food;

    private RecipeStorage(
        bool food,
        Action<IEnumerator> startCoroutine,
        Action stopCoroutines,
        Action<string> warning,
        Action<string> error,
        Func<bool> enabled,
        Func<float> radius)
    {
        _food = food;
        _startCoroutine = startCoroutine;
        _stopCoroutines = stopCoroutines;
        _warning = warning;
        _error = error;
        _enabled = enabled;
        _radius = radius;
    }

    private float Radius => StorageRange.Clamp(_radius?.Invoke() ?? StorageRange.DefaultRadius);

    public static RecipeStorage Register(
        bool food,
        Action<IEnumerator> startCoroutine,
        Action stopCoroutines,
        Action<string> warning,
        Action<string> error,
        Func<bool> enabled,
        Func<float> radius = null)
    {
        if (startCoroutine == null || stopCoroutines == null || warning == null || error == null || enabled == null)
        {
            throw new ArgumentNullException(nameof(startCoroutine), "Recipe-storage callbacks must not be null.");
        }

        if (_clients.ContainsKey(food))
        {
            throw new InvalidOperationException("A recipe-storage client is already registered for this station category.");
        }

        if (_clients.Count == 0)
        {
            var harmony = new Harmony(HarmonyOwner);

            try
            {
                var patchTypes = typeof(RecipeStorage).Assembly.GetTypes().Where(type =>
                    type.Namespace == typeof(RecipeStorage).Namespace
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
                        throw new InvalidOperationException($"Shared recipe patch was not registered: {method.DeclaringType.Name}.{method.Name}.");
                    }
                }

                NearbyStorage.Initialize();
                _harmony = harmony;
            }
            catch
            {
                harmony.UnpatchSelf();
                NearbyStorage.Clear();

                throw;
            }
        }

        var client = new RecipeStorage(food, startCoroutine, stopCoroutines, warning, error, enabled, radius);
        _clients.Add(food, client);

        return client;
    }

    public static bool IsFoodStation(CraftingStation station)
    {
        return station != null && station.m_name is "$piece_cauldron" or "$piece_preptable" or "$piece_meadcauldron";
    }

    public void Dispose()
    {
        if (!_clients.TryGetValue(_food, out var client) || client != this)
        {
            return;
        }

        if (_active == this)
        {
            StorageCrafting.Cancel();
            _stopCoroutines();
            _active = null;
        }

        _clients.Remove(_food);

        if (_clients.Count == 0)
        {
            StorageCrafting.Clear();
            NearbyStorage.Clear();
            _harmony.UnpatchSelf();
            _harmony = null;
        }
    }

    internal static bool Supports(Player player, Recipe recipe)
    {
        var current = player.GetCurrentCraftingStation();

        return _clients.TryGetValue(IsFoodStation(recipe.m_craftingStation), out var client)
            && client._enabled() && IsFoodStation(current) == client._food;
    }

    internal static void StartCraft(InventoryGui gui, Player player)
    {
        _active = _clients[IsFoodStation(gui.m_craftRecipe.m_craftingStation)];

        try
        {
            _active._startCoroutine(StorageCrafting.Craft(gui, player));
        }
        finally
        {
            if (!StorageCrafting.IsPending)
            {
                _active = null;
            }
        }
    }

    internal static void FinishCraft() => _active = null;

    internal static float RadiusFor(Player player)
    {
        if (_active != null)
        {
            return _active.Radius;
        }

        var current = player?.GetCurrentCraftingStation();

        if (current != null && _clients.TryGetValue(IsFoodStation(current), out var client))
        {
            return client.Radius;
        }

        // Without a station context (for example a guarded manual container open), allow the widest registered range.
        return _clients.Count == 0 ? StorageRange.DefaultRadius : _clients.Values.Max(registered => registered.Radius);
    }

    internal static void LogWarning(string message) => Log(message, false);

    internal static void LogError(string message) => Log(message, true);

    private static void Log(string message, bool error)
    {
        var client = _active ?? _clients.Values.First();
        var log = error ? client._error : client._warning;
        log(message);
    }
}