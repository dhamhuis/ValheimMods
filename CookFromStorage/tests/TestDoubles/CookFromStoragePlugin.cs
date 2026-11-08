using ValheimMods.Testing;

namespace CookFromStorage;

internal static class CookFromStoragePlugin
{
    internal static bool AutomaticIngredientsEnabled { get; set; } = true;

    internal static float StorageRadius { get; set; } = 20f;

    internal static TestLog Log { get; } = new();
}