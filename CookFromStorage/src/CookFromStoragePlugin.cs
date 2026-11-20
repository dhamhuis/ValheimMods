using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ValheimMods.Common;
using ValheimMods.Common.Crafting;
using ValheimMods.Common.InputSelection;

namespace CookFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class CookFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "CookFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.cookfromstorage";
    public const string PluginVersion = "1.0.1";

    private const string StorageRadiusDescription =
        "How far, in meters, CookFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100.\n"
        + "Applies to food recipes, cooking stations, the Stone Oven and the Fermenter. Only storage you could normally open is used.";

    private const string AutomaticIngredientsDescription =
        "Use accessible storage within 'Storage radius' for food and brewing ingredients you do not have in your inventory.\n"
        + "Covers:\n"
        + "- Recipes at the Cauldron, Food Preparation Table and Mead Ketill (storage counts toward requirements and is consumed when you craft).\n"
        + "- Raw food for the Cooking Station, Iron Cooking Station and Stone Oven: one item per E press.\n"
        + "- Mead bases for an empty Fermenter: one per E press.\n"
        + "With input selection enabled, cooking/Fermenter type priorities apply across inventory and storage; inventory wins within the chosen type. Otherwise inventory is always used first. Frost Foundry casts are never loaded from storage. Fuel is handled by SupplyFromStorage, not this setting.";

    private static ConfigEntry<bool> _automaticIngredients;
    private static ConfigEntry<float> _storageRadius;
    private static ConfigEntry<bool> _inputSelectionEnabled;
    private static ConfigEntry<KeyCode> _pickerKey;
    private Harmony _harmony;
    private RecipeStorage _recipeStorage;
    private InputPickerRegistration _inputPicker;
    private bool _initialized;
    private bool _pickerInitializationFailed;
    private ConfigEntry<string> _inputPreferences;
    private long _preferencePlayer;
    private string _preferenceStationType;
    private string _loadedPreferences;
    private Player _selectionPlayer;
    private readonly Dictionary<ConfigEntry<string>, string> _failedPreferenceSaves = new();

    internal static ManualLogSource Log { get; private set; }

    internal static bool AutomaticIngredientsEnabled => _automaticIngredients?.Value == true;

    internal static float StorageRadius => _storageRadius?.Value ?? StorageRange.DefaultRadius;

    internal static bool InputSelectionEnabled => _inputSelectionEnabled?.Value == true && AutomaticIngredientsEnabled;

    internal static string InputPickerKeyName => _pickerKey.Value.ToString();

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
        _inputSelectionEnabled = Config.Bind(
            "Input selection",
            "Enabled",
            true,
            "Enable selected-type insertion, per-character/per-station-type priorities and exclusions for cooking stations and Fermenters with multiple inputs. Requires Automatic ingredients. Inventory is preferred within the chosen type; explicit item use and collection remain vanilla. Disable for legacy inventory-first behavior.");
        _pickerKey = Config.Bind(
            "Input selection",
            "Open key",
            KeyCode.F8,
            "Open the shared input picker while aiming at a cooking station's ingredient input or a Fermenter. Not fuel, food recipes or Frost Foundry casts. Selection moves no items; use E afterward. Choose a binding not used by other mods.");
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
        catch (Exception exception)
        {
            _harmony.UnpatchSelf();
            NearbyStorage.Clear();
            Log.LogError($"{PluginName}: shared food recipe-storage initialization failed. {exception}");

            throw;
        }

        _initialized = true;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded; food recipes, cooking-station inputs and Fermenter bases use storage ({NearbyStorage.Radius:0.#}m); input selection enabled={InputSelectionEnabled}.");
    }

    private void Update()
    {
        if (!_initialized || _pickerInitializationFailed)
        {
            return;
        }

        if (!InputSelectionEnabled)
        {
            _inputPicker?.Dispose();
            _inputPicker = null;
            ClearSelection();

            return;
        }

        if (_inputPicker == null)
        {
            try
            {
                _inputPicker = InputPickerPreview.Register(
                    () => InputSelectionEnabled,
                    () => _pickerKey.Value,
                    CookingInputPreview.Read,
                    new InputPickerControls(
                        () => InputSelectionEnabled,
                        () => CookingInputSelection.Preferences,
                        SavePreferences,
                        () => CookingInputSelection.Session.Selected,
                        prefab => CookingInputSelection.Session.Select(prefab),
                        ObserveInput,
                        () => CookingInputSelection.Session.Context == null ? null
                            : $"{_pickerKey.Value}: choose / preferences",
                        () => CookingInputSelection.Session.HeldType,
                        () => CookingInputSelection.Session.IsStopped));
                Log.LogInfo($"{PluginName}: shared cooking/Fermenter input picker registered; open key {_pickerKey.Value}.");
            }
            catch (Exception exception)
            {
                _pickerInitializationFailed = true;
                Log.LogError($"{PluginName}: input picker initialization failed until restart. Controlled input is blocked while selection is enabled; food recipes are unchanged. {exception}");

                return;
            }
        }

        var player = Player.m_localPlayer;

        if (player == null || player.IsDead() || player.IsTeleporting() || player.InCutscene())
        {
            ClearSelection();
        }

        _inputPicker.Update();
    }

    private void OnGUI()
    {
        _inputPicker?.Draw();
    }

    private void OnDisable()
    {
        _inputPicker?.Dispose();
        _inputPicker = null;
        ClearSelection();
    }

    private void OnDestroy()
    {
        _inputPicker?.Dispose();
        ClearSelection();
        _recipeStorage?.Dispose();
        StopAllCoroutines();
        NearbyStorage.Clear();
        _harmony?.UnpatchSelf();

        if (ReferenceEquals(Log, Logger))
        {
            Log = null;
        }
    }

    private void ObserveInput(Player player, bool allowed)
    {
        if (!InputSelectionEnabled)
        {
            ClearSelection();

            return;
        }

        var station = CookingInputPreview.GetStation(player);
        LoadPreferences(player, station);
        bool held = ZInput.GetButton("Use") || ZInput.GetButton("JoyUse");
        bool pressed = ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse");
        CookingInputSelection.Session.Observe(station, held, pressed, !allowed);
    }

    private void ClearSelection()
    {
        CookingInputSelection.Session.Clear();
        CookingInputSelection.Preferences = null;
        CookingInputSelection.PreferenceStationType = null;
        _selectionPlayer = null;
        _preferencePlayer = 0;
        _preferenceStationType = null;
        _inputPreferences = null;
        _loadedPreferences = null;
    }

    private void LoadPreferences(Player player, Component station)
    {
        if (_selectionPlayer != player)
        {
            ClearSelection();
            _selectionPlayer = player;
        }

        long id = player.GetPlayerID();
        string stationType = CookingInputSelection.StationType(station);

        if (id == 0 || station == null)
        {
            CookingInputSelection.Preferences = null;
            CookingInputSelection.PreferenceStationType = null;
            _preferenceStationType = null;
            _inputPreferences = null;

            return;
        }

        if (id != _preferencePlayer || stationType != _preferenceStationType)
        {
            bool playerChanged = id != _preferencePlayer;
            _preferencePlayer = id;
            _preferenceStationType = stationType;
            CookingInputSelection.PreferenceStationType = stationType;
            CookingInputSelection.Preferences = null;
            _inputPreferences = null;
            _loadedPreferences = null;

            if (playerChanged)
            {
                CookingInputSelection.Session.Clear();
            }

            try
            {
                _inputPreferences = Config.Bind(
                    "Input preferences " + stationType,
                    "Player " + id.ToString(CultureInfo.InvariantCulture),
                    CookingInputSelection.DefaultPreferences(station),
                    "Per-character, per-station-type priority|exclusions using exact comma-separated prefab IDs. New entries rank current accepted inputs in conversion order, with no exclusions. Unknown IDs survive editing. Exclusions affect automatic E from inventory and storage, not explicit item use or picker selection.");
            }
            catch (IOException exception)
            {
                PreferenceSaveFailed(exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                PreferenceSaveFailed(exception);
            }
        }

        if (_inputPreferences == null)
        {
            return;
        }

        if (_failedPreferenceSaves.TryGetValue(_inputPreferences, out string failedValue))
        {
            if (_inputPreferences.Value == failedValue)
            {
                CookingInputSelection.Preferences = null;

                return;
            }

            _failedPreferenceSaves.Remove(_inputPreferences);
        }

        if (_inputPreferences.Value == _loadedPreferences)
        {
            return;
        }

        _loadedPreferences = _inputPreferences.Value;

        try
        {
            CookingInputSelection.Preferences = InputPreferences.Parse(_loadedPreferences);
        }
        catch (FormatException exception)
        {
            CookingInputSelection.Preferences = null;
            Log.LogError($"Invalid cooking input preferences for {stationType}; controlled insertion is blocked. {exception.Message}");
        }
    }

    private void SavePreferences(InputPreferences preferences)
    {
        if (_inputPreferences == null)
        {
            CookingInputSelection.Preferences = null;
            Log.LogError("Cannot save cooking preferences without an initialized player configuration.");
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                "Cannot save cooking preferences. Insertion blocked; check the log.", 0, null, false);

            return;
        }

        try
        {
            _inputPreferences.Value = preferences.Serialize();
            Config.Save();
            _loadedPreferences = _inputPreferences.Value;
            CookingInputSelection.Preferences = preferences;
        }
        catch (IOException exception)
        {
            PreferenceSaveFailed(exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            PreferenceSaveFailed(exception);
        }
    }

    private void PreferenceSaveFailed(Exception exception)
    {
        _loadedPreferences = _inputPreferences?.Value;
        CookingInputSelection.Preferences = null;

        if (_inputPreferences != null)
        {
            _failedPreferenceSaves[_inputPreferences] = _inputPreferences.Value;
        }

        Log.LogError($"Could not persist cooking preferences; insertion is blocked until restart/config correction. {exception}");
        Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
            "Could not save cooking preferences. Insertion blocked; check the log.", 0, null, false);
    }

    private static bool IsRegistered(Type targetType, string methodName, Type patchType)
    {
        var target = AccessTools.Method(targetType, methodName);
        var patchInfo = target == null ? null : Harmony.GetPatchInfo(target);

        return patchInfo?.Prefixes.Concat(patchInfo.Postfixes)
            .Any(patch => patch.owner == PluginGuid && patch.PatchMethod.DeclaringType == patchType) == true;
    }
}