using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WorldClock;

internal static class ClockTextFormatter
{
    internal const string DefaultTemplate = "<b><font size=24 color=#FF8E0080>Day {day} <font size=18 color=#B4B4B480>|</font> {time} <font size=18 color=#B4B4B480>|</font> {phase}</font></b>";

    private static readonly Regex _placeholder = new Regex(@"\{([a-z]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _fontTag = new Regex(@"</?font\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _attribute = new Regex(@"\G\s+(size|color|name)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""']+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _hexColor = new Regex(@"^#(?:[0-9a-f]{6}|[0-9a-f]{8})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _colorTag = new Regex(@"</?color(?:\s*=\s*[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _hexColorTag = new Regex(@"(<color=)#([0-9a-f]{6})([0-9a-f]{2})?(>)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex _runTokens = new Regex(@"<worldclockfont(?:=([^>]*))?>|<worldclocktime(?:=end)?>|</?(?:b|i|size|color)(?:=[^>]*)?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static bool RequiresDay(string template)
    {
        return template != null && Regex.IsMatch(template, @"\{day\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static string Format(string template, string time, int day, string phase, bool markTime = false)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new FormatException("The display format cannot be empty.");
        }

        if (template.IndexOf("<worldclockfont", StringComparison.OrdinalIgnoreCase) >= 0
            || template.IndexOf("<worldclocktime", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new FormatException("The display format contains a reserved tag.");
        }

        string expanded = _placeholder.Replace(template, match =>
        {
            switch (match.Groups[1].Value.ToLowerInvariant())
            {
                case "time":
                    int openingTag = template.LastIndexOf('<', match.Index);
                    int closingTag = template.LastIndexOf('>', match.Index);

                    if (markTime && openingTag > closingTag)
                    {
                        return time;
                    }

                    if (markTime)
                    {
                        return "<worldclocktime>" + time + "<worldclocktime=end>";
                    }

                    return time;
                case "day":
                    return day.ToString(CultureInfo.InvariantCulture);
                case "phase":
                    return phase;
                default:
                    throw new FormatException($"Unknown placeholder '{match.Value}'. Use {{time}}, {{day}}, or {{phase}}.");
            }
        });
        var result = new StringBuilder();
        var openFonts = new Stack<(string Closing, string PreviousName)>();
        string currentFont = null;
        int previousEnd = 0;

        foreach (Match match in _fontTag.Matches(expanded))
        {
            result.Append(expanded, previousEnd, match.Index - previousEnd);

            if (match.Value.StartsWith("</", StringComparison.Ordinal))
            {
                if (!string.Equals(match.Value, "</font>", StringComparison.OrdinalIgnoreCase) || openFonts.Count == 0)
                {
                    throw new FormatException("Unexpected closing font tag.");
                }

                var previous = openFonts.Pop();
                result.Append(previous.Closing);

                if (currentFont != previous.PreviousName)
                {
                    currentFont = previous.PreviousName;
                    AppendFontMarker(result, currentFont);
                }
            }
            else
            {
                string attributes = match.Value.Substring(5, match.Value.Length - 6);
                string size = null;
                string color = null;
                string fontName = null;
                int position = 0;

                while (position < attributes.Length)
                {
                    if (string.IsNullOrWhiteSpace(attributes.Substring(position)))
                    {
                        break;
                    }

                    Match attribute = _attribute.Match(attributes, position);

                    if (!attribute.Success)
                    {
                        throw new FormatException("Font tags support only name, size and color attributes.");
                    }

                    string name = attribute.Groups[1].Value;
                    string value = attribute.Groups[2].Success ? attribute.Groups[2].Value
                        : attribute.Groups[3].Success ? attribute.Groups[3].Value : attribute.Groups[4].Value;

                    if (string.Equals(name, "size", StringComparison.OrdinalIgnoreCase))
                    {
                        if (size != null || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int pixels)
                            || pixels < 1 || pixels > 256)
                        {
                            throw new FormatException("Font size must be an integer from 1 to 256 and appear only once.");
                        }

                        size = pixels.ToString(CultureInfo.InvariantCulture);
                    }
                    else if (string.Equals(name, "color", StringComparison.OrdinalIgnoreCase))
                    {
                        if (color != null || !_hexColor.IsMatch(value))
                        {
                            throw new FormatException("Font color must be a single #RRGGBB or #RRGGBBAA value.");
                        }

                        color = value;
                    }
                    else
                    {
                        if (fontName != null || !FontCatalog.IsValidName(value))
                        {
                            throw new FormatException("Font name must be nonempty and cannot contain angle brackets, quotes or control characters.");
                        }

                        fontName = value;
                    }

                    position = attribute.Index + attribute.Length;
                }

                if (size == null && color == null && fontName == null)
                {
                    throw new FormatException("Font tags require a name, size or color attribute.");
                }

                string closing = string.Empty;
                string previousName = currentFont;

                if (fontName != null && fontName != currentFont)
                {
                    currentFont = fontName;
                    AppendFontMarker(result, currentFont);
                }

                if (size != null)
                {
                    result.Append("<size=").Append(size).Append('>');
                    closing = "</size>";
                }

                if (color != null)
                {
                    result.Append("<color=").Append(color).Append('>');
                    closing = "</color>" + closing;
                }

                openFonts.Push((closing, previousName));
            }

            previousEnd = match.Index + match.Length;
        }

        result.Append(expanded, previousEnd, expanded.Length - previousEnd);

        if (openFonts.Count != 0 || _fontTag.IsMatch(result.ToString())
            || Regex.IsMatch(result.ToString(), @"</?font\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            throw new FormatException("A font tag is unclosed or malformed.");
        }

        return result.ToString();
    }

    internal static string Shadow(string formattedText)
    {
        return _colorTag.Replace(formattedText, string.Empty);
    }

    internal static string PrepareColorsForRendering(string formattedText, bool linearColorSpace)
    {
        if (!linearColorSpace)
        {
            return formattedText;
        }

        return _hexColorTag.Replace(formattedText, match =>
        {
            string rgb = match.Groups[2].Value;
            string red = EncodeColorChannel(rgb.Substring(0, 2));
            string green = EncodeColorChannel(rgb.Substring(2, 2));
            string blue = EncodeColorChannel(rgb.Substring(4, 2));

            return match.Groups[1].Value + "#" + red + green + blue + match.Groups[3].Value + match.Groups[4].Value;
        });
    }

    internal static IReadOnlyList<(string FontName, string Text)> SplitRuns(string formattedText)
    {
        var result = new List<(string FontName, string Text)>();

        foreach (var run in SplitDisplayRuns(formattedText))
        {
            result.Add((run.FontName, run.Text));
        }

        return result;
    }

    internal static IReadOnlyList<(string FontName, string Text, bool IsTime, string Prefix, string Suffix)> SplitDisplayRuns(string formattedText)
    {
        var runs = new List<(string FontName, string Text, bool IsTime, string Prefix, string Suffix)>();
        var activeTags = new List<(string Name, string Opening)>();
        var text = new StringBuilder();
        string fontName = null;
        int previousEnd = 0;
        bool hasText = false;
        bool isTime = false;
        string prefix = string.Empty;

        foreach (Match match in _runTokens.Matches(formattedText))
        {
            string content = formattedText.Substring(previousEnd, match.Index - previousEnd);
            text.Append(content);
            hasText |= content.Length > 0;

            if (match.Value.StartsWith("<worldclockfont", StringComparison.OrdinalIgnoreCase)
                || match.Value.StartsWith("<worldclocktime", StringComparison.OrdinalIgnoreCase))
            {
                if (hasText)
                {
                    var suffix = new StringBuilder();

                    for (int i = activeTags.Count - 1; i >= 0; i--)
                    {
                        suffix.Append("</").Append(activeTags[i].Name).Append('>');
                    }

                    text.Append(suffix);
                    runs.Add((fontName, text.ToString(), isTime, prefix, suffix.ToString()));
                }

                text.Clear();

                foreach (var tag in activeTags)
                {
                    text.Append(tag.Opening);
                }

                prefix = text.ToString();
                hasText = false;

                if (match.Value.StartsWith("<worldclockfont", StringComparison.OrdinalIgnoreCase))
                {
                    fontName = match.Groups[1].Success ? match.Groups[1].Value : null;
                }
                else
                {
                    isTime = !match.Value.Equals("<worldclocktime=end>", StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                text.Append(match.Value);
                bool closing = match.Value.StartsWith("</", StringComparison.Ordinal);
                int nameStart = closing ? 2 : 1;
                int nameEnd = match.Value.IndexOfAny(new[] { '=', '>' }, nameStart);
                string tagName = match.Value.Substring(nameStart, nameEnd - nameStart).ToLowerInvariant();

                if (closing)
                {
                    if (activeTags.Count == 0 || activeTags[activeTags.Count - 1].Name != tagName)
                    {
                        throw new FormatException($"Unbalanced rich-text tag: {match.Value}.");
                    }

                    activeTags.RemoveAt(activeTags.Count - 1);
                }
                else
                {
                    activeTags.Add((tagName, match.Value));
                }
            }

            previousEnd = match.Index + match.Length;
        }

        string tail = formattedText.Substring(previousEnd);
        text.Append(tail);
        hasText |= tail.Length > 0;

        if (activeTags.Count != 0)
        {
            throw new FormatException("Rich-text tags must be properly closed.");
        }

        if (hasText)
        {
            runs.Add((fontName, text.ToString(), isTime, prefix, string.Empty));
        }

        return runs;
    }

    private static string EncodeColorChannel(string hex)
    {
        double value = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
        double encoded = value <= 0.0031308 ? value * 12.92 : (1.055 * Math.Pow(value, 1.0 / 2.4)) - 0.055;

        return ((int)Math.Round(encoded * 255.0, MidpointRounding.AwayFromZero)).ToString("X2", CultureInfo.InvariantCulture);
    }

    private static void AppendFontMarker(StringBuilder output, string fontName)
    {
        output.Append("<worldclockfont");

        if (fontName != null)
        {
            output.Append('=').Append(fontName);
        }

        output.Append('>');
    }
}