public class Localization
{
    public static Localization instance { get; } = new();

    public string Localize(string text, params string[] words) => text + " " + string.Join(" ", words);
}