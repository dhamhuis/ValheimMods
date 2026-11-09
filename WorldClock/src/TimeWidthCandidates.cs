using System;
using System.Collections.Generic;
using System.Globalization;

namespace WorldClock;

internal sealed class TimeWidthCandidates
{
    private readonly HashSet<string> _values = new HashSet<string>(StringComparer.Ordinal);

    internal TimeWidthCandidates(string format)
    {
        for (int seconds = 0; seconds < 86400; seconds++)
        {
            _values.Add(DateTime.MinValue.AddSeconds(seconds).ToString(format, CultureInfo.InvariantCulture));
        }
    }

    internal float Measure(Func<string, float> measure)
    {
        float widest = 0f;

        foreach (string value in _values)
        {
            widest = Math.Max(widest, measure(value));
        }

        return widest;
    }
}