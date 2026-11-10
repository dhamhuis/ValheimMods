using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ValheimMods.Common.InputSelection;

public sealed class InputPickerPreview : IDisposable
{
    private const string HarmonyOwner = "hoose.valheim.inputpickerpreview";

    private static InputPickerPreview _instance;
    private readonly Harmony _harmony;
    private readonly Func<bool> _enabled;
    private readonly Func<KeyCode> _key;
    private readonly Func<Player, InputPreviewSnapshot> _snapshot;
    private readonly InputPickerControls _controls;
    private readonly InputPreviewSession _session = new();
    private InputPreviewSnapshot _current;
    private InputPreviewSnapshot _hintSnapshot;
    private Player _player;
    private string _selected;
    private float _nextRefresh;
    private float _nextHintRefresh;
    private int _page;
    private int _lastOpenAttempt = -1;
    private int _lastObserveFrame = -1;
    private bool _hintAllowed;
    private GUIStyle _titleStyle;
    private GUIStyle _itemStyle;

    private InputPickerPreview(
        Harmony harmony,
        Func<bool> enabled,
        Func<KeyCode> key,
        Func<Player, InputPreviewSnapshot> snapshot,
        InputPickerControls controls)
    {
        _harmony = harmony;
        _enabled = enabled;
        _key = key;
        _snapshot = snapshot;
        _controls = controls;
    }

    internal static bool BlocksInput => _instance?._session.BlocksInput == true;

    internal static bool IsOpen => _instance?._session.IsOpen == true;

    public static InputPickerPreview Register(
        Func<bool> enabled,
        Func<KeyCode> key,
        Func<Player, InputPreviewSnapshot> snapshot,
        InputPickerControls controls = null)
    {
        if (enabled == null || key == null || snapshot == null)
        {
            throw new ArgumentNullException(nameof(enabled), "Picker-preview callbacks must not be null.");
        }

        if (_instance != null)
        {
            throw new InvalidOperationException("The input picker preview is already registered.");
        }

        var harmony = new Harmony(HarmonyOwner);

        try
        {
            harmony.CreateClassProcessor(typeof(PreviewPlayerInputPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PreviewControllerInputPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PreviewCursorPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PreviewMenuPatch)).Patch();
            var registered = harmony.GetPatchedMethods();

            if (registered.Count() != 4)
            {
                throw new InvalidOperationException("Not all four input picker preview patches were registered.");
            }
        }
        catch
        {
            harmony.UnpatchSelf();

            throw;
        }

        _instance = new InputPickerPreview(harmony, enabled, key, snapshot, controls);

        return _instance;
    }

    public void Update()
    {
        if (_instance != this)
        {
            return;
        }

        if (!_enabled() || Player.m_localPlayer == null || (_player != null && _player != Player.m_localPlayer)
            || (_player != null && (_player.IsDead() || _player.IsTeleporting() || _player.InCutscene())))
        {
            Close();
        }

        if (_session.CanProcessInput(Time.frameCount))
        {
            if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(_key())
                || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyMenu"))
            {
                Close();
            }
            else
            {
                Refresh();

                if (_session.IsOpen)
                {
                    Navigate();

                    if (_controls?.Enabled() == true
                        && (ZInput.GetKeyDown(KeyCode.Return) || ZInput.GetButtonDown("JoyUse")))
                    {
                        ApplySelection();
                    }
                }
            }
        }

        _session.UpdateRelease(Time.frameCount, ControlsHeld());

        if (!_session.BlocksInput && _hintAllowed && _controls?.Enabled() == true
            && Time.unscaledTime >= _nextHintRefresh)
        {
            _nextHintRefresh = Time.unscaledTime + 0.25f;
            _hintSnapshot = _snapshot(Player.m_localPlayer);
        }
    }

    public void Draw()
    {
        if (!_session.IsOpen || _current == null)
        {
            if (!_session.BlocksInput && _hintAllowed && _hintSnapshot != null && _controls?.Enabled() == true)
            {
                string hint = _controls.Hint();

                if (!string.IsNullOrEmpty(hint))
                {
                    string selected = _controls.Selected();
                    string held = _controls.HeldType();
                    string chosen = held ?? selected ?? _controls.Preferences()?.Choose(
                        _hintSnapshot.Items.Where(item => item.IsAvailable).Select(item => item.Item.name));
                    var entry = _hintSnapshot.Items.FirstOrDefault(item => item.Item.name == chosen);
                    string name = entry == null ? chosen ?? "none" : Localize(entry.Item.m_itemData.m_shared.m_name);
                    string mode = _controls.Stopped() ? "Stopped (release use)"
                        : held != null ? "Holding" : selected == null ? "Auto" : "Selected";
                    string text = $"{mode}: {name} "
                        + $"(inventory {entry?.InventoryCount ?? 0}, storage {entry?.StorageCount ?? 0}) | {hint}";
                    GUI.Box(new Rect((Screen.width - 700) / 2f, Screen.height - 155, 700, 28), text);
                }
            }

            return;
        }

        bool controlled = _controls?.Enabled() == true;
        var preferences = controlled ? _controls.Preferences() : null;
        var order = preferences?.Order(_current.Items.Select(entry => entry.Item.name));
        float height = controlled ? 550 : 390;
        _titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter
        };
        _itemStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        float scale = Mathf.Min(1.5f, Mathf.Min(Screen.width / 760f, Screen.height / (height + 70)));
        var previousMatrix = GUI.matrix;
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        bool previousEnabled = GUI.enabled;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        GUI.depth = -100;

