using System;

namespace UnityEngine;

public class Object
{
    public string name { get; set; }

    public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) => [];

    public static T Instantiate<T>(T original, Transform parent)
        where T : Object
    {
        if (original is not GameObject source)
        {
            throw new NotSupportedException("The stub only instantiates game objects.");
        }

        var clone = new GameObject(source.name + "(Clone)");
        clone.AttachTransform(source.transform.CloneFor(clone));
        parent?.AddExistingChild(clone.transform);

        return (T)(Object)clone;
    }
}