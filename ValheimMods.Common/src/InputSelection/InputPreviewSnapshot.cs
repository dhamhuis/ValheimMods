using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimMods.Common.InputSelection;

public sealed class InputPreviewSnapshot
{
    public InputPreviewSnapshot(Component station, string name, IReadOnlyList<InputPreviewItem> items)
    {
        Station = station != null ? station : throw new ArgumentNullException(nameof(station));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public Component Station { get; }

    public string Name { get; }

    public IReadOnlyList<InputPreviewItem> Items { get; }
}