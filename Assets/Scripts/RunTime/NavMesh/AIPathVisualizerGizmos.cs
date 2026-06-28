using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

namespace Ase
{
    using UnityEngine;

    public class AIPathVisualizerGizmos
    {
        private PathInterpolator interpolator;
        private float m_SphereRadius = 0.2f;
        private Color m_PathLineColor = Color.green;
        private Color m_PathPointColor = Color.cyan;

        private List<fp3> pathBuffer = new List<fp3>();

        private fp3 offset = new fp3(0, (fp)0.2f, 0);

        public AIPathVisualizerGizmos(PathInterpolator path)
        {
            #if UNITY_EDITOR
            if (DrawDebugTools.Instance == null)
            {
                return;
            }

            m_SphereRadius = DrawDebugTools.Instance.m_DDTSettings.m_SphereRadius;
            m_PathLineColor = DrawDebugTools.Instance.m_DDTSettings.m_PathLineColor;
            m_PathPointColor = DrawDebugTools.Instance.m_DDTSettings.m_PathPointColor;

            this.interpolator = path;
            #endif
        }

        /// <summary>
        /// 绘制路径
        /// </summary>
        public void DrawGizmos()
        {
            if (DrawDebugTools.Instance == null)
            {
                return;
            }

            if (!DrawDebugTools.Instance.m_DDTSettings.m_EnableAgentPathVisualization)
                return;

            if (this.interpolator == null || !this.interpolator.valid)
            {
                return;
            }

            if (this.interpolator.remainingDistance > 0)
            {
                this.pathBuffer.Clear();

                this.interpolator.GetRemainingPath(pathBuffer);

                for (int i = 0; i < pathBuffer.Count; i++)
                {
                    fp3 cornerPos = pathBuffer[i] + offset;

                    DrawDebugTools.DrawSphere(cornerPos.ToVector3(), m_SphereRadius, 4, m_PathPointColor, 0.0f);

                    if (i < pathBuffer.Count - 1)
                    {
                        fp3 nextCornerPos = pathBuffer[i + 1] + offset;
                        DrawDebugTools.DrawLine(cornerPos.ToVector3(), nextCornerPos.ToVector3(), m_PathLineColor, 0.0f);
                    }
                }
            }

            DrawDebugTools.DrawString3D(this.interpolator.endPoint.ToVector3() + Vector3.up * 1, $"{interpolator.remainingDistance:F2}", TextAnchor.MiddleCenter, Color.white);
        }
    }
}
