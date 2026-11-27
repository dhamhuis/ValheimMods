using System;
using ValheimMods.Testing;

namespace FarmFromStorage.Tests;

internal static class Program
{
    private static void Main()
    {
        var runner = new TestRunner();
        PlantingTests.Run(runner);

        Console.WriteLine($"{runner.TestsPassed} tests and {runner.AssertionsPassed} assertions passed.");
    }
}