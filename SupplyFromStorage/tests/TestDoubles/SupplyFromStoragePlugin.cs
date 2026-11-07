using ValheimMods.Testing;

namespace SupplyFromStorage;

internal static class SupplyFromStoragePlugin
{
    internal static bool FuelAutomationEnabled { get; set; } = true;

    internal static bool ProcessingAutomationEnabled { get; set; } = true;

    internal static TestLog Log { get; } = new();
}
