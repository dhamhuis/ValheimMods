using System;
using ValheimMods.Testing;

namespace WorldClock.Tests;

internal static class FontCatalogTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(UsesAndLabelsDefaultFont);
        runner.Run(ListsOnlyAvailableFonts);
        runner.Run(ValidatesFontNames);
    }

    private static void UsesAndLabelsDefaultFont(Action<bool, string> check)
    {
        check(FontCatalog.DefaultName == "Norsebold" && FontCatalog.DefaultSize == 22, "Default font or size changed.");
        check(FontCatalog.DemoLabelName == "AveriaSansLibre-Bold", "Demo labels did not use the readable preferred font.");
        check(FontCatalog.EffectiveName(null) == "Norsebold", "Unnamed text did not use the default font.");
        check(FontCatalog.EffectiveName("Arial") == "Arial", "Explicit font did not override the default.");
        check(FontCatalog.DisplayName("Norsebold") == "Norsebold (default)", "Default font was not labeled.");
        check(FontCatalog.DisplayName("norsebold") == "norsebold (default)", "Default font label was case-sensitive.");
        check(FontCatalog.DisplayName("Norse") == "Norse", "A non-default font was labeled as default.");
    }

    private static void ListsOnlyAvailableFonts(Action<bool, string> check)
    {
        string[] names = ["missing", "Zebra", "New Font", "Arial", "bad<name"];
        var available = FontCatalog.AvailableNames(names, name => name != "missing");
        check(available.Count == 3, "The demo did not filter missing and unusable fonts.");
        check(available[0] == "Arial" && available[1] == "New Font" && available[2] == "Zebra", "Available fonts were not sorted.");
        check(FontCatalog.AvailableNames(Array.Empty<string>(), name => true).Count == 0, "Empty catalog produced entries.");
    }

    private static void ValidatesFontNames(Action<bool, string> check)
    {
        check(FontCatalog.IsValidName("Font (Regular)/Medium"), "A usable font name was rejected.");
        check(FontCatalog.IsValidName("Norsebold"), "The default font was rejected.");
        check(!FontCatalog.IsValidName(" "), "A blank font name was accepted.");
        check(!FontCatalog.IsValidName("bad>name"), "A marker-breaking font name was accepted.");
        check(!FontCatalog.IsValidName("bad\"name"), "An unquotable font name was accepted.");
        string formatted = ClockTextFormatter.Format("<font name=\"Font (Regular)/Medium\">{time}</font>", "12:00", 1, "Midday");
        var runs = ClockTextFormatter.SplitRuns(formatted);
        check(runs.Count == 1 && runs[0].FontName == "Font (Regular)/Medium", "A listed font could not be used in a template.");
    }
}