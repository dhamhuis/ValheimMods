using System;

namespace UnityEngine;

public class GameObject
{
    public GameObject()
    {
    }

    public GameObject(string name)
    {
        this.name = name;
    }

    public string name;

    public Component[] ParentComponents { get; set; } = Array.Empty<Component>();

    public T GetComponentInParent<T>() where T : Component
    {
        foreach (var component in ParentComponents)
        {
            if (component is T match)
            {
                return match;
            }
        }

        return null;
    }
}