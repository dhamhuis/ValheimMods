using System;
using ValheimMods.Testing;

namespace SupplyFromStorage.Tests;

internal static class Program
{
    private static void Main()
    {
        var runner = new TestRunner();
        SupplyBehaviorTests.Run(runner);
        PatchIntegrationTests.Run(runner);
        Console.WriteLine($"Passed {runner.TestsPassed} tests and {runner.AssertionsPassed} assertions.");
    }
}
