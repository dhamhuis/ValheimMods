using System;

namespace UnityEngine;

public class GameObject : Object
{
    private Transform _transform;

    public GameObject()
    {
    }

    public GameObject(string name)
    {
        this.name = name;
    }

    public bool activeSelf { get; set; }

    public Transform transform => _transform ??= new Transform { gameObject = this };

    public Component[] ParentComponents { get; set; } = Array.Empty<Component>();

    public T GetComponent<T>()
        where T : Component => transform.GetComponent<T>();

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

    internal void AttachTransform(Transform transform)
    {
        _transform = transform;
        transform.gameObject = this;
    }
}