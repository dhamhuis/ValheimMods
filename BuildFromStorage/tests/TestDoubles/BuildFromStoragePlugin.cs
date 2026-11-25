using System.Collections.Generic;

namespace BuildFromStorage;

internal static class BuildFromStoragePlugin
{
    internal static BuildStorageRegistry StorageRegistry { get; set; }

    internal static float StorageRadius { get; set; }

    internal static List<string> Warnings { get; } = new();

    internal static List<string> Errors { get; } = new();

    internal static void LogWarning(string message) => Warnings.Add(message);

    internal static void LogError(string message) => Errors.Add(message);
}