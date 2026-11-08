using System;
using ValheimMods.Testing;

namespace CookFromStorage.Tests;

internal static class Program
{
    private static void Main()
    {
        var runner = new TestRunner();
        PatchIntegrationTests.Run(runner);
        FermenterIntegrationTests.Run(runner);
        RecipeIntegrationTests.Run(runner);
        Console.WriteLine($"Passed {runner.TestsPassed} tests and {runner.AssertionsPassed} assertions.");
    }
}