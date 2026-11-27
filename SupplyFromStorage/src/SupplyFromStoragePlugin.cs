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
using ValheimMods.Common.InputSelection;

namespace SupplyFromStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class SupplyFromStoragePlugin : BaseUnityPlugin
{
    public const string PluginName = "SupplyFromStorage";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.supplyfromstorage";
    public const string PluginVersion = "1.0.3";

    private const string StorageRadiusDescription =
        "How far, in meters, SupplyFromStorage searches for storage around you (a sphere centred on your character). Allowed range: 1 to 100.\n"
        + "Applies to both 'Automatic fuel' and 'Automatic processing inputs'. Only storage you could normally open is used.";

    private const string AutomaticFuelDescription =
        "When you press E on a machine's fuel slot and have no accepted fuel in your inventory, add one accepted fuel item from accessible storage within 'Storage radius'.\n"
        + "Covers:\n"
        + "- Campfires, bonfires, hearths, fireplaces, braziers, torches and other fires.\n"
        + "- Cooking-station and Stone Oven fuel, and the Frost Foundry's liquid frost. Never cooking ingredients or Foundry casts.\n"
        + "- Smelter and Blast Furnace fuel (coal), and Eitr Refinery fuel (sap).\n"
        + "- Hot Tub (wood) and Frost Kiln (ice).\n"
        + "Your inventory is always used first. Disabling this does not affect 'Automatic processing inputs'.";

    private const string AutomaticProcessingInputsDescription =
        "When you press E on a machine's input slot and have no accepted input in your inventory, add one accepted item from accessible storage within 'Storage radius'.\n"
        + "Covers:\n"
        + "- Ore and scrap for the Smelter and Blast Furnace.\n"
        + "- Wood, Fine Wood or Core Wood for the Charcoal Kiln.\n"
        + "- Barley for the Windmill and flax for the Spinning Wheel.\n"
        + "- Soft Tissue for the Eitr Refinery (sap uses its separate fuel slot).\n"
        + "- Wood, Fine Wood, Core Wood or Ashwood for the Battering Ram engine.\n"
        + "Each machine's own accepted-item list decides what can be added. Your inventory is used first unless optional input selection is enabled for the Charcoal Kiln, Smelter, Blast Furnace or Battering Ram engine. Disabling this also disables input selection and restores vanilla inventory handling; it does not affect 'Automatic fuel'.";

    private static ConfigEntry<bool> _automaticFuel;
    private static ConfigEntry<bool> _automaticProcessingInputs;
    private static ConfigEntry<float> _storageRadius;
    private static ConfigEntry<bool> _inputSelectionEnabled;
    private static ConfigEntry<KeyCode> _previewKey;
    private Harmony _harmony;
    private InputPickerRegistration _inputPreview;
    private ConfigEntry<bool> _previewEnabled;
    private bool _initialized;
    private bool _previewInitializationFailed;
    private ConfigEntry<string> _processorPreferences;
    private long _preferencePlayer;
    private string _loadedPreferences;
    private string _preferenceStationType;
    private Player _selectionPlayer;
    private readonly Dictionary<ConfigEntry<string>, string> _failedPreferenceSaves = new();

    internal static ManualLogSource Log { get; private set; }

    internal static bool FuelAutomationEnabled => _automaticFuel?.Value == true;

    internal static bool ProcessingAutomationEnabled => _automaticProcessingInputs?.Value == true;

    internal static float StorageRadius => _storageRadius?.Value ?? StorageRange.DefaultRadius;

    internal static bool InputSelectionEnabled => _inputSelectionEnabled?.Value == true && ProcessingAutomationEnabled;

