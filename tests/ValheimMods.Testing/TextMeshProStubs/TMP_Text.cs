using UnityEngine;

namespace TMPro;

public class TMP_Text : Component
{
    public string text { get; set; } = string.Empty;

    public bool enableAutoSizing { get; set; }

    public float fontSize { get; set; }

    public float fontSizeMin { get; set; }

    public float fontSizeMax { get; set; }

    public TextOverflowModes overflowMode { get; set; }

    public TextWrappingModes textWrappingMode { get; set; }

    public Color color { get; set; }
}