using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace WorldClock;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class WorldClockPlugin : BaseUnityPlugin
{
    public const string PluginName = "WorldClock";
    public const string PluginAuthor = "Hoose";
    public const string PluginGuid = "hoose.valheim.worldclock";
    public const string PluginVersion = "1.0.0";

    private ConfigEntry<float> _horizontalPosition;
    private ConfigEntry<float> _verticalPosition;
    private ConfigEntry<string> _displayFormat;
    private ConfigEntry<string> _timeFormat;
    private ConfigEntry<bool> _showFontDemo;
    private ConfigEntry<string> _fontDemoText;
    private readonly ClockDisplayCache _displayCache = new ClockDisplayCache();
    private IReadOnlyList<(string FontName, string Text, bool IsTime, string Prefix, string Suffix)> _runs;
    private IReadOnlyList<(string FontName, string Text, bool IsTime, string Prefix, string Suffix)> _measuredRuns;
    private List<(GUIStyle Style, GUIStyle ShadowStyle, string Text, string ShadowText, Vector2 Size, float Advance)> _measured;
    private readonly Dictionary<(Font Font, string Prefix, string Suffix), float> _timeWidths = new Dictionary<(Font Font, string Prefix, string Suffix), float>();
    private string _widthTimeFormat;
    private TimeWidthCandidates _timeCandidates;
    private float _measuredWidth;
    private float _measuredHeight;
    private bool _formatErrorLogged;
    private bool _demoFontWarningLogged;
    private string _missingFont;
    private float _nextFontRefresh;
    private Vector2 _demoScroll;
    private List<(string Name, string Sample, GUIStyle SampleStyle, float Height, float NameHeight, float SampleHeight, float NameOffset, float SampleOffset)> _demoRows;
    private GUIStyle _demoNameStyle;
    private GUIStyle _demoHintStyle;
    private float _demoContentWidth;
    private float _demoContentHeight;
    private string _demoSample;
    private (float NameWidth, float SampleOffset, float SampleWidth) _demoColumns;
    private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);
    private GUIStyle _clockStyle;
    private GUIStyle _shadowStyle;

    private void Awake()
    {
        _displayFormat = Config.Bind(
            "Display",
            "Display format",
            ClockTextFormatter.DefaultTemplate,
            "Whole clock template. Use {time}, {day}, and {phase}. Style with <b> and <font name=\"Norse\" size=24 color=#FFFFFF>.");
        _timeFormat = Config.Bind("Display", "Time format", "HH:mm", "A .NET custom date and time format string.");
        _horizontalPosition = Config.Bind(
            "Display",
            "Horizontal position",
            0.07f,
            new ConfigDescription(
                "Horizontal center of the clock as a fraction of screen width: 0 = left edge, 0.5 = center, 1 = right edge.",
                new AcceptableValueRange<float>(0f, 1f)));
        _verticalPosition = Config.Bind(
            "Display",
            "Vertical position",
            0f,
            new ConfigDescription(
                "Top edge of the clock, as a fraction of screen height.",
                new AcceptableValueRange<float>(0f, 1f)));
        _showFontDemo = Config.Bind("Font demo", "Show font demo", false, "Display all currently available fonts on screen, one per row. Open the game menu to use the mouse wheel over the list.");
        _fontDemoText = Config.Bind("Font demo", "Sample text", "Day 42 | 12:00", "Text shown after each font name in the sampler.");

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Active color space: {QualitySettings.activeColorSpace}.");
    }

    private void Update()
    {
        _displayCache.SetTemplate(_displayFormat.Value);

        if (Player.m_localPlayer == null || Hud.instance == null || EnvMan.instance == null
            || (_displayCache.RequiresDay && ZNet.instance == null))
        {
            _runs = null;

            return;
        }

        int day = _displayCache.RequiresDay ? EnvMan.instance.GetDay() : 0;
        _displayCache.Update(EnvMan.instance.GetDayFraction(), _timeFormat.Value, day, QualitySettings.activeColorSpace == ColorSpace.Linear);
        _runs = _displayCache.Runs;

        if (_displayCache.Error == null)
        {
            _formatErrorLogged = false;
        }
        else if (!_formatErrorLogged)
        {
            Logger.LogError($"Invalid WorldClock display or time format: {_displayCache.Error}");
            _formatErrorLogged = true;
        }
    }

    private void OnGUI()
    {
        if (_runs == null && (!_showFontDemo.Value || Player.m_localPlayer == null || Hud.instance == null))
        {
            return;
        }

        Color previousColor = GUI.color;
        Color previousContentColor = GUI.contentColor;

        try
        {
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            DrawOverlay();
        }
        finally
        {
            GUI.color = previousColor;
            GUI.contentColor = previousContentColor;
        }
    }

    private void DrawOverlay()
    {
        EnsureStyles();

        if (Time.unscaledTime >= _nextFontRefresh)
        {
            RefreshFonts();
        }

        if (_runs == null)
        {
            DrawFontDemo();

            return;
        }

        if (_measured != null)
        {
            foreach (var run in _measured)
            {
                if (run.Style.font == null)
                {
                    _measuredRuns = null;
                    break;
                }
            }
        }

        if (!ReferenceEquals(_measuredRuns, _runs))
        {
            var measured = new List<(GUIStyle Style, GUIStyle ShadowStyle, string Text, string ShadowText, Vector2 Size, float Advance)>();
            float textWidth = 0f;
            float textHeight = 0f;

            for (int index = 0; index < _runs.Count; index++)
            {
                var run = _runs[index];
                var style = new GUIStyle(_clockStyle);
                string fontName = FontCatalog.EffectiveName(run.FontName);

                if (!_fonts.TryGetValue(fontName, out Font font) || font == null)
                {
                    if (_missingFont != fontName)
                    {
                        Logger.LogWarning($"WorldClock font '{fontName}' is not currently loaded. Check the font demo or choose another font.");
                        _missingFont = fontName;
                    }

                    GUI.Label(new Rect(0f, 0f, Screen.width, 30f), $"WorldClock font unavailable: {fontName}", _clockStyle);

                    if (_showFontDemo.Value)
                    {
                        DrawFontDemo();
                    }

                    return;
                }

                style.font = font;
                Vector2 size = style.CalcSize(new GUIContent(run.Text));
                var shadow = new GUIStyle(style);
                shadow.normal.textColor = _shadowStyle.normal.textColor;
                float reserved = run.IsTime ? TimeWidth(style, font, run.Prefix, run.Suffix, size.x) : size.x;
                bool lastInFont = index == _runs.Count - 1
                    || !string.Equals(run.FontName, _runs[index + 1].FontName, StringComparison.OrdinalIgnoreCase);
                float advance = reserved + (lastInFont ? 4f : 0f);
                measured.Add((style, shadow, run.Text, ClockTextFormatter.Shadow(run.Text), size, advance));
                textWidth += advance;
                textHeight = Mathf.Max(textHeight, size.y);
            }

            _measured = measured;
            _measuredRuns = _runs;
            _measuredWidth = textWidth;
            _measuredHeight = textHeight;
        }

        _missingFont = null;
        var labelSize = ClockLayout.LabelSize(Screen.width, Screen.height, _measuredWidth, _measuredHeight);
        float width = labelSize.Width;
        float height = labelSize.Height;
        float x = ClockLayout.HorizontalOffset(Screen.width, width, _horizontalPosition.Value);
        float y = Mathf.Clamp(
            _verticalPosition.Value * Screen.height,
            0f,
            Mathf.Max(0f, Screen.height - height));
        GUI.BeginGroup(new Rect(x, y, width, height));
        float offset = Mathf.Max(0f, (width - _measuredWidth) / 2f);

        foreach (var run in _measured)
        {
            var rect = new Rect(offset, 0f, run.Size.x + 4f, height - 2f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), run.ShadowText, run.ShadowStyle);
            GUI.Label(rect, run.Text, run.Style);
            offset += run.Advance;
        }

        GUI.EndGroup();

        if (_showFontDemo.Value)
        {
            DrawFontDemo();
        }
    }

    private float TimeWidth(GUIStyle style, Font font, string prefix, string suffix, float actualWidth)
    {
        string format = _timeFormat.Value;

        if (_timeCandidates == null || _widthTimeFormat != format)
        {
            _timeCandidates = new TimeWidthCandidates(format);
            _widthTimeFormat = format;
            _timeWidths.Clear();
        }

        var key = (font, prefix, suffix);

        if (!_timeWidths.TryGetValue(key, out float width))
        {
            width = _timeCandidates.Measure(value =>
                style.CalcSize(new GUIContent(prefix + value + suffix)).x);
            width = Mathf.Ceil(width);
            _timeWidths.Add(key, width);
        }

        return ClockLayout.TimeAdvance(width, actualWidth);
    }

    private void EnsureStyles()
    {
        if (_clockStyle != null)
        {
            return;
        }

        _clockStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = FontCatalog.DefaultSize,
            richText = true,
            wordWrap = false,
            fixedWidth = 0f,
            fixedHeight = 0f,
            padding = new RectOffset(0, 0, 0, 0),
        };
        _clockStyle.normal.textColor = Color.white;

        _shadowStyle = new GUIStyle(_clockStyle);
        _shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
    }

    private void RefreshFonts()
    {
        _measuredRuns = null;
        _demoRows = null;
        _fonts.Clear();

        foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
        {
            if (font != null && FontCatalog.IsValidName(font.name) && !_fonts.ContainsKey(font.name))
            {
                _fonts.Add(font.name, font);
            }
        }

        _nextFontRefresh = Time.unscaledTime + 5f;
    }

    private void DrawFontDemo()
    {
        float viewportWidth = Mathf.Max(1f, Screen.width * 0.9f);
        float viewportHeight = Mathf.Max(1f, Screen.height * 0.65f);
        var viewport = new Rect((Screen.width - viewportWidth) / 2f, Screen.height * 0.2f, viewportWidth, viewportHeight);
        var inner = new Rect(viewport.x + 4f, viewport.y + 44f, Mathf.Max(1f, viewport.width - 8f), Mathf.Max(1f, viewport.height - 48f));
        float contentWidth = Mathf.Max(1f, inner.width - 20f);

        if (_demoRows != null)
        {
            foreach (var row in _demoRows)
            {
                if (row.SampleStyle.font == null)
                {
                    _demoRows = null;
                    break;
                }
            }
        }

        if (_demoRows == null || _demoContentWidth != contentWidth || _demoSample != _fontDemoText.Value)
        {
            var nameStyle = new GUIStyle(_clockStyle) { fontSize = 18, richText = false, wordWrap = true, alignment = TextAnchor.UpperLeft };

            if (_fonts.TryGetValue(FontCatalog.DemoLabelName, out Font demoFont) && demoFont != null)
            {
                nameStyle.font = demoFont;
                _demoFontWarningLogged = false;
            }
            else if (!_demoFontWarningLogged)
            {
                Logger.LogWarning($"WorldClock font demo label font '{FontCatalog.DemoLabelName}' is not currently loaded; using the Unity GUI label font.");
                _demoFontWarningLogged = true;
            }

            float maximumNameWidth = 0f;
            var orderedNames = FontCatalog.AvailableNames(_fonts.Keys, name => _fonts[name] != null);

            foreach (string name in orderedNames)
            {
                maximumNameWidth = Mathf.Max(maximumNameWidth, nameStyle.CalcSize(new GUIContent(FontCatalog.DisplayName(name))).x);
            }

            var columns = ClockLayout.FontDemoColumns(contentWidth, maximumNameWidth);
            var rows = new List<(string Name, string Sample, GUIStyle SampleStyle, float Height, float NameHeight, float SampleHeight, float NameOffset, float SampleOffset)>();
            float contentHeight = 0f;

            foreach (string name in orderedNames)
            {
                var sampleStyle = new GUIStyle(nameStyle);
                sampleStyle.font = _fonts[name];
                string sample = _fontDemoText.Value;

                string displayName = FontCatalog.DisplayName(name);
                float nameHeight = Mathf.Ceil(Mathf.Max(nameStyle.lineHeight, nameStyle.CalcHeight(new GUIContent(displayName), Mathf.Max(1f, columns.NameWidth))));
                float sampleHeight = Mathf.Ceil(Mathf.Max(sampleStyle.lineHeight, sampleStyle.CalcHeight(new GUIContent(sample), Mathf.Max(1f, columns.SampleWidth))));
                var row = ClockLayout.FontDemoRow(nameHeight, sampleHeight);
                rows.Add((displayName, sample, sampleStyle, row.Height, nameHeight, sampleHeight, row.NameOffset, row.SampleOffset));
                contentHeight += row.Height + 2f;
            }

            _demoNameStyle = nameStyle;
            _demoHintStyle = new GUIStyle(nameStyle) { fontSize = 14 };
            _demoColumns = columns;
            _demoRows = rows;
            _demoContentHeight = contentHeight;
            _demoContentWidth = contentWidth;
            _demoSample = _fontDemoText.Value;
        }

        GUI.Box(viewport, string.Empty);
        GUI.Label(new Rect(viewport.x + 8f, viewport.y + 4f, Mathf.Max(1f, viewport.width - 16f), 38f),
            "Font demo: open the game menu (Esc), then use the mouse wheel over the list to scroll.", _demoHintStyle);

        if (_demoRows.Count == 0)
        {
            GUI.Label(inner, "No usable fonts are currently loaded.", _demoNameStyle);

            return;
        }

        _demoScroll = GUI.BeginScrollView(inner, _demoScroll, new Rect(0f, 0f, contentWidth, _demoContentHeight));
        float y = 0f;

        foreach (var row in _demoRows)
        {
            GUI.BeginGroup(new Rect(0f, y, contentWidth, row.Height));
            GUI.Label(new Rect(0f, row.NameOffset, _demoColumns.NameWidth, row.NameHeight), row.Name, _demoNameStyle);
            GUI.Label(new Rect(_demoColumns.SampleOffset, row.SampleOffset, _demoColumns.SampleWidth, row.SampleHeight), row.Sample, row.SampleStyle);
            GUI.EndGroup();
            y += row.Height + 2f;
        }

        GUI.EndScrollView();
    }
}