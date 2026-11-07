using System;
using ValheimMods.Testing;

namespace CraftFromStorage.Tests;

internal static class Program
{
    private static void Main()
    {
        var runner = new TestRunner();

        try
        {
            StorageBehaviorTests.Run(runner);
            PatchIntegrationTests.Run(runner);
        }
        finally
        {
            CraftingTestSetup.Reset();
        }

        Console.WriteLine($"{runner.TestsPassed} tests and {runner.AssertionsPassed} assertions passed.");
    }
}