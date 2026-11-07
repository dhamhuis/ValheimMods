using System;

namespace ValheimMods.Testing;

public sealed class TestRunner
{
    public int TestsPassed { get; private set; }

    public int AssertionsPassed { get; private set; }

    public void Run(Action<Action<bool, string>> test)
    {
        try
        {
            test(Check);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"FAIL {test.Method.DeclaringType.Name}.{test.Method.Name}", exception);
        }

        TestsPassed++;
        Console.WriteLine($"PASS {test.Method.DeclaringType.Name}.{test.Method.Name}");
    }

    private void Check(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }

        AssertionsPassed++;
    }
}