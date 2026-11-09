using System;
using System.Collections.Generic;

namespace WorldClock;

internal sealed class ClockDisplayCache
{
    private static readonly IReadOnlyList<(string FontName, string Text, bool IsTime, string Prefix, string Suffix)> _invalidRuns =
        new[] { (FontName: (string)null, Text: "Invalid clock format", IsTime: false, Prefix: string.Empty, Suffix: string.Empty) };

    private string _template;
    private bool _hasTemplate;
    private bool _requiresDay;
    private bool _hasInput;
    private int _seconds;
    private int _day;
    private string _timeFormat;
    private bool _linearColorSpace;
    private string _renderedTemplate;
    private string _renderedTime;
    private string _renderedTimeFormat;
    private string _renderedPhase;
    private int _renderedDay;
    private bool _renderedLinearColorSpace;

    internal bool RequiresDay => _requiresDay;

    internal string Error { get; private set; }

    internal IReadOnlyList<(string FontName, string Text, bool IsTime, string Prefix, string Suffix)> Runs { get; private set; }

    internal void SetTemplate(string template)
    {
        if (_hasTemplate && _template == template)
        {
            return;
        }

        _template = template;
        _hasTemplate = true;
        _requiresDay = ClockTextFormatter.RequiresDay(template);
        _hasInput = false;
    }

    internal void Update(float fraction, string timeFormat, int day, bool linearColorSpace)
    {
        int seconds = ClockTimeFormatter.SecondsIntoDay(fraction);

        if (_hasInput && _seconds == seconds && _day == day
            && _timeFormat == timeFormat && _linearColorSpace == linearColorSpace)
        {
            return;
        }

        _hasInput = true;
        _seconds = seconds;
        _day = day;
        _timeFormat = timeFormat;
        _linearColorSpace = linearColorSpace;

        try
        {
            var (time, phase) = ClockTimeFormatter.FormatSeconds(seconds, timeFormat);

            if (Error == null && Runs != null && _renderedTemplate == _template
                && _renderedTime == time && _renderedTimeFormat == timeFormat && _renderedPhase == phase
                && _renderedDay == day && _renderedLinearColorSpace == linearColorSpace)
            {
                return;
            }

            string formatted = ClockTextFormatter.Format(_template, time, day, phase, true);
            formatted = ClockTextFormatter.PrepareColorsForRendering(formatted, linearColorSpace);
            Runs = ClockTextFormatter.SplitDisplayRuns(formatted);
            _renderedTemplate = _template;
            _renderedTime = time;
            _renderedTimeFormat = timeFormat;
            _renderedPhase = phase;
            _renderedDay = day;
            _renderedLinearColorSpace = linearColorSpace;
            Error = null;
        }
        catch (FormatException exception)
        {
            Runs = _invalidRuns;
            Error = exception.Message;
        }
    }
}