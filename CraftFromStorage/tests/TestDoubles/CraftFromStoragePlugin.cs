using System;
using System.Collections;
using System.Collections.Generic;
using ValheimMods.Testing;

namespace CraftFromStorage;

internal sealed class CraftFromStoragePlugin
{
    private readonly List<IEnumerator> _coroutines = new();

    internal static CraftFromStoragePlugin Instance { get; } = new();

    internal static TestLog Log { get; } = new();

    internal void StartCoroutine(IEnumerator coroutine)
    {
        if (coroutine.MoveNext())
        {
            _coroutines.Add(coroutine);
        }
    }

    internal void Advance()
    {
        _coroutines.RemoveAll(coroutine => !coroutine.MoveNext());
    }

    internal void Clear()
    {
        foreach (var coroutine in _coroutines)
        {
            (coroutine as IDisposable)?.Dispose();
        }

        _coroutines.Clear();
    }
}