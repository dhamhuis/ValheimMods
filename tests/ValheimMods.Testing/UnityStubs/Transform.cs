using System.Collections.Generic;

namespace UnityEngine;

public class Transform : Component
{
    private List<Transform> _children = new();

    public Vector3 position;

    public Transform parent { get; private set; }

    public Transform Find(string name) => _children.Find(child => child.gameObject.name == name);

    public Transform AddChild(string name, params Component[] components) => AddChild(new Transform(), name, components);

    public Transform AddChild(Transform child, string name, params Component[] components)
    {
        child.AttachedComponents.AddRange(components);
        new GameObject(name).AttachTransform(child);
        AddExistingChild(child);

        return child;
    }

    internal void AddExistingChild(Transform child)
    {
        child.parent = this;
        _children.Add(child);
    }

    internal Transform CloneFor(GameObject owner)
    {
        var clone = (Transform)CloneComponent();
        clone._children = new List<Transform>();
        clone.parent = null;
        clone.gameObject = owner;

        foreach (var component in AttachedComponents)
        {
            clone.AttachedComponents.Add(component.CloneComponent());
        }

        return clone;
    }
}