using ValheimMods.Testing;

namespace SupplyFromStorage;

internal static class SupplyFromStoragePlugin
{
    internal static bool FuelAutomationEnabled { get; set; } = true;

    internal static bool ProcessingAutomationEnabled { get; set; } = true;

    internal static bool InputSelectionEnabled { get; set; }

    internal static string InputPickerKeyName => "F8";

    internal static float StorageRadius { get; set; } = 20f;

    internal static TestLog Log { get; } = new();
}