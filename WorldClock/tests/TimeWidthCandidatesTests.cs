using System;
using System.Globalization;
using ValheimMods.Testing;

namespace WorldClock.Tests;

internal static class TimeWidthCandidatesTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(ReservesAcrossFullDay);
        runner.Run(SplitsStyledTimeRuns);
    }

    private static void ReservesAcrossFullDay(Action<bool, string> check)
    {
        string[] formats = ["HH:mm", "HH:mm:ss", "h:mm tt", "H:m:s", "'literal' HH:mm", "yyyy-MM-dd HH:mm:ss"];

        foreach (string format in formats)
        {
            var candidates = new TimeWidthCandidates(format);
            float width = candidates.Measure(TextWidth);
            bool fits = true;

            for (int seconds = 0; seconds < 86400; seconds++)
            {
                string text = DateTime.MinValue.AddSeconds(seconds).ToString(format, CultureInfo.InvariantCulture);
                fits &= TextWidth(text) <= width;
            }

            check(fits, $"Width did not cover all times for {format}.");
        }
    }

    private static float GlyphWidth(char character)
    {
        return character == '1' ? 2f : character == '8' ? 9f : character == 'M' ? 12f : 5f;
    }

    private static float TextWidth(string text)
    {
        float width = text.Contains("11") ? 20f : 0f;

        foreach (char character in text)
        {
            width += GlyphWidth(character);
        }

        return width;
    }

    private static void SplitsStyledTimeRuns(Action<bool, string> check)
    {
        string format = "<font name=\"Norse\" size=24><b>Before {time} after <font name=\"Arial\" size=18>{TIME}</font> end</b></font>";
        string output = ClockTextFormatter.Format(format, "12:00", 1, "Midday", true);
        var runs = ClockTextFormatter.SplitDisplayRuns(output);
        check(runs.Count == 5, "Time and font boundaries did not produce separate runs.");
        check(!runs[0].IsTime && runs[1].IsTime && !runs[2].IsTime && runs[3].IsTime && !runs[4].IsTime, "Time run boundaries were incorrect.");
        check(runs[1].FontName == "Norse" && runs[1].Text == "<size=24><b>12:00</b></size>", "First time lost its font or style.");
        check(runs[3].FontName == "Arial" && runs[3].Text == "<size=24><b><size=18>12:00</size></b></size>", "Repeated time lost nested font or style.");
        check(runs[3].Prefix + "8" + runs[3].Suffix == "<size=24><b><size=18>8</size></b></size>", "Measurement did not retain surrounding styling.");
        string attribute = ClockTextFormatter.Format("<font name=\"{time}\">text</font>", "Arial", 1, "Midday", true);
        check(ClockTextFormatter.SplitDisplayRuns(attribute)[0].FontName == "Arial", "A placeholder inside a font attribute was altered.");
    }
}