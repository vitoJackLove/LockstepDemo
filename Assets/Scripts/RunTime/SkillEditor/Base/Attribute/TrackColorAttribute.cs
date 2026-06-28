using UnityEngine;

/// <summary>
/// 描述轨道颜色
/// </summary>
public class TrackColorAttribute : System.Attribute
{
    public Color Color;

    public TrackColorAttribute(float r, float g, float b)
    {
        Color = new Color(r, g, b);
    }
}