        try
        {
            float left = ((Screen.width / scale) - 700) / 2;
            float top = ((Screen.height / scale) - height) / 2;
            GUI.Box(new Rect(left, top, 700, height), string.Empty);
            GUI.Label(
                new Rect(left + 15, top + 12, 670, 35),
                $"{Localize(_current.Name)} - {(controlled ? "input selection" : "input picker preview")}",
                _titleStyle);
            GUI.Label(
                new Rect(left + 20, top + 52, 660, 45),
                controlled ? $"Current: {_controls.Selected() ?? "Auto"}. Choose a type, then press E at the kiln. Choosing consumes nothing."
                    : "Preview only: clicking an item does not add it or change what E uses.",
                _itemStyle);

            for (int index = 0; index < Math.Min(3, _current.Items.Count - (_page * 3)); index++)
            {
                var entry = _current.Items[(_page * 3) + index];
                var rect = new Rect(left + 20 + (index * 225), top + 110, 210, 180);
                bool selected = _selected == entry.Item.name;
                GUI.color = selected ? new Color(1f, 0.85f, 0.4f) : previousColor;
                GUI.enabled = previousEnabled && (entry.IsAvailable || controlled);

                if (GUI.Button(rect, string.Empty))
                {
                    _selected = entry.Item.name;
                }

                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
                var sprite = entry.Item.m_itemData.GetIcon();

                if (sprite != null)
                {
                    Rect source = sprite.textureRect;
                    var texture = sprite.texture;
                    var coordinates = new Rect(
                        source.x / texture.width,
                        source.y / texture.height,
                        source.width / texture.width,
                        source.height / texture.height);
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x + 73, rect.y + 10, 64, 64), texture, coordinates);
                }

                GUI.Label(
                    new Rect(rect.x + 5, rect.y + 75, 200, 40),
                    Localize(entry.Item.m_itemData.m_shared.m_name),
                    _itemStyle);
                GUI.Label(
                    new Rect(rect.x + 5, rect.y + 120, 200, 50),
                    $"Inventory: {entry.InventoryCount}\nStorage: {entry.StorageCount}",
                    _itemStyle);

