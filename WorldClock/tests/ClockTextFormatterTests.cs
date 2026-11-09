using System;
using ValheimMods.Testing;

namespace WorldClock.Tests;

internal static class ClockTextFormatterTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(FormatsDefaultTemplate);
        runner.Run(ReplacesPlaceholdersCaseInsensitively);
        runner.Run(ConvertsCombinedAndNestedFontTags);
        runner.Run(PreservesConfiguredHexColor);
        runner.Run(CompensatesLinearRenderingColors);
        runner.Run(RemovesColorsFromShadowOnly);
        runner.Run(RejectsInvalidTemplates);
        runner.Run(DetectsDayDependency);
        runner.Run(SplitsNamedFontsAndRestoresNestedStyles);
    }

    private static void CompensatesLinearRenderingColors(Action<bool, string> check)
    {
        string template = "<b><font size=50><font color=#FFA500>ORANGE</font> | <font color=#808080>GREY</font> | <font color=#FFFFFF>WHITE</font></font></b>";
        string formatted = ClockTextFormatter.Format(template, "12:00", 42, "Midday");
        string compensated = ClockTextFormatter.PrepareColorsForRendering(formatted, true);
        check(compensated == "<b><size=50><color=#FFD200>ORANGE</color> | <color=#BCBCBC>GREY</color> | <color=#FFFFFF>WHITE</color></size></b>", "Linear-space colors were not converted correctly.");
        check(ClockTextFormatter.PrepareColorsForRendering(formatted, false) == formatted, "Gamma-space colors changed.");
        string alpha = ClockTextFormatter.Format("<font color=#0080FF7F>{time}</font>", "12:00", 1, "Midday");
        check(ClockTextFormatter.PrepareColorsForRendering(alpha, true) == "<color=#00BCFF7F>12:00</color>", "RGB endpoints or alpha changed incorrectly.");
        check(ClockTextFormatter.PrepareColorsForRendering("<color=red>red</color>", true) == "<color=red>red</color>", "Named color was changed.");
        check(ClockTextFormatter.Shadow(compensated) == "<b><size=50>ORANGE | GREY | WHITE</size></b>", "Compensation changed shadow styling.");
    }

    private static void PreservesConfiguredHexColor(Action<bool, string> check)
    {
        string template = "<b><font size=24 color=#FF8E00>Day {day} | {time} | {phase}</font></b>";
        string formatted = ClockTextFormatter.Format(template, "12:00", 42, "Midday");
        var runs = ClockTextFormatter.SplitRuns(formatted);

        check(runs.Count == 1, "The configured template unexpectedly split into multiple runs.");
        check(runs[0].Text == "<b><size=24><color=#FF8E00>Day 42 | 12:00 | Midday</color></size></b>", "The configured hex color changed during formatting.");
        check(ClockTextFormatter.Shadow(runs[0].Text) == "<b><size=24>Day 42 | 12:00 | Midday</size></b>", "The foreground color leaked into the shadow.");
    }

    /// <summary>The default template displays a bold day and time.</summary>
    private static void FormatsDefaultTemplate(Action<bool, string> check)
    {
        string template = ClockTextFormatter.DefaultTemplate;
        check(template == "<b><font size=24 color=#FF8E0080>Day {day} <font size=18 color=#B4B4B480>|</font> {time} <font size=18 color=#B4B4B480>|</font> {phase}</font></b>", "Default template changed.");
        string formatted = ClockTextFormatter.Format(template, "12:00", 42, "Midday");
        check(formatted == "<b><size=24><color=#FF8E0080>Day 42 <size=18><color=#B4B4B480>|</color></size> 12:00 <size=18><color=#B4B4B480>|</color></size> Midday</color></size></b>", "Default output was incorrect.");
        check(ClockTextFormatter.SplitRuns(ClockTextFormatter.PrepareColorsForRendering(formatted, true)).Count == 1, "The default template did not form a valid run.");
        check(ClockTextFormatter.Format("{time} | {phase}", "12:00", 42, "Midday") == "12:00 | Midday", "Time and phase template was incorrect.");
    }

    /// <summary>Placeholder names are case-insensitive, reorderable, and repeatable.</summary>
    private static void ReplacesPlaceholdersCaseInsensitively(Action<bool, string> check)
    {
        string formatted = ClockTextFormatter.Format("{PHASE} {Day} {TIME} {day}", "12:00", 7, "Midday");
        check(formatted == "Midday 7 12:00 7", "Placeholders were not replaced correctly.");
    }

    /// <summary>Combined and nested font tags translate to balanced Unity rich-text tags.</summary>
    private static void ConvertsCombinedAndNestedFontTags(Action<bool, string> check)
    {
        string template = "<font size=24 color=#FFAA00><b>Day <font color='#00ff00'>{day}</font> {time}</b></font>";
        string formatted = ClockTextFormatter.Format(template, "12:00", 42, "Midday");
        check(formatted == "<size=24><color=#FFAA00><b>Day <color=#00ff00>42</color> 12:00</b></color></size>", "Combined or nested tags were translated incorrectly.");
        check(ClockTextFormatter.Format("<FONT COLOR=#11223344 SIZE=\"18\">{time}</FONT>", "12:00", 42, "Midday")
            == "<size=18><color=#11223344>12:00</color></size>", "Quoted attributes, alpha, or case were not handled.");
    }

    /// <summary>The shadow retains sizing and bold styling without inheriting foreground colors.</summary>
    private static void RemovesColorsFromShadowOnly(Action<bool, string> check)
    {
        string foreground = ClockTextFormatter.Format("<font size=24 color=#ff0000><b>{time}</b></font>", "12:00", 1, "Midday");
        check(ClockTextFormatter.Shadow(foreground) == "<size=24><b>12:00</b></size>", "Shadow retained a foreground color or lost styling.");
        check(foreground.Contains("<color=#ff0000>"), "Foreground color was removed.");
        check(ClockTextFormatter.Shadow("<color=yellow><b>12:00</b></color>") == "<b>12:00</b>", "Native color tag changed the shadow.");
    }

    /// <summary>Invalid placeholders and malformed font tags produce explicit errors.</summary>
    private static void RejectsInvalidTemplates(Action<bool, string> check)
    {
        string[] invalid =
        [
            "{unknown}",
            "<font size=abc>{time}</font>",
            "<font size=257>{time}</font>",
            "<font color=#oops>{time}</font>",
            "<font size=24 size=25>{time}</font>",
            "<font name=\"\">{time}</font>",
            "<font name=Arial name=Norse>{time}</font>",
            "<font name=\"Arial<bad>\">{time}</font>",
            "<font size=24>{time}",
            "</font>{time}",
            "<font size=24",
        ];

        foreach (string template in invalid)
        {
            bool rejected = false;

            try
            {
                ClockTextFormatter.Format(template, "12:00", 1, "Midday");
            }
            catch (FormatException)
            {
                rejected = true;
            }

            check(rejected, $"Invalid template was accepted: {template}");
        }
    }

    /// <summary>Only a template containing a day placeholder requires the game day API.</summary>
    private static void DetectsDayDependency(Action<bool, string> check)
    {
        check(ClockTextFormatter.RequiresDay("Day {DAY}"), "Uppercase day placeholder was not detected.");
        check(!ClockTextFormatter.RequiresDay("{time} | {phase}"), "A day-free template required the day API.");
    }

    /// <summary>Named fonts form measured runs while nested styles and outer fonts remain balanced.</summary>
    private static void SplitsNamedFontsAndRestoresNestedStyles(Action<bool, string> check)
    {
        string template = "<font name=\"Norse\" size=24><b>Day <font name='Arial' color=#ff0000>{day}</font> {time}</b></font>";
        string formatted = ClockTextFormatter.Format(template, "12:00", 42, "Midday");
        var runs = ClockTextFormatter.SplitRuns(formatted);
        check(runs.Count == 3, "Expected three font runs.");
        check(runs[0].FontName == "Norse" && runs[0].Text == "<size=24><b>Day </b></size>", "Outer font first run was wrong.");
        check(runs[1].FontName == "Arial" && runs[1].Text == "<size=24><b><color=#ff0000>42</color></b></size>", "Nested font run was wrong.");
        check(runs[2].FontName == "Norse" && runs[2].Text == "<size=24><b> 12:00</b></size>", "Outer font was not restored.");
        check(ClockTextFormatter.Shadow(runs[1].Text) == "<size=24><b>42</b></size>", "Nested font shadow lost formatting.");
        var mixed = ClockTextFormatter.SplitRuns(ClockTextFormatter.Format("A<font name=Norse>B</font>C", "12:00", 42, "Midday"));
        check(mixed.Count == 3 && mixed[0].FontName == null && mixed[1].FontName == "Norse" && mixed[2].FontName == null, "Default font was not restored.");
        var spaced = ClockTextFormatter.SplitRuns(ClockTextFormatter.Format("<font name=\"Custom Font\">{time}</font>", "12:00", 42, "Midday"));
        check(spaced.Count == 1 && spaced[0].FontName == "Custom Font" && spaced[0].Text == "12:00", "Quoted font names with spaces failed.");
    }
}