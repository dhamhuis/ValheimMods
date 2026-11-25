using System;
using ValheimMods.Testing;

namespace BuildFromStorage.Tests;

internal static class Program
{
    private static void Main()
    {
        var runner = new TestRunner();
        PiecePlacementTests.Run(runner);

        Console.WriteLine($"{runner.TestsPassed} tests and {runner.AssertionsPassed} assertions passed.");
    }
}