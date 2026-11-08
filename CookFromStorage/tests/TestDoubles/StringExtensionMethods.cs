using System;

public static class StringExtensionMethods
{
    // Tests need consistent identities, not the game's actual stable-hash algorithm.
    public static int GetStableHashCode(this string value) => StringComparer.Ordinal.GetHashCode(value);
}