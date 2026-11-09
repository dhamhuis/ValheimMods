using System;
using ValheimMods.Testing;

namespace WorldClock.Tests;

internal static class Program
{
    private static void Main()
    {
        var runner = new TestRunner();
        ClockTimeFormatterTests.Run(runner);
        ClockDisplayCacheTests.Run(runner);
        TimeWidthCandidatesTests.Run(runner);
        ClockTextFormatterTests.Run(runner);
        ClockLayoutTests.Run(runner);
        FontCatalogTests.Run(runner);

        Console.WriteLine($"{runner.TestsPassed} tests and {runner.AssertionsPassed} assertions passed.");
    }
}