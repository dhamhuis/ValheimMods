namespace UnityEngine;

public struct Rect
{
    public Rect(float x, float y, float width, float height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public float x { get; set; }

    public float y { get; set; }

    public float width { get; set; }

    public float height { get; set; }
}