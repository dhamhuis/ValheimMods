using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ValheimMods.Common.InputSelection;

public sealed class InputPickerPreview
{
    private const string HarmonyOwner = "hoose.valheim.inputpickerpreview";
    private const int ItemsPerPage = 8;
    private const int GridColumns = 4;

    private static InputPickerPreview _instance;
    private readonly Harmony _harmony;
    private readonly InputPickerProviderRegistry _providers = new();
    private readonly InputPreviewSession _session = new();
    private InputPickerProvider _activeProvider;
    private InputPickerProvider _hintProvider;
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
    private GUIStyle _detailStyle;
    private GUIStyle _helpStyle;

    private InputPickerPreview(Harmony harmony)
    {
        _harmony = harmony;
    }

    internal static bool BlocksInput => _instance?._session.BlocksInput == true;

    internal static bool IsOpen => _instance?._session.IsOpen == true;

    public static InputPickerRegistration Register(
        Func<bool> enabled,
        Func<KeyCode> key,
        Func<Player, InputPreviewSnapshot> snapshot,
        InputPickerControls controls = null)
    {
        var provider = new InputPickerProvider(enabled, key, snapshot, controls);

        if (_instance == null)
        {
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

            _instance = new InputPickerPreview(harmony);
        }

        var registration = new InputPickerRegistration(_instance, provider);
        _instance._providers.Add(provider);

        return registration;
    }

    internal bool IsDriver(InputPickerRegistration registration)
    {
        return _providers.Driver == registration.Provider;
    }

