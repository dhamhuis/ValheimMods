using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WorldClock;

internal static class FontCatalog
{
    internal const string DefaultName = "Norsebold";
    internal const int DefaultSize = 22;
    internal const string DemoLabelName = "AveriaSansLibre-Bold";

    private static readonly Regex _validName = new Regex(@"^[^<>""'\x00-\x1f\x7f]+$", RegexOptions.CultureInvariant);

    internal static bool IsValidName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && _validName.IsMatch(name);
    }

    internal static string EffectiveName(string name)
    {
        return name ?? DefaultName;
    }

    internal static string DisplayName(string name)
    {
        return string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase) ? $"{name} (default)" : name;
    }

    internal static IReadOnlyList<string> AvailableNames(IEnumerable<string> names, Predicate<string> isAvailable)
    {
        var available = new List<string>();

        foreach (string name in names)
        {
            if (IsValidName(name) && isAvailable(name))
            {
                available.Add(name);
            }
        }

        available.Sort(StringComparer.OrdinalIgnoreCase);

        return available;
    }
}