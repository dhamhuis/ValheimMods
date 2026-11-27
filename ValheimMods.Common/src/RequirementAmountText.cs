using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ValheimMods.Common;

// Shows inventory and storage counts on a separate label one line below res_amount. The vanilla field is single-line
// and auto-sized, so extra text there shrinks the required amount and is clipped. In the crafting panel a label below
// it is hidden by the control under the requirements, so there the label takes res_amount's original place and
// res_amount moves up one line while counts are shown. Tooltips raise it too, but their item names wrap onto a
// second line that the raised amount would overlap, so res_name is kept on one line, shrinking to fit, while
// counts are shown.
internal static class RequirementAmountText
{
    internal const string CountsLabelName = "hoose_res_counts";
    internal const string RaisedCountsLabelName = "hoose_res_counts_raised";
    internal const float MinNameScale = 0.5f;

    internal static readonly Color CountsColor = new(1f, 0.843f, 0f, 1f);

    private static readonly Dictionary<TMP_Text, NameLayout> _fittedNames = new();

    // Inventory and storage counts as "carried:stored"; a zero side is left blank, for example "12:" or ":56".
    internal static string FormatCounts(int carried, int stored)
    {
        if (carried <= 0 && stored <= 0)
        {
            return string.Empty;
        }

        string carriedText = carried > 0 ? carried.ToString() : string.Empty;
        string storedText = stored > 0 ? stored.ToString() : string.Empty;

        return $"{carriedText}:{storedText}";
    }

    // Requirement elements are reused across tools and recipes, so every display prefix clears stale counts.
    internal static void Clear(Transform elementRoot)
    {
        var labelRoot = FindLabelRoot(elementRoot);
        var label = labelRoot == null ? null : labelRoot.GetComponent<TMP_Text>();

        if (label != null)
        {
            label.text = string.Empty;
            PlaceAmount(elementRoot, labelRoot, false);
        }

        RestoreName(elementRoot);
        var belowLabel = elementRoot == null ? null : elementRoot.Find(CountsLabelName);
        var below = belowLabel == null ? null : belowLabel.GetComponent<TMP_Text>();

        if (below != null)
        {
            below.text = string.Empty;
        }
    }

    // Vanilla fills some requirement slots without SetupRequirement (the build tooltip's station slot) or leaves
    // surplus slots untouched, so whole lists are cleared before they are redrawn.
    internal static void ClearAll(GameObject[] elements)
    {
        if (elements == null)
        {
            return;
        }

        foreach (var element in elements)
        {
            if (element != null)
            {
                Clear(element.transform);
            }
        }
    }

    internal static void Apply(Transform elementRoot, int carried, int stored, bool raiseAmount = false, bool fitName = false)
    {
        string counts = FormatCounts(carried, stored);
        string labelName = raiseAmount ? RaisedCountsLabelName : CountsLabelName;
        var labelRoot = elementRoot == null ? null : elementRoot.Find(labelName);

        if (labelRoot == null)
        {
            if (counts.Length == 0)
            {
                return;
            }

            labelRoot = CreateLabel(elementRoot, labelName, !raiseAmount);

            if (labelRoot == null)
            {
                return;
            }
        }

        var label = labelRoot.GetComponent<TMP_Text>();

        if (label == null)
        {
            return;
        }

        label.text = counts;

        if (raiseAmount)
        {
            PlaceAmount(elementRoot, labelRoot, counts.Length > 0);
        }

        if (fitName && counts.Length > 0)
        {
            FitName(elementRoot);
        }
    }

    internal static TMP_Text FindLabel(Transform elementRoot)
    {
        var labelRoot = elementRoot == null ? null : elementRoot.Find(CountsLabelName);

        if (labelRoot == null)
        {
            labelRoot = FindLabelRoot(elementRoot);
        }

        return labelRoot == null ? null : labelRoot.GetComponent<TMP_Text>();
    }

    private static Transform FindLabelRoot(Transform elementRoot) => elementRoot == null ? null : elementRoot.Find(RaisedCountsLabelName);

    // A raised clone keeps res_amount's original position, which is restored to res_amount whenever counts are
    // hidden; otherwise the clone sits one line below the unmoved res_amount.
    private static Transform CreateLabel(Transform elementRoot, string labelName, bool placeBelow)
    {
        var amountRoot = elementRoot == null ? null : elementRoot.Find("res_amount");
        var amount = amountRoot == null ? null : amountRoot.GetComponent<TMP_Text>();

        if (amount == null)
        {
            return null;
        }

        var labelObject = Object.Instantiate(amountRoot.gameObject, elementRoot);
        labelObject.name = labelName;
        var label = labelObject.transform.GetComponent<TMP_Text>();

        if (label == null)
        {
            return null;
        }

        label.enableAutoSizing = false;
        label.fontSize = amount.fontSize;
        label.overflowMode = TextOverflowModes.Overflow;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = CountsColor;

        if (placeBelow && labelObject.transform is RectTransform labelRect)
        {
            labelRect.anchoredPosition -= new Vector2(0f, labelRect.rect.height);
        }

        return labelObject.transform;
    }

    // Auto-sizing between the original size and MinNameScale of it keeps a long name on one line.
    private static void FitName(Transform elementRoot)
    {
        var nameRoot = elementRoot.Find("res_name");
        var name = nameRoot == null ? null : nameRoot.GetComponent<TMP_Text>();

        if (name == null || _fittedNames.ContainsKey(name))
        {
            return;
        }

        _fittedNames[name] = new NameLayout(name);

        if (!name.enableAutoSizing)
        {
            name.fontSizeMax = name.fontSize;
            name.fontSizeMin = name.fontSize * MinNameScale;
            name.enableAutoSizing = true;
        }

        name.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void RestoreName(Transform elementRoot)
    {
        var nameRoot = elementRoot == null ? null : elementRoot.Find("res_name");
        var name = nameRoot == null ? null : nameRoot.GetComponent<TMP_Text>();

        if (name != null && _fittedNames.TryGetValue(name, out var layout))
        {
            _fittedNames.Remove(name);
            layout.Restore(name);
        }
    }

    private static void PlaceAmount(Transform elementRoot, Transform labelRoot, bool raised)
    {
        var amountRoot = elementRoot.Find("res_amount");

        if (amountRoot is RectTransform amountRect && labelRoot is RectTransform labelRect)
        {
            amountRect.anchoredPosition = raised
                ? labelRect.anchoredPosition + new Vector2(0f, amountRect.rect.height)
                : labelRect.anchoredPosition;
        }
    }

    private sealed class NameLayout
    {
        private readonly TextWrappingModes _wrapping;
        private readonly bool _autoSizing;
        private readonly float _fontSize;
        private readonly float _fontSizeMin;
        private readonly float _fontSizeMax;

        public NameLayout(TMP_Text name)
        {
            _wrapping = name.textWrappingMode;
            _autoSizing = name.enableAutoSizing;
            _fontSize = name.fontSize;
            _fontSizeMin = name.fontSizeMin;
            _fontSizeMax = name.fontSizeMax;
        }

        public void Restore(TMP_Text name)
        {
            name.enableAutoSizing = _autoSizing;
            name.fontSizeMin = _fontSizeMin;
            name.fontSizeMax = _fontSizeMax;
            name.textWrappingMode = _wrapping;

            if (!_autoSizing)
            {
                name.fontSize = _fontSize;
            }
        }
    }
}