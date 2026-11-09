using System;
using ValheimMods.Testing;

namespace WorldClock.Tests;

internal static class ClockLayoutTests
{
    internal static void Run(TestRunner runner)
    {
        runner.Run(PositionsLabelAtScreenEdgesAndCenter);
        runner.Run(SizesClockWithoutFixedWidth);
        runner.Run(ReducesTimeAdvanceWithoutChangingMeasuredText);
        runner.Run(AlignsSamplerColumns);
        runner.Run(AlignsSamplerRows);
    }

    private static void ReducesTimeAdvanceWithoutChangingMeasuredText(Action<bool, string> check)
    {
        check(ClockLayout.TimeAdvance(100f, 90f) == 98f, "Time advance did not shrink by two pixels.");
        check(ClockLayout.TimeAdvance(100f, 100f) == 98f, "The widest time changed the reserved advance.");
        check(ClockLayout.TimeAdvance(100f, 101f) == 99f, "An unexpectedly wider time was not accounted for.");
        check(ClockLayout.TimeAdvance(1f, 1f) == 0f, "Time advance became negative.");
    }

    private static void AlignsSamplerRows(Action<bool, string> check)
    {
        var tallerSample = ClockLayout.FontDemoRow(20f, 60f);
        check(tallerSample.Height == 68f && tallerSample.NameOffset == 24f && tallerSample.SampleOffset == 4f, "Tall sample was not centered beside its name.");
        var tallerName = ClockLayout.FontDemoRow(72f, 18f);
        check(tallerName.Height == 80f && tallerName.NameOffset == 4f && tallerName.SampleOffset == 31f, "Wrapped name was not centered beside its sample.");
        var shortRow = ClockLayout.FontDemoRow(10f, 10f);
        check(shortRow.Height == 30f && shortRow.NameOffset == 10f && shortRow.SampleOffset == 10f, "Minimum row did not center both columns.");
        var fractional = ClockLayout.FontDemoRow(20.5f, 22.25f);
        check(fractional.Height == 31f && fractional.SampleOffset >= 4f, "Fractional heights did not retain padding.");
    }

    private static void SizesClockWithoutFixedWidth(Action<bool, string> check)
    {
        var large = ClockLayout.LabelSize(1920f, 1080f, 800f, 50f);
        check(large.Width == 804f && large.Height == 58f, "Clock retained a fixed width or lost vertical padding.");
        var clipped = ClockLayout.LabelSize(320f, 200f, 400f, 300f);
        check(clipped.Width == 320f && clipped.Height == 200f, "Clock exceeded the screen.");
    }

    /// <summary>Position values zero, one-half and one place the label at the left, center and right.</summary>
    private static void PositionsLabelAtScreenEdgesAndCenter(Action<bool, string> check)
    {
        check(ClockLayout.HorizontalOffset(1920f, 180f, 0f) == 0f, "Left edge position was incorrect.");
        check(ClockLayout.HorizontalOffset(1920f, 180f, 0.5f) == 870f, "Center position was incorrect.");
        check(ClockLayout.HorizontalOffset(1920f, 180f, 1f) == 1740f, "Right edge position was incorrect.");
        check(ClockLayout.HorizontalOffset(320f, 320f, 1f) == 0f, "A full-width label extended beyond the screen.");
    }

    /// <summary>The sampler shares column offsets across rows and leaves space on narrow screens.</summary>
    private static void AlignsSamplerColumns(Action<bool, string> check)
    {
        var wide = ClockLayout.FontDemoColumns(800f, 250f);
        check(wide.NameWidth == 250f && wide.SampleOffset == 266f && wide.SampleWidth == 534f, "Wide columns were not aligned.");
        var narrow = ClockLayout.FontDemoColumns(100f, 250f);
        check(narrow.NameWidth == 45f && narrow.SampleOffset == 55f && narrow.SampleWidth == 45f, "Narrow columns did not leave room for samples.");
        var tiny = ClockLayout.FontDemoColumns(1f, 250f);
        check(tiny.NameWidth > 0f && tiny.SampleWidth > 0f && tiny.SampleOffset + tiny.SampleWidth == 1f, "Tiny columns exceeded the available width.");
    }
}