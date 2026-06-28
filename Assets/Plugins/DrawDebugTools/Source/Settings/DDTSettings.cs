using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DrawDebugTools/DDTSettings")]
public class DDTSettings : ScriptableObject
{
    [Header("Debug Camera")]
    [Tooltip("When toggling debug camera should we freeze time or not")]
    public bool m_IsDebugCamFreezeTime = true;
    
    [Tooltip("Change color of the main color of the shape indicating the main camera position and rotation")]
    public Color m_MainCamShapeColor = Color.red;

    [Tooltip("Step value to use when changing time scale")]
    public float m_TimeControlStep = 0.1f;
    
    [Tooltip("Change color of the line indicating the normal vector of the surface under crosshair")]
    public Color m_DebugNormalVectColor = Color.red;

    [Header("Agent Path")]
    [Tooltip("Enable or Disable agent path visualization, affects all agents")]
    public bool m_EnableAgentPathVisualization = true;
    [Tooltip("Set the radius of the sphere that represent path points")]
    public float m_SphereRadius = 0.1f;
    [Tooltip("Set the color of the line that represent segments between path points")]
    public Color m_PathLineColor = Color.green;
    [Tooltip("Set the color of the path points")]
    public Color m_PathPointColor = Color.cyan;
    
    public DDTSettings()
    {
        m_IsDebugCamFreezeTime = true;
        m_MainCamShapeColor = Color.red;
        m_TimeControlStep = 0.1f;
        m_DebugNormalVectColor = Color.red;
    }
}