    internal static string InputPickerKeyName => _previewKey.Value.ToString();

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
        _previewEnabled = Config.Bind(
            "Input selection preview",
            "Enabled",
            false,
            "Experimental, non-consuming Charcoal Kiln input grid. Alone, it does not change E behavior. The separate Input selection setting enables real selection and preferences.");
        _previewKey = Config.Bind(
            "Input selection preview",
            "Open key",
            KeyCode.F8,
            "Keyboard key for the kiln preview or enabled Charcoal Kiln/Smelter/Blast Furnace/Battering Ram input picker. Aim at the material input or ram engine-loading input, not a separate fuel or operating interaction. Escape closes it. Choose a key not used by other mods.");
        _inputSelectionEnabled = Config.Bind(
            "Input selection",
            "Enabled",
            true,
            "Enable selected-type insertion, separate per-character/per-station-type priorities/exclusions and held-E type locking for the Charcoal Kiln, Smelter, Blast Furnace and Battering Ram engine while Automatic processing inputs is enabled. Type priority applies across inventory and storage; inventory wins within the chosen type. Separate fuel callbacks and other stations stay unchanged. Uses the preview Open key. Existing enabled selection also enables the ram engine.");

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

        _initialized = true;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded; E interactions add one accepted fuel or processing input from nearby accessible storage ({NearbyStorage.Radius:0.#}m); input selection enabled={InputSelectionEnabled}.");
    }

    private void Update()
    {
        if (!_initialized || _previewInitializationFailed)
        {
            return;
        }

        if (!InputSelectionEnabled && !_previewEnabled.Value)
        {
            _inputPreview?.Dispose();
            _inputPreview = null;
            ProcessorInputSelection.Session.Clear();

            return;
        }

        if (_inputPreview == null)
        {
            try
            {
                _inputPreview = InputPickerPreview.Register(
                    () => _previewEnabled.Value || InputSelectionEnabled,
                    () => _previewKey.Value,
                    ProcessorInputPreview.Read,
                    new InputPickerControls(
                        () => InputSelectionEnabled,
                        () => ProcessorInputSelection.Preferences,
                        SavePreferences,
                        () => ProcessorInputSelection.Session.Selected,
                        prefab => ProcessorInputSelection.Session.Select(prefab),
                        ObserveInput,
                        () => ProcessorInputSelection.Session.Context == null ? null
                            : $"{_previewKey.Value}: choose / preferences",
                        () => ProcessorInputSelection.Session.HeldType,
                        () => ProcessorInputSelection.Session.IsStopped));
                Log.LogInfo($"{PluginName}: processor input picker registered (Charcoal Kiln, Smelter, Blast Furnace, Battering Ram); selection enabled={InputSelectionEnabled}, open key {_previewKey.Value}.");
            }
            catch (Exception exception)
            {
                _previewInitializationFailed = true;
                Log.LogError($"{PluginName}: input picker initialization failed until restart. Controlled processor insertion is blocked when selection is enabled; other automation is unchanged. {exception}");

                return;
            }
        }

        if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead()
            || Player.m_localPlayer.IsTeleporting() || Player.m_localPlayer.InCutscene())
        {
            ProcessorInputSelection.Session.Clear();
            _preferencePlayer = 0;
            _preferenceStationType = null;
            _processorPreferences = null;
            _loadedPreferences = null;
            _selectionPlayer = null;
            ProcessorInputSelection.Preferences = null;
        }

        _inputPreview?.Update();
    }

    private void OnGUI()
    {
        _inputPreview?.Draw();
    }

    private void OnDisable()
    {
        _inputPreview?.Dispose();
        _inputPreview = null;
        ProcessorInputSelection.Session.Clear();
    }

    private void OnDestroy()
    {
        _inputPreview?.Dispose();
        ProcessorInputSelection.Session.Clear();
        SupplyTransfer.Clear();
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
            ProcessorInputSelection.Session.Clear();

            return;
        }

