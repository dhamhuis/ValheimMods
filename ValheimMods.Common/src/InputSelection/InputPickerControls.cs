using System;

namespace ValheimMods.Common.InputSelection;

public sealed class InputPickerControls
{
    public InputPickerControls(
        Func<bool> enabled,
        Func<InputPreferences> preferences,
        Action<InputPreferences> save,
        Func<string> selected,
        Action<string> select,
        Action<Player, bool> observe,
        Func<string> hint,
        Func<string> heldType,
        Func<bool> stopped)
    {
        Enabled = enabled ?? throw new ArgumentNullException(nameof(enabled));
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        Save = save ?? throw new ArgumentNullException(nameof(save));
        Selected = selected ?? throw new ArgumentNullException(nameof(selected));
        Select = select ?? throw new ArgumentNullException(nameof(select));
        Observe = observe ?? throw new ArgumentNullException(nameof(observe));
        Hint = hint ?? throw new ArgumentNullException(nameof(hint));
        HeldType = heldType ?? throw new ArgumentNullException(nameof(heldType));
        Stopped = stopped ?? throw new ArgumentNullException(nameof(stopped));
    }

    public Func<bool> Enabled { get; }

    public Func<InputPreferences> Preferences { get; }

    public Action<InputPreferences> Save { get; }

    public Func<string> Selected { get; }

    public Action<string> Select { get; }

    public Action<Player, bool> Observe { get; }

    public Func<string> Hint { get; }

    public Func<string> HeldType { get; }

    public Func<bool> Stopped { get; }
}