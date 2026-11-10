using System.Collections.Generic;

namespace ValheimMods.Common.InputSelection;

public sealed class InputTypeSession
{
    private bool _held;
    private bool _stopped;
    private string _latched;

    public object Context { get; private set; }

    public string Selected { get; private set; }

    public bool CanInsert => Context != null && _held && !_stopped;

    public string HeldType => _latched;

    public bool IsStopped => _held && _stopped;

    public void Observe(object context, bool held, bool pressed, bool interrupted)
    {
        bool changed = !ReferenceEquals(Context, context);

        if (changed)
        {
            Context = context;
            Selected = null;
        }

        if (!held || pressed)
        {
            _latched = null;
            _stopped = false;
        }
        else if (changed && _held)
        {
            _stopped = true;
        }

        _held = held;

        if (interrupted && held)
        {
            _stopped = true;
        }
    }

    public void Select(string prefabName)
    {
        Selected = prefabName;
        _latched = null;
        _stopped = _held;
    }

    public string Resolve(InputPreferences preferences, IEnumerable<string> available)
    {
        if (!CanInsert)
        {
            return null;
        }

        _latched ??= Selected ?? preferences.Choose(available);

        return _latched;
    }

    public bool Stop()
    {
        bool first = !_stopped;
        _stopped = true;

        return first;
    }

    public void Clear()
    {
        Context = null;
        Selected = null;
        _latched = null;
        _stopped = false;
        _held = false;
    }
}