        bool held = ZInput.GetButton("Use") || ZInput.GetButton("JoyUse");
        bool pressed = ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse");
        var station = ProcessorInputPreview.GetStation(player);
        LoadPreferences(player, station?.m_name);
        ProcessorInputSelection.Session.Observe(station, held, pressed, !allowed);
    }

    private void LoadPreferences(Player player, string stationType)
    {
        if (_selectionPlayer != player)
        {
            _selectionPlayer = player;
            _preferencePlayer = 0;
            _preferenceStationType = null;
            _loadedPreferences = null;
            _processorPreferences = null;
            ProcessorInputSelection.Session.Clear();
            ProcessorInputSelection.Preferences = null;
        }

        long id = player.GetPlayerID();

        if (id == 0 || !ProcessorInputSelection.Supports(stationType))
        {
            ProcessorInputSelection.Preferences = null;
            _preferenceStationType = null;
            ProcessorInputSelection.PreferenceStationType = null;

            return;
        }

        if (id != _preferencePlayer || stationType != _preferenceStationType)
        {
            bool playerChanged = id != _preferencePlayer;
            _preferencePlayer = id;
            _preferenceStationType = stationType;
            ProcessorInputSelection.PreferenceStationType = stationType;
            _loadedPreferences = null;
            _processorPreferences = null;
            ProcessorInputSelection.Preferences = null;

            if (playerChanged)
            {
                ProcessorInputSelection.Session.Clear();
            }

            try
            {
                _processorPreferences = Config.Bind(
                    ProcessorInputSelection.PreferenceSection(stationType),
                    "Player " + id.ToString(CultureInfo.InvariantCulture),
                    ProcessorInputSelection.DefaultPreferences(stationType),
                    "Per-character, per-station-type priority|exclusions. Comma-separated prefab IDs; RoundLog is Core Wood. Empty priority uses ordinal prefab order. Exclusions affect automatic E from inventory AND storage, not explicit selection. Edit in the picker. Unknown IDs are preserved.");
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

        if (_processorPreferences == null)
        {
            return;
        }

        if (_failedPreferenceSaves.TryGetValue(_processorPreferences, out string failedValue))
        {
            if (_processorPreferences.Value == failedValue)
            {
                ProcessorInputSelection.Preferences = null;

                return;
            }

            _failedPreferenceSaves.Remove(_processorPreferences);
        }

        if (_processorPreferences.Value == _loadedPreferences)
        {
            return;
        }

        _loadedPreferences = _processorPreferences.Value;

        try
        {
            ProcessorInputSelection.Preferences = InputPreferences.Parse(_loadedPreferences);
        }
        catch (FormatException exception)
        {
            ProcessorInputSelection.Preferences = null;
            Log.LogError($"Invalid {ProcessorInputSelection.PreferenceSection(stationType)} for the current character; automatic station insertion is blocked. {exception.Message}");
        }
    }

    private void SavePreferences(InputPreferences preferences)
    {
        if (_processorPreferences == null)
        {
            Log.LogError("Cannot save station preferences without an initialized player configuration.");

            return;
        }

        try
        {
            _processorPreferences.Value = preferences.Serialize();
            Config.Save();
            _loadedPreferences = _processorPreferences.Value;
            ProcessorInputSelection.Preferences = preferences;
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
        _loadedPreferences = _processorPreferences?.Value;
        ProcessorInputSelection.Preferences = null;

        if (_processorPreferences != null)
        {
            _failedPreferenceSaves[_processorPreferences] = _processorPreferences.Value;
        }

        Log.LogError($"Could not persist station preferences; automatic insertion is blocked until restart/config correction. {exception}");
        Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
            "Could not save station preferences. Insertion blocked; check the log.", 0, null, false);
    }

    private static bool IsRegistered(Type targetType, string methodName, Type patchType)
    {
        var target = AccessTools.Method(targetType, methodName);
        var patchInfo = target == null ? null : Harmony.GetPatchInfo(target);

        return patchInfo?.Prefixes.Concat(patchInfo.Postfixes)
            .Any(patch => patch.owner == PluginGuid && patch.PatchMethod.DeclaringType == patchType) == true;
    }
}