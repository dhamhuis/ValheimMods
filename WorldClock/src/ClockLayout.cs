using System;

namespace WorldClock;

internal static class ClockLayout
{
    internal static float HorizontalOffset(float screenWidth, float labelWidth, float position)
    {
        float desiredOffset = (position * screenWidth) - (labelWidth / 2f);

        return Math.Max(0f, Math.Min(desiredOffset, screenWidth - labelWidth));
    }

    internal static (float Width, float Height) LabelSize(float screenWidth, float screenHeight, float textWidth, float textHeight)
    {
        return (Math.Min(screenWidth, textWidth + 4f), Math.Min(screenHeight, textHeight + 8f));
    }

    internal static float TimeAdvance(float maximumWidth, float actualWidth)
    {
        return Math.Max(0f, Math.Max(maximumWidth, actualWidth) - 2f);
    }

    internal static (float NameWidth, float SampleOffset, float SampleWidth) FontDemoColumns(float availableWidth, float maximumNameWidth)
    {
        float width = Math.Max(1f, availableWidth);
        float gap = Math.Min(16f, width / 10f);
        float nameWidth = Math.Min(Math.Max(0f, maximumNameWidth), (width - gap) / 2f);
        float sampleOffset = nameWidth + gap;

        return (nameWidth, sampleOffset, width - sampleOffset);
    }

    internal static (float Height, float NameOffset, float SampleOffset) FontDemoRow(float nameHeight, float sampleHeight)
    {
        float safeNameHeight = Math.Max(0f, nameHeight);
        float safeSampleHeight = Math.Max(0f, sampleHeight);
        float height = Math.Max(30f, (float)Math.Ceiling(Math.Max(safeNameHeight, safeSampleHeight)) + 8f);

        return (height, (height - safeNameHeight) / 2f, (height - safeSampleHeight) / 2f);
    }
}