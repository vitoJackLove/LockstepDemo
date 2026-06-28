using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/骨骼动画世界坐标曲线")]
public class SkeletonWorldSpaceCurveData : ScriptableObject
{
    /// <summary>
    /// 骨骼点世界坐标曲线
    /// </summary>
    public List<WorldSpaceData> skeletonWorldSpaceData = new ();

    public WorldSpaceData GetCurveData(string skeletonName)
    {
        for (int i = 0; i < skeletonWorldSpaceData.Count; i++)
        {
            if (skeletonWorldSpaceData[i] == null)
            {
                continue;
            }
            
            if (string.Equals(skeletonWorldSpaceData[i].skeletonName,skeletonName))
            {
                return skeletonWorldSpaceData[i];
            }
        }

        return null;
    }
}
