using System;

namespace UnityEngine;

public struct Vector3(float x, float y, float z)
{
    public float x = x, y = y, z = z;

    public float sqrMagnitude => (x * x) + (y * y) + (z * z);

    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);

    public static bool operator ==(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

    public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);

    public override bool Equals(object obj) => obj is Vector3 value && this == value;

    public override int GetHashCode() => HashCode.Combine(x, y, z);
}