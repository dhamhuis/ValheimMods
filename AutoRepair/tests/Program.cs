using System;
using ValheimMods.Testing;

namespace AutoRepair.Tests;

internal static class Program
{
    private static void Main()
    {
        var runner = new TestRunner();
        StationInteractionTests.Run(runner);

        Console.WriteLine($"{runner.TestsPassed} tests and {runner.AssertionsPassed} assertions passed.");
    }
}