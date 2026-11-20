using System;
using System.Collections.Generic;
using System.Linq;

namespace ValheimMods.Common.InputSelection;

public sealed class InputPreferences
{
    private readonly string[] _priority;
    private readonly HashSet<string> _excluded;

    private InputPreferences(string[] priority, IEnumerable<string> excluded)
    {
        _priority = priority;
        _excluded = new HashSet<string>(excluded, StringComparer.Ordinal);
    }

    public static InputPreferences Parse(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var sections = value.Split('|');

        if (sections.Length != 2)
        {
            throw new FormatException("Input preferences must contain priority|exclusions.");
        }

        return new InputPreferences(ParseList(sections[0]), ParseList(sections[1]));
    }

    public bool IsExcluded(string prefabName) => _excluded.Contains(prefabName);

    public bool IsConfigured(string prefabName) => _priority.Contains(prefabName) || _excluded.Contains(prefabName);

    public string Serialize() => string.Join(",", _priority) + "|" + string.Join(",", _excluded.OrderBy(name => name, StringComparer.Ordinal));

    public IReadOnlyList<string> Order(IEnumerable<string> accepted)
    {
        var types = new HashSet<string>(accepted, StringComparer.Ordinal);
        var ordered = _priority.Where(types.Contains).ToList();
        ordered.AddRange(types.Except(ordered).OrderBy(name => name, StringComparer.Ordinal));

        return ordered;
    }

    public string Choose(IEnumerable<string> available)
    {
        return Order(available).FirstOrDefault(name => !IsExcluded(name));
    }

    public InputPreferences ToggleExcluded(string prefabName)
    {
        ValidateName(prefabName);
        var excluded = new HashSet<string>(_excluded, StringComparer.Ordinal);

        if (!excluded.Remove(prefabName))
        {
            excluded.Add(prefabName);
        }

        return new InputPreferences(_priority, excluded);
    }

    public InputPreferences MoveFirst(string prefabName, IEnumerable<string> accepted)
    {
        ValidateName(prefabName);
        var ordered = new[] { prefabName }.Concat(_priority).Concat(Order(accepted))
            .Distinct(StringComparer.Ordinal).ToArray();

        return new InputPreferences(ordered, _excluded);
    }

    public InputPreferences MoveUp(string prefabName, IEnumerable<string> accepted)
    {
        return Move(prefabName, accepted, -1);
    }

    public InputPreferences MoveDown(string prefabName, IEnumerable<string> accepted)
    {
        return Move(prefabName, accepted, 1);
    }

    private InputPreferences Move(string prefabName, IEnumerable<string> accepted, int direction)
    {
        ValidateName(prefabName);
        var acceptedOrder = Order(accepted).ToList();
        int index = acceptedOrder.IndexOf(prefabName);

        if (index < 0)
        {
            throw new ArgumentException("The input type is not accepted by this station.", nameof(prefabName));
        }

        int targetIndex = index + direction;

        if (targetIndex < 0 || targetIndex >= acceptedOrder.Count)
        {
            return this;
        }

        string displaced = acceptedOrder[targetIndex];
        acceptedOrder[targetIndex] = acceptedOrder[index];
        acceptedOrder[index] = displaced;
        var acceptedNames = new HashSet<string>(acceptedOrder, StringComparer.Ordinal);
        var existingOrder = _priority.Concat(acceptedOrder).Distinct(StringComparer.Ordinal);
        var reordered = new List<string>();
        int acceptedIndex = 0;

        foreach (string name in existingOrder)
        {
            if (acceptedNames.Contains(name))
            {
                reordered.Add(acceptedOrder[acceptedIndex]);
                acceptedIndex++;
            }
            else
            {
                reordered.Add(name);
            }
        }

        return new InputPreferences(reordered.ToArray(), _excluded);
    }

    private static string[] ParseList(string value)
    {
        if (value.Length == 0)
        {
            return Array.Empty<string>();
        }

        var names = value.Split(',');
        var unique = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            ValidateName(name);

            if (!unique.Add(name))
            {
                throw new FormatException($"Duplicate input preference: {name}.");
            }
        }

        return names;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != name.Trim()
            || name.Any(character => char.IsControl(character) || character is ',' or '|'))
        {
            throw new FormatException("Input preference identifiers cannot be blank, padded or contain commas, pipes or control characters.");
        }
    }
}