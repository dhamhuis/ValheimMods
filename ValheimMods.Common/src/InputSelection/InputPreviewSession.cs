namespace ValheimMods.Common.InputSelection;

internal sealed class InputPreviewSession
{
    private int _closedFrame = -1;

    internal bool IsOpen { get; private set; }

    internal bool BlocksInput { get; private set; }

    internal int OpenedFrame { get; private set; } = -1;

    internal bool CanProcessInput(int frame) => IsOpen && frame > OpenedFrame;

    internal void Open(int frame)
    {
        IsOpen = true;
        BlocksInput = true;
        OpenedFrame = frame;
    }

    internal void Close(int frame)
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _closedFrame = frame;
    }

    internal void UpdateRelease(int frame, bool controlsHeld)
    {
        if (!IsOpen && frame > _closedFrame && !controlsHeld)
        {
            BlocksInput = false;
        }
    }
}