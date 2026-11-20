using System;

namespace ValheimMods.Common.InputSelection;

public sealed class InputPickerRegistration : IDisposable
{
    private InputPickerPreview _owner;

    internal InputPickerRegistration(InputPickerPreview owner, InputPickerProvider provider)
    {
        _owner = owner;
        Provider = provider;
    }

    internal InputPickerProvider Provider { get; }

    public void Update()
    {
        _owner?.Update(this);
    }

    public void Draw()
    {
        _owner?.Draw(this);
    }

    public void Dispose()
    {
        _owner?.Unregister(this);
    }

    internal void Detach()
    {
        _owner = null;
    }
}