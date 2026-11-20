public class Localization
{
    public static Localization instance { get; } = new();

    public string Localize(string text) => text == "$item_raw_meat" ? "Boar meat" : text;
}