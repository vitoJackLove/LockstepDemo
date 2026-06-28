using System.Collections.Generic;
using UnityEngine;

public class TrackStyleData
{
    public string Name;
    public Color Color;
    public List<ClipStyleData> Clips;
    public ClipUIAction UpdateUI;
    public bool OverrideUI;
}