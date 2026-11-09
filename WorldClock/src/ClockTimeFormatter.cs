using System;
using System.Globalization;

namespace WorldClock;

internal static class ClockTimeFormatter
{
    private const int SecondsPerDay = 24 * 60 * 60;

    internal static (string Time, string Phase) Format(float dayFraction, string timeFormat)
    {
        return FormatSeconds(SecondsIntoDay(dayFraction), timeFormat);
    }

    internal static int SecondsIntoDay(float dayFraction)
    {
        if (float.IsNaN(dayFraction) || float.IsInfinity(dayFraction))
        {
            throw new ArgumentOutOfRangeException(nameof(dayFraction), "The day fraction must be finite.");
        }

        double normalizedFraction = dayFraction - Math.Floor(dayFraction);

        return (int)Math.Floor(normalizedFraction * SecondsPerDay);
    }

    internal static (string Time, string Phase) FormatSeconds(int secondsIntoDay, string timeFormat)
    {
        if (string.IsNullOrWhiteSpace(timeFormat))
        {
            throw new FormatException("The time format cannot be empty.");
        }

        int hour = secondsIntoDay / 3600;
        string time = DateTime.MinValue.AddSeconds(secondsIntoDay)
            .ToString(timeFormat, CultureInfo.InvariantCulture);

        string dayPhase = hour switch
        {
            < 4 => "Late night",
            < 6 => "Before dawn",
            < 8 => "Dawn",
            < 12 => "Morning",
            < 14 => "Midday",
            < 17 => "Afternoon",
            < 20 => "Evening",
            _ => "Night",
        };

        return (time, dayPhase);
    }
}