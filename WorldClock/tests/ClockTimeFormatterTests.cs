using System;
using ValheimMods.Testing;

namespace WorldClock.Tests;

internal static class ClockTimeFormatterTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(FormatsMiddayAsTwentyFourHourTime);
        runner.Run(FormatsMiddayWithTwelveHourClock);
        runner.Run(WrapsFractionsAcrossDayBoundaries);
        runner.Run(AppendsExpectedDayPhase);
        runner.Run(RejectsInvalidTimeFormats);
        runner.Run(RejectsNonFiniteDayFractions);
    }

    /// <summary>A half-day fraction formats as noon using the default clock format.</summary>
    private static void FormatsMiddayAsTwentyFourHourTime(Action<bool, string> check)
    {
        check(ClockTimeFormatter.Format(0.5f, "HH:mm").Time == "12:00", "Midday did not format as 12:00.");
    }

    /// <summary>A configured twelve-hour format uses invariant AM/PM markers.</summary>
    private static void FormatsMiddayWithTwelveHourClock(Action<bool, string> check)
    {
        check(ClockTimeFormatter.Format(0.5f, "h:mm tt").Time == "12:00 PM", "Twelve-hour formatting was incorrect.");
    }

    /// <summary>Fractions at and outside the day boundary wrap into the same 24-hour cycle.</summary>
    private static void WrapsFractionsAcrossDayBoundaries(Action<bool, string> check)
    {
        check(ClockTimeFormatter.Format(1f, "HH:mm").Time == "00:00", "A full day did not wrap to midnight.");
        check(ClockTimeFormatter.Format(-0.25f, "HH:mm").Time == "18:00", "A negative fraction did not wrap to 18:00.");
    }

    /// <summary>Optional phase labels follow the documented fixed hour ranges.</summary>
    private static void AppendsExpectedDayPhase(Action<bool, string> check)
    {
        check(ClockTimeFormatter.Format(0.25f, "HH:mm").Phase == "Dawn", "Dawn phase was incorrect.");
        check(ClockTimeFormatter.Format(0.5f, "HH:mm").Phase == "Midday", "Midday phase was incorrect.");
    }

    /// <summary>An invalid custom time format is reported instead of silently replaced.</summary>
    private static void RejectsInvalidTimeFormats(Action<bool, string> check)
    {
        bool rejected = false;

        try
        {
            ClockTimeFormatter.Format(0.5f, "\"");
        }
        catch (FormatException)
        {
            rejected = true;
        }

        check(rejected, "An unmatched quote in the time format was accepted.");
    }

    /// <summary>Non-finite day fractions are rejected instead of producing misleading time.</summary>
    private static void RejectsNonFiniteDayFractions(Action<bool, string> check)
    {
        bool rejected = false;

        try
        {
            ClockTimeFormatter.Format(float.NaN, "HH:mm");
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }

        check(rejected, "A non-finite day fraction was accepted.");
    }
}