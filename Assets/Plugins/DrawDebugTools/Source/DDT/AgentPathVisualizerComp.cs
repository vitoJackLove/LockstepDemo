// Programmed by Mourad Bakhali mourad.bakhali@gmail.com
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentPathVisualizerComp : MonoBehaviour
{
	private NavMeshAgent m_NavMeshAgent;
	private float m_SphereRadius = 0.1f;
	private Color m_PathLineColor = Color.green;
	private Color m_PathPointColor = Color.cyan;

	private void Start()
	{
		m_NavMeshAgent = GetComponent<NavMeshAgent>();
		m_SphereRadius = DrawDebugTools.Instance.m_DDTSettings.m_SphereRadius;
		m_PathLineColor = DrawDebugTools.Instance.m_DDTSettings.m_PathLineColor;
		m_PathPointColor = DrawDebugTools.Instance.m_DDTSettings.m_PathPointColor;
	}

	private void Update()
	{
		// Leave if agent path visualization is disabled
		if(!DrawDebugTools.Instance.m_DDTSettings.m_EnableAgentPathVisualization)
			return;
		
		// Visualize agent path if he has one
		if (m_NavMeshAgent != null && m_NavMeshAgent.hasPath)
		{
			Vector3 YOffset = new Vector3(0.0f, 0.2f, 0.0f);
			
			for (int i = 0; i < m_NavMeshAgent.path.corners.Length; i++)
			{
				Vector3 CornerPos = m_NavMeshAgent.path.corners[i] + YOffset;
                
				DrawDebugTools.DrawSphere(CornerPos, m_SphereRadius, 4, m_PathPointColor, 0.0f);

				if (i < m_NavMeshAgent.path.corners.Length - 1)
				{
					Vector3 NextCornerPos = m_NavMeshAgent.path.corners[i + 1] + YOffset;
					DrawDebugTools.DrawLine(CornerPos, NextCornerPos, m_PathLineColor, 0.0f);
				}
			}

			// Display remaining distance
			if (m_NavMeshAgent.remainingDistance != Mathf.Infinity)
			{
				Vector3 LastPointPosition = m_NavMeshAgent.path.corners[m_NavMeshAgent.path.corners.Length - 1];
				DrawDebugTools.DrawString3D(LastPointPosition + Vector3.up * 1.0f, m_NavMeshAgent.remainingDistance.ToString("F1"), TextAnchor.LowerLeft, Color.white);
			}
		}
	}
}
