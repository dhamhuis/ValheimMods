using System.Collections.Generic;
using System.Linq;

namespace ValheimMods.Common.InputSelection;

public sealed class InputTypeSession
{
    private readonly bool _continueAcrossTargets;
    private bool _held;
    private bool _stopped;
    private string _latched;
    private object _target;

    /// <summary>
    /// Creates a held-input session. When <paramref name="continueAcrossTargets"/> is true, a continuous hold
    /// survives aiming away and back, restarts on a newly aimed station, and lets automatic choice move to the
    /// next available priority when the held type runs out.
    /// </summary>
    public InputTypeSession(bool continueAcrossTargets = false)
    {
        _continueAcrossTargets = continueAcrossTargets;
    }

    public object Context { get; private set; }

    public string Selected { get; private set; }

    public bool CanInsert => Context != null && _held && !_stopped;

    public string HeldType => _latched;

    public bool IsStopped => _held && _stopped;

    public void Observe(object context, bool held, bool pressed, bool interrupted)
    {
        bool changed = !ReferenceEquals(Context, context);
        Context = context;

        if (_continueAcrossTargets && _held && held && !pressed)
        {
            // Aiming at nothing keeps the hold anchored to its station; a different station starts afresh.
            if (context != null && !ReferenceEquals(context, _target))
            {
                _target = context;
                Selected = null;
                _latched = null;
                _stopped = false;
            }
        }
        else
        {
            if (!ReferenceEquals(_target, context))
            {
                _target = context;
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

        if (_continueAcrossTargets && Selected == null && _latched != null)
        {
            var current = available as ICollection<string> ?? available.ToList();
            available = current;

            if (!current.Contains(_latched))
            {
                _latched = null;
            }
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
        _target = null;
        _stopped = false;
        _held = false;
    }
}