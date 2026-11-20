public class Localization
{
    public static Localization instance { get; } = new();

    public string Localize(string text, params string[] words)
    {
        if (text == "$item_finewood")
        {
            return "Finewood";
        }

        return text + " " + string.Join(" ", words);
    }
}