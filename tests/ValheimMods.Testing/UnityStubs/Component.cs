using System.Collections.Generic;

namespace UnityEngine;

public class Component : Object
{
    private GameObject _gameObject;

    public GameObject gameObject
    {
        get => _gameObject ??= CreateOwner();
        internal set => _gameObject = value;
    }

    public List<Component> AttachedComponents { get; private set; } = new();

    public T GetComponent<T>()
        where T : Component
    {
        if (this is T self)
        {
            return self;
        }

        foreach (var component in AttachedComponents)
        {
            if (component is T match)
            {
                return match;
            }
        }

        return null;
    }

    internal Component CloneComponent()
    {
        var clone = (Component)MemberwiseClone();
        clone._gameObject = null;
        clone.AttachedComponents = new List<Component>();

        return clone;
    }

    private GameObject CreateOwner()
    {
        var owner = new GameObject(name);

        if (this is Transform transform)
        {
            owner.AttachTransform(transform);
        }

        return owner;
    }
}