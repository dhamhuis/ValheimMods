using ValheimMods.Testing;

namespace CookFromStorage;

internal static class CookFromStoragePlugin
{
    internal static bool AutomaticIngredientsEnabled { get; set; } = true;

    internal static bool InputSelectionEnabled { get; set; }

    internal static string InputPickerKeyName => "F8";

    internal static float StorageRadius { get; set; } = 20f;

    internal static TestLog Log { get; } = new();
}