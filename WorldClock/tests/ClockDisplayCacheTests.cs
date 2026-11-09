using System;
using ValheimMods.Testing;

namespace WorldClock.Tests;

internal static class ClockDisplayCacheTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(ReusesUnchangedDisplay);
        runner.Run(UpdatesForSecondsAndConfiguration);
        runner.Run(ReportsAndRecoversFromErrors);
    }

    private static void ReusesUnchangedDisplay(Action<bool, string> check)
    {
        var cache = new ClockDisplayCache();
        cache.SetTemplate(ClockTextFormatter.DefaultTemplate);
        check(cache.RequiresDay, "Day dependency was not detected.");
        cache.Update(0.5f, "HH:mm", 42, true);
        var first = cache.Runs;
        cache.Update(0.5f, "HH:mm", 42, true);
        check(ReferenceEquals(first, cache.Runs), "Repeated input rebuilt the display.");
        cache.Update(0.50002f, "HH:mm", 42, true);
        check(ReferenceEquals(first, cache.Runs), "An unchanged minute rebuilt the display.");
        cache.Update(0.501f, "HH:mm", 42, true);
        check(!ReferenceEquals(first, cache.Runs), "A changed minute did not rebuild the display.");
    }

    private static void UpdatesForSecondsAndConfiguration(Action<bool, string> check)
    {
        var cache = new ClockDisplayCache();
        cache.SetTemplate("{time}");
        cache.Update(0.5f, "HH:mm:ss", 0, false);
        var first = cache.Runs;
        cache.Update(0.50002f, "HH:mm:ss", 0, false);
        check(!ReferenceEquals(first, cache.Runs) && cache.Runs[0].Text == "12:00:01", "Seconds format did not update.");
        cache.SetTemplate("<font color=#808080>{time} {day} {phase}</font>");
        check(cache.RequiresDay, "Changed template did not update day dependency.");
        cache.Update(0.5f, "HH:mm", 2, false);
        first = cache.Runs;
        cache.Update(0.5f, "HH:mm", 3, false);
        check(!ReferenceEquals(first, cache.Runs) && cache.Runs[1].Text.Contains("3 Midday"), "A day change was not reflected.");
        first = cache.Runs;
        cache.Update(0.5f, "HH:mm", 3, true);
        check(!ReferenceEquals(first, cache.Runs) && cache.Runs[0].Text.Contains("#BCBCBC"), "Color-space change was not reflected.");
        first = cache.Runs;
        cache.Update(0.5f, "h:mm tt", 3, true);
        check(!ReferenceEquals(first, cache.Runs) && cache.Runs[0].Text.Contains("12:00 PM"), "Time-format change was not reflected.");
        cache.Update(0.5f, "HH:mm", 3, true);
        first = cache.Runs;
        cache.Update(0.5f, "hh:mm", 3, true);
        check(!ReferenceEquals(first, cache.Runs), "A format change with the same current text did not invalidate the measured width.");
        cache.SetTemplate("{phase}");
        check(!cache.RequiresDay, "Removed day dependency was retained.");
        cache.Update(0.49f, "HH:mm", 0, false);
        first = cache.Runs;
        cache.Update(0.5f, "HH:mm", 0, false);
        check(!ReferenceEquals(first, cache.Runs) && cache.Runs[0].Text == "Midday", "Phase transition was not reflected.");
    }

    private static void ReportsAndRecoversFromErrors(Action<bool, string> check)
    {
        var cache = new ClockDisplayCache();
        cache.SetTemplate("{invalid}");
        cache.Update(0.5f, "HH:mm", 0, false);
        var invalid = cache.Runs;
        check(cache.Error != null && invalid[0].Text == "Invalid clock format", "Invalid template was not reported.");
        cache.Update(0.5f, "HH:mm", 0, false);
        check(ReferenceEquals(invalid, cache.Runs) && cache.Error != null, "Invalid input was not retained.");
        cache.SetTemplate("{time}");
        cache.Update(0.5f, "\"", 0, false);
        check(cache.Error != null, "Invalid time format was not reported.");
        cache.Update(0.5f, "HH:mm", 0, false);
        check(cache.Error == null && cache.Runs[0].Text == "12:00", "Corrected configuration did not recover.");
    }
}