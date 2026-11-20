using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimMods.Common.InputSelection;

internal sealed class InputPickerProvider
{
    internal InputPickerProvider(
        Func<bool> enabled,
        Func<KeyCode> key,
        Func<Player, InputPreviewSnapshot> snapshot,
        InputPickerControls controls)
    {
        Enabled = enabled ?? throw new ArgumentNullException(nameof(enabled));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Controls = controls;
    }

    internal Func<bool> Enabled { get; }

    internal Func<KeyCode> Key { get; }

    internal Func<Player, InputPreviewSnapshot> Snapshot { get; }

    internal InputPickerControls Controls { get; }
}

internal sealed class InputPickerProviderRegistry
{
    private readonly List<InputPickerProvider> _providers = new();

    internal InputPickerProvider Driver => _providers.Count == 0 ? null : _providers[0];

    internal IReadOnlyList<InputPickerProvider> All => _providers;

    internal bool IsEmpty => _providers.Count == 0;

    internal void Add(InputPickerProvider provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (_providers.Contains(provider))
        {
            throw new InvalidOperationException("The input picker provider is already registered.");
        }

        _providers.Add(provider);
    }

    internal bool Remove(InputPickerProvider provider) => _providers.Remove(provider);

    internal InputPickerProvider FindOpenTarget(
        Player player,
        Func<KeyCode, bool> keyPressed,
        out InputPreviewSnapshot snapshot,
        out bool attempted)
    {
        attempted = false;

        foreach (var provider in _providers)
        {
            if (!provider.Enabled())
            {
                continue;
            }

            KeyCode key = provider.Key();

            if (key == KeyCode.None || !keyPressed(key))
            {
                continue;
            }

            attempted = true;
            snapshot = provider.Snapshot(player);

            if (snapshot != null)
            {
                return provider;
            }
        }

        snapshot = null;

        return null;
    }

    internal InputPickerProvider FindHintTarget(Player player, out InputPreviewSnapshot snapshot)
    {
        foreach (var provider in _providers)
        {
            if (!provider.Enabled() || provider.Controls?.Enabled() != true)
            {
                continue;
            }

            snapshot = provider.Snapshot(player);

            if (snapshot != null)
            {
                return provider;
            }
        }

        snapshot = null;

        return null;
    }
}