    internal void Update(InputPickerRegistration registration)
    {
        if (!IsDriver(registration))
        {
            return;
        }

        if ((_activeProvider != null && !_activeProvider.Enabled())
            || Player.m_localPlayer == null || (_player != null && _player != Player.m_localPlayer)
            || (_player != null && (_player.IsDead() || _player.IsTeleporting() || _player.InCutscene())))
        {
            Close();
        }

        if (_session.CanProcessInput(Time.frameCount))
        {
            if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(_activeProvider.Key())
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
                    var controls = _activeProvider.Controls;

                    if (controls?.Enabled() == true
                        && (ZInput.GetKeyDown(KeyCode.Return) || ZInput.GetButtonDown("JoyUse")))
                    {
                        ApplySelection();
                    }
                }
            }
        }

        _session.UpdateRelease(Time.frameCount, ControlsHeld());

        if (!_session.BlocksInput && _hintAllowed && Time.unscaledTime >= _nextHintRefresh)
        {
            _nextHintRefresh = Time.unscaledTime + 0.25f;
            _hintProvider = _providers.FindHintTarget(Player.m_localPlayer, out _hintSnapshot);
        }
    }

    internal void Draw(InputPickerRegistration registration)
    {
        if (!IsDriver(registration))
        {
            return;
        }

        if (!_session.IsOpen || _current == null)
        {
            var hintControls = _hintProvider?.Controls;

            if (!_session.BlocksInput && _hintAllowed && _hintSnapshot != null && hintControls?.Enabled() == true)
            {
                string hint = hintControls.Hint();

                if (!string.IsNullOrEmpty(hint))
                {
                    string selected = hintControls.Selected();
                    string held = hintControls.HeldType();
                    string chosen = held ?? selected ?? hintControls.Preferences()?.Choose(
                        _hintSnapshot.Items.Where(item => item.IsAvailable).Select(item => item.Item.name));
                    var entry = _hintSnapshot.Items.FirstOrDefault(item => item.Item.name == chosen);
                    string name = entry == null ? "none" : Localize(entry.Item.m_itemData.m_shared.m_name);
                    string mode = hintControls.Stopped() ? "Stopped (release use)"
                        : held != null ? "Holding" : selected == null ? "Auto" : "Selected";
                    string text = $"{mode}: {name} "
                        + $"(inventory {entry?.InventoryCount ?? 0}, storage {entry?.StorageCount ?? 0}) | {hint}";
                    GUI.Box(new Rect((Screen.width - 700) / 2f, Screen.height - 155, 700, 28), text);
                }
            }

            return;
        }

        var controls = _activeProvider?.Controls;
        bool controlled = controls?.Enabled() == true;
        var preferences = controlled ? controls.Preferences() : null;
        string[] accepted = _current.Items.Select(entry => entry.Item.name).ToArray();
        var order = preferences?.Order(accepted).ToList();
        int totalPages = Math.Max(1, (_current.Items.Count + ItemsPerPage - 1) / ItemsPerPage);
        _page = Math.Min(_page, totalPages - 1);
        int startIndex = _page * ItemsPerPage;
        int visibleCount = Math.Min(ItemsPerPage, _current.Items.Count - startIndex);
        int columnCount = Math.Max(1, Math.Min(GridColumns, visibleCount));
        int rowCount = Math.Max(1, (visibleCount + GridColumns - 1) / GridColumns);
        float width = Math.Max(528, 40 + (columnCount * 165) - 7);
        float tileHeight = controlled ? 116 : 70;
        float gridTop = controlled ? 112 : 92;
        float gridBottom = gridTop + (rowCount * tileHeight) + ((rowCount - 1) * 7);
        float pageControlsHeight = totalPages > 1 ? 30 : 0;
        float actionsTop = gridBottom + 8 + pageControlsHeight;
        float helpTop = actionsTop + 42;
        float helpHeight = controlled ? 38 : 30;
        float height = helpTop + helpHeight + 10;
        _titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        _itemStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        _detailStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        _helpStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        float scale = Mathf.Min(1.25f, Mathf.Min(Screen.width / (width + 60), Screen.height / (height + 30)));
        var previousMatrix = GUI.matrix;
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        bool previousEnabled = GUI.enabled;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        GUI.depth = -100;

        try
        {
            float left = ((Screen.width / scale) - width) / 2;
            float top = ((Screen.height / scale) - height) / 2;
            var panelRect = new Rect(left, top, width, height);
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTextureWithTexCoords(panelRect, Texture2D.whiteTexture, new Rect(0, 0, 1, 1), true);
            GUI.color = previousColor;
            GUI.Box(panelRect, string.Empty);
            GUI.Label(
                new Rect(left + 15, top + 12, width - 30, 35),
                $"{Localize(_current.Name)} - {(controlled ? "input selection" : "input picker preview")}",
                _titleStyle);
            GUI.Label(
                new Rect(left + 20, top + 48, width - 40, controlled ? 60 : 38),
                controlled
                    ? $"Current: {GetCurrentChoice(controls, _current)}. Select a type, then Use highlighted.\n"
                        + "E uses it until changed, reset to Auto, or you leave.\n"
                        + "Nothing moves from this menu."
                    : "Preview only: this view does not change what E adds.",
                _itemStyle);

            for (int offset = 0; offset < visibleCount; offset++)
            {
                int index = startIndex + offset;
                var entry = _current.Items[index];
                int column = offset % GridColumns;
                int row = offset / GridColumns;
                int rowItems = Math.Min(GridColumns, visibleCount - (row * GridColumns));
                float rowWidth = (rowItems * 165) - 7;
                var rect = new Rect(
                    left + ((width - rowWidth) / 2) + (column * 165),
                    top + gridTop + (row * (tileHeight + 7)),
                    158,
                    tileHeight);
                bool selected = _selected == entry.Item.name;
                GUI.color = selected ? new Color(1f, 0.88f, 0.58f) : previousColor;
                GUI.Box(rect, string.Empty);
                GUI.enabled = previousEnabled && (entry.IsAvailable || controlled);

                if (GUI.Button(
                    new Rect(rect.x + 4, rect.y + 4, rect.width - 8, 34),
                    new GUIContent(string.Empty, controlled
                        ? "Highlight this type, then choose Use highlighted. It stays selected for this input until changed or reset to Auto. No item is added until you press E."
                        : "Preview only: highlighting this type does not change E behavior.")))
                {
                    _selected = entry.Item.name;
                }

                GUI.enabled = previousEnabled;
                GUI.color = previousColor;
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
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x + 7, rect.y + 7, 28, 28), texture, coordinates);
                }

                GUI.Label(
                    new Rect(rect.x + 39, rect.y + 5, 113, 32),
                    Localize(entry.Item.m_itemData.m_shared.m_name),
                    _itemStyle);
                GUI.Label(
                    new Rect(rect.x + 4, rect.y + 37, rect.width - 8, 24),
                    new GUIContent(
                        $"Inventory {entry.InventoryCount} | Storage {entry.StorageCount}",
                        "Current eligible counts, not a reservation. Availability can change before you press E."),
                    _detailStyle);

                if (controlled)
                {
                    bool hasPreferences = preferences != null;
                    bool excluded = hasPreferences && preferences.IsExcluded(entry.Item.name);
                    int rank = order?.IndexOf(entry.Item.name) + 1 ?? 0;
                    string autoLabel = !hasPreferences ? "Unavailable"
                        : excluded ? "Never auto-add"
                        : "Auto";

                    GUI.enabled = previousEnabled && hasPreferences;

                    if (GUI.Button(
                        new Rect(rect.x + 5, rect.y + 63, rect.width - 10, 21),
                        new GUIContent(
                            autoLabel,
                            excluded
                                ? "This type is excluded from automatic choices only; you can still select it manually."
                                : "This type is eligible for automatic choices. Click to exclude it from automatic selection.")))
                    {
                        controls.Save(preferences.ToggleExcluded(entry.Item.name));
                    }

                    GUI.enabled = previousEnabled && hasPreferences && rank > 1;

                    if (GUI.Button(
                        new Rect(rect.x + 5, rect.y + 88, 50, 24),
                        new GUIContent("Up", "Move this type earlier in the automatic input order.")))
                    {
                        controls.Save(preferences.MoveUp(entry.Item.name, accepted));
                    }

                    GUI.enabled = previousEnabled && hasPreferences;
                    GUI.Label(
                        new Rect(rect.x + 57, rect.y + 88, 38, 24),
                        new GUIContent($"#{rank}", "This is the automatic input priority. Automatic choice selects an item type first; inventory is searched before storage within that type."),
                        _detailStyle);
                    GUI.enabled = previousEnabled && hasPreferences && rank > 0 && rank < order.Count;

                    if (GUI.Button(
                        new Rect(rect.x + 97, rect.y + 88, 56, 24),
                        new GUIContent("Down", "Move this type later in the automatic input order.")))
                    {
                        controls.Save(preferences.MoveDown(entry.Item.name, accepted));
                    }

                    GUI.enabled = previousEnabled;
                }
            }

            if (_current.Items.Count == 0)
            {
                GUI.Label(new Rect(left + 20, top + gridTop, width - 40, tileHeight), "No discovered inputs for this station.", _itemStyle);
            }

            if (totalPages > 1)
            {
                float pageTop = top + gridBottom + 8;
                GUI.enabled = previousEnabled && _page > 0;

                if (GUI.Button(
                    new Rect(left + 30, pageTop, 120, 24),
                    new GUIContent("Previous", "Show the previous group of input types.")))
                {
                    _page--;
                    _selected = _current.Items[_page * ItemsPerPage].Item.name;
                }

                GUI.enabled = previousEnabled;
                GUI.Label(new Rect(left + ((width - 170) / 2), pageTop, 170, 24), $"Page {_page + 1} of {totalPages}", _detailStyle);
                GUI.enabled = previousEnabled && _page < totalPages - 1;

                if (GUI.Button(
                    new Rect(left + width - 150, pageTop, 120, 24),
                    new GUIContent("Next", "Show the next group of input types.")))
                {
                    _page++;
                    _selected = _current.Items[_page * ItemsPerPage].Item.name;
                }

                GUI.enabled = previousEnabled;
            }

            float actionTop = top + actionsTop;

            if (controlled)
            {
                float actionsLeft = left + ((width - 486) / 2);
                GUI.enabled = previousEnabled && preferences != null;

                if (GUI.Button(
                    new Rect(actionsLeft, actionTop, 160, 34),
                    new GUIContent("Use automatic choice", "Clear the manual type choice. This does not change priorities or exclusions.")))
                {
                    controls.Select(null);
                    Close();
                }

                GUI.enabled = previousEnabled && preferences != null
                    && _current != null && _current.Items.Any(entry => entry.Item.name == _selected && entry.IsAvailable);

                if (GUI.Button(
                    new Rect(actionsLeft + 168, actionTop, 185, 34),
                    new GUIContent("Use highlighted (Enter)", "Use this exact type for this input until changed or reset to Auto. No item moves until you press E.")))
                {
                    ApplySelection();
                }

                GUI.enabled = previousEnabled;

                if (GUI.Button(
                    new Rect(actionsLeft + 361, actionTop, 125, 34),
                    new GUIContent("Close (Esc / B)", "Close without changing your manual type choice.")))
                {
                    Close();
                }
            }
            else if (GUI.Button(
                new Rect(left + ((width - 200) / 2), actionTop, 200, 34),
                new GUIContent("Close (Esc / B)", "Close the preview. E behavior is unchanged.")))
            {
                Close();
            }

            string help = GetHelpText(controlled, preferences, order, _current.Items, _selected);

            if (!string.IsNullOrWhiteSpace(GUI.tooltip))
            {
                help = GUI.tooltip;
            }

            var helpRect = new Rect(left + 20, top + helpTop, width - 40, helpHeight);
            GUI.Box(helpRect, string.Empty);
            GUI.Label(new Rect(helpRect.x + 6, helpRect.y + 2, helpRect.width - 12, helpRect.height - 4), help, _helpStyle);
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.depth = previousDepth;
            GUI.color = previousColor;
            GUI.enabled = previousEnabled;
        }
    }

    internal void Unregister(InputPickerRegistration registration)
    {
        if (!_providers.Remove(registration.Provider))
        {
            registration.Detach();

            return;
        }

        if (_activeProvider == registration.Provider)
        {
            Close();
        }

        if (_hintProvider == registration.Provider)
        {
            _hintProvider = null;
            _hintSnapshot = null;
        }

        registration.Detach();

        if (_providers.IsEmpty)
        {
            _instance = null;
            Close();
            _harmony.UnpatchSelf();
            RestoreCursor();
        }
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

        if (allowed && !preview._session.BlocksInput
            && preview._lastOpenAttempt != Time.frameCount
            && !UnifiedPopup.IsVisible() && !Hud.IsPieceSelectionVisible() && !Hud.InRadial())
        {
            var provider = preview._providers.FindOpenTarget(
                player,
                key => ZInput.GetKeyDown(key),
                out var snapshot,
                out bool attempted);

            if (attempted)
            {
                preview._lastOpenAttempt = Time.frameCount;
            }

            if (provider != null)
            {
                preview._activeProvider = provider;
                preview._player = player;
                preview._current = snapshot;
                preview._selected = provider.Controls?.Enabled() == true ? provider.Controls.Selected() : null;
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

            foreach (var provider in preview._providers.All)
            {
                provider.Controls?.Observe(player, allowed);
            }
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
        var snapshot = _activeProvider.Snapshot(_player);

        if (snapshot == null || snapshot.Station != _current.Station)
        {
            Close();

            return;
        }

        _current = snapshot;
        _page = Math.Min(_page, Math.Max(0, (_current.Items.Count - 1) / ItemsPerPage));

        if (!_current.Items.Any(entry => (entry.IsAvailable || _activeProvider.Controls?.Enabled() == true)
            && entry.Item.name == _selected))
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

            if (_current.Items[index].IsAvailable || _activeProvider.Controls?.Enabled() == true)
            {
                _selected = _current.Items[index].Item.name;
                _page = index / ItemsPerPage;

                return;
            }
        }
    }

    private void ApplySelection()
    {
        var controls = _activeProvider?.Controls;

        if (controls?.Enabled() != true || controls.Preferences() == null
            || _current == null || !_current.Items.Any(entry => entry.Item.name == _selected && entry.IsAvailable))
        {
            return;
        }

        controls.Select(_selected);
        Close();
    }

    private static string GetCurrentChoice(InputPickerControls controls, InputPreviewSnapshot snapshot)
    {
        string selected = controls.Selected();

        if (selected == null)
        {
            return "Auto";
        }

        var item = snapshot.Items.FirstOrDefault(entry => entry.Item.name == selected);

        return item == null ? "unavailable" : Localize(item.Item.m_itemData.m_shared.m_name);
    }

    private static string GetHelpText(
        bool controlled,
        InputPreferences preferences,
        IReadOnlyList<string> order,
        IReadOnlyList<InputPreviewItem> items,
        string selected)
    {
        if (!controlled)
        {
            return "Preview only: counts are informational; this view does not change E behavior.";
        }

        if (preferences == null)
        {
            return "Preferences are invalid or unavailable. Automatic insertion is blocked; see the log.";
        }

        var entry = items.FirstOrDefault(item => item.Item.name == selected);

        if (entry == null)
        {
            return "Auto uses priority order; inventory is preferred within a type. Never auto-add affects automatic choices only.";
        }

        int rank = order.ToList().IndexOf(entry.Item.name) + 1;
        string mode = preferences.IsExcluded(entry.Item.name) ? "never auto-added" : "allowed for Auto";

        return $"{Localize(entry.Item.m_itemData.m_shared.m_name)}: priority #{rank}, {mode}. Manual selection can still choose excluded types.";
    }

    private void Close()
    {
        if (!_session.IsOpen)
        {
            _activeProvider = null;
            _current = null;
            _selected = null;
            _player = null;

            return;
        }

        _session.Close(Time.frameCount);
        _activeProvider = null;
        _current = null;
        _selected = null;
        _player = null;
        RestoreCursor();
    }
}