                if (controlled && preferences != null)
                {
                    bool excluded = preferences.IsExcluded(entry.Item.name);
                    int rank = order.ToList().IndexOf(entry.Item.name) + 1;

                    if (GUI.Button(new Rect(rect.x, top + 300, 210, 28),
                        excluded ? "Never auto-add" : preferences.IsConfigured(entry.Item.name)
                            ? $"Auto priority: {rank} (allowed)" : $"New type: allowed (priority {rank})"))
                    {
                        _controls.Save(preferences.ToggleExcluded(entry.Item.name));
                    }

                    if (GUI.Button(new Rect(rect.x, top + 335, 210, 28), "Move to first priority"))
                    {
                        _controls.Save(preferences.MoveFirst(entry.Item.name, _current.Items.Select(item => item.Item.name)));
                    }
                }
            }

            GUI.Label(
                new Rect(left + 20, top + (controlled ? 372 : 295), 660, controlled ? 60 : 40),
                controlled ? preferences == null
                        ? "Preferences invalid/unavailable: insertion is blocked. Correct the config and restart; see the log."
                        : "Click the auto-status button to allow/block a type. Explicit selection can override exclusions.\nPriority chooses the TYPE first; inventory then storage. Release E before changing type."
                    : "Click or use Left/Right / D-pad to highlight. Only locally owned storage is counted.",
                _itemStyle);

            if (controlled)
            {
                if (_current.Items.Count > 3 && GUI.Button(new Rect(left + 250, top + 440, 200, 28),
                    $"Next page ({_page + 1}/{(_current.Items.Count + 2) / 3})"))
                {
                    _page = (_page + 1) % ((_current.Items.Count + 2) / 3);
                }

                GUI.enabled = previousEnabled && preferences != null;

                if (GUI.Button(new Rect(left + 20, top + 485, 180, 32), "Auto"))
                {
                    _controls.Select(null);
                    Close();
                }

                GUI.enabled = previousEnabled && preferences != null
                    && _current != null && _current.Items.Any(entry => entry.Item.name == _selected && entry.IsAvailable);

                if (GUI.Button(new Rect(left + 215, top + 485, 240, 32), "Use highlighted (Enter)"))
                {
                    ApplySelection();
                }

                GUI.enabled = previousEnabled;
            }

            if (GUI.Button(new Rect(left + (controlled ? 475 : 250), top + (controlled ? 485 : 340), 200, 32),
                controlled ? "Cancel (Esc / B)" : "Close (Esc / B)"))
            {
                Close();
            }
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.depth = previousDepth;
            GUI.color = previousColor;
            GUI.enabled = previousEnabled;
        }
    }

    public void Dispose()
    {
        if (_instance != this)
        {
            return;
        }

        Close();
        _instance = null;
        _harmony.UnpatchSelf();
        RestoreCursor();
    }

    internal static void FilterPlayerInput(Player player, ref bool allowed)
    {
        if (_instance == null || player != Player.m_localPlayer)
        {
            return;
        }

        var preview = _instance;

        if (!allowed && preview._session.IsOpen)
        {
            preview.Close();
        }

        if (allowed && !preview._session.BlocksInput && preview._enabled()
            && preview._key() != KeyCode.None && ZInput.GetKeyDown(preview._key())
            && preview._lastOpenAttempt != Time.frameCount
            && !UnifiedPopup.IsVisible() && !Hud.IsPieceSelectionVisible() && !Hud.InRadial())
        {
            preview._lastOpenAttempt = Time.frameCount;
            var snapshot = preview._snapshot(player);

            if (snapshot != null)
            {
                preview._player = player;
                preview._current = snapshot;
                preview._selected = preview._controls?.Enabled() == true ? preview._controls.Selected() : null;
                preview._page = 0;
                preview._nextRefresh = Time.unscaledTime + 0.25f;
                preview._session.Open(Time.frameCount);
                ZCursor.LockState = CursorLockMode.None;
                ZCursor.Show();
            }
        }

        if (preview._session.BlocksInput)
        {
            allowed = false;
        }

        preview._hintAllowed = allowed;

        if (preview._lastObserveFrame != Time.frameCount)
        {
            preview._lastObserveFrame = Time.frameCount;
            preview._controls?.Observe(player, allowed);
        }
    }

    private static string Localize(string text)
    {
        return Localization.instance.Localize(text);
    }

    private static bool ControlsHeld()
    {
        return ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")
            || ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack")
            || ZInput.GetButton("SecondaryAttack") || ZInput.GetButton("JoySecondaryAttack")
            || ZInput.GetButton("JoyButtonB") || ZInput.GetButton("JoyMenu")
            || ZInput.GetKey(KeyCode.Escape);
    }

    private static void RestoreCursor()
    {
        if (GameCamera.instance != null)
        {
            GameCamera.instance.UpdateMouseCapture();
        }
    }

    private void Refresh()
    {
        if (Time.unscaledTime < _nextRefresh)
        {
            return;
        }

        _nextRefresh = Time.unscaledTime + 0.25f;
        var snapshot = _snapshot(_player);

        if (snapshot == null || snapshot.Station != _current.Station)
        {
            Close();

            return;
        }

        _current = snapshot;
        _page = Math.Min(_page, (_current.Items.Count - 1) / 3);

        if (!_current.Items.Any(entry => (entry.IsAvailable || _controls?.Enabled() == true) && entry.Item.name == _selected))
        {
            _selected = null;
        }
    }

    private void Navigate()
    {
        int direction = ZInput.GetKeyDown(KeyCode.LeftArrow) || ZInput.GetButtonDown("JoyDPadLeft") ? -1
            : ZInput.GetKeyDown(KeyCode.RightArrow) || ZInput.GetButtonDown("JoyDPadRight") ? 1 : 0;

        if (direction == 0)
        {
            return;
        }

        int index = -1;

        for (int candidate = 0; candidate < _current.Items.Count; candidate++)
        {
            if (_current.Items[candidate].Item.name == _selected)
            {
                index = candidate;

                break;
            }
        }

        if (index < 0)
        {
            index = direction > 0 ? _current.Items.Count - 1 : 0;
        }

        for (int step = 0; step < _current.Items.Count; step++)
        {
            index = (index + direction + _current.Items.Count) % _current.Items.Count;

            if (_current.Items[index].IsAvailable || _controls?.Enabled() == true)
            {
                _selected = _current.Items[index].Item.name;
                _page = index / 3;

                return;
            }
        }
    }

    private void ApplySelection()
    {
        if (_controls?.Enabled() != true || _controls.Preferences() == null
            || _current == null || !_current.Items.Any(entry => entry.Item.name == _selected && entry.IsAvailable))
        {
            return;
        }

        _controls.Select(_selected);
        Close();
    }

    private void Close()
    {
        if (!_session.IsOpen)
        {
            return;
        }

        _session.Close(Time.frameCount);
        _current = null;
        _selected = null;
        _player = null;
        RestoreCursor();
    }
}