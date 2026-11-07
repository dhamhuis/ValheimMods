using System;

namespace ValheimMods.Testing;

public sealed class TestLog
{
    public void LogWarning(string message) => Console.WriteLine($"  expected warning: {message}");

    public void LogInfo(string message) => Console.WriteLine($"  info: {message}");

    public void LogError(string message) => Console.WriteLine($"  error: {message}");
}