using System.Collections;
using System.Collections.Generic;
using DDTExamples;
using UnityEngine;
using UnityEngine.UI;

public class DDTDebugCameraInfos : MonoBehaviour {
		
	[Space(10)]
	public DDTDebugValueText m_CamPosText;
	public DDTDebugValueText m_CamRotText;
	public DDTDebugValueText m_CamSpeedText;
	
	[Space(10)]
	[Header("Raycast Infos")]
	public RectTransform m_ColliderRaycastPanel;
	public DDTDebugValueText m_RaycastInfos_ValText_1;
	public DDTDebugValueText m_RaycastInfos_ValText_2;
	public DDTDebugValueText m_RaycastInfos_ValText_3;
	public DDTDebugValueText m_RaycastInfos_ValText_4;
	public DDTDebugValueText m_RaycastInfos_ValText_5;
	public DDTDebugValueText m_RaycastInfos_ValText_6;
	public DDTDebugValueText m_RaycastInfos_ValText_7;
	public DDTDebugValueText m_RaycastInfos_ValText_8;
	[Header("Mesh Materials Infos")]
	public GameObject m_MaterialListEntryPrefab;
	public RectTransform m_RaycastMeshMatsDebugInfosParent;
	public List<DDTDebugValueText> m_MaterialsListTextesList;
	
	[Space(10)]
	public Text m_DeltaTimeText;
	public Text m_TimeScaleText;

	private float m_TimeControlStep = 0.1f;
	private float m_MaxTimeControlScale = 100.0f;

	void Start () {
		m_MaterialsListTextesList = new List<DDTDebugValueText>();		
		m_ColliderRaycastPanel.gameObject.SetActive(false);
		m_RaycastMeshMatsDebugInfosParent.gameObject.SetActive(false);
		m_TimeControlStep = DrawDebugTools.Instance.m_DDTSettings.m_TimeControlStep;
	}
	
	public void UpdateInfos (RaycastHit HitInfos) {
		// Cam transform and speed
		m_CamPosText.SetText("position: " + DrawDebugTools.Instance.DebugCamera.transform.position.ToString());
		m_CamRotText.SetText("rotation: " + DrawDebugTools.Instance.DebugCamera.transform.eulerAngles.ToString());
		m_CamSpeedText.SetText("cam speed (mouse wheel): " + DrawDebugTools.Instance.DebugCamera.GetDebugCameraMovSpeedMultiplier().ToString());

		// Raycast infos
		if (HitInfos.collider != null)
		{
			if (!m_ColliderRaycastPanel.gameObject.activeInHierarchy)			
				m_ColliderRaycastPanel.gameObject.SetActive(true);
			
			m_RaycastInfos_ValText_1.SetText("ray hit point: " + HitInfos.point.ToString());
			m_RaycastInfos_ValText_2.SetText("ray hit normal: " + HitInfos.normal.ToString());
			m_RaycastInfos_ValText_3.SetText("ray hit barycentricCoordinate: " + HitInfos.barycentricCoordinate.ToString());
			m_RaycastInfos_ValText_4.SetText("ray hit distance: " + HitInfos.distance.ToString());
			m_RaycastInfos_ValText_5.SetText("ray hit triangleIndex: " + HitInfos.triangleIndex.ToString());
			m_RaycastInfos_ValText_6.SetText("ray hit textureCoord: " + HitInfos.textureCoord.ToString());
			m_RaycastInfos_ValText_7.SetText("ray hit textureCoord2: " + HitInfos.textureCoord2.ToString());
			m_RaycastInfos_ValText_8.SetText("ray hit Object name: \"" + HitInfos.transform.name + "\"");

            if (HitInfos.transform.GetComponent<MeshRenderer>() != null || HitInfos.transform.GetComponent<SkinnedMeshRenderer>() != null)
            {
                if (!m_RaycastMeshMatsDebugInfosParent.gameObject.activeInHierarchy)
                    m_RaycastMeshMatsDebugInfosParent.gameObject.SetActive(true);

                Material[] MatsArray;
                if (HitInfos.transform.GetComponent<MeshRenderer>() != null)
                    MatsArray = HitInfos.transform.GetComponent<MeshRenderer>().materials;
                else
                    MatsArray = HitInfos.transform.GetComponent<SkinnedMeshRenderer>().materials;

                int DiffNum = Mathf.Abs(m_MaterialsListTextesList.Count - MatsArray.Length);

                if (DiffNum != 0)
                {
                    if (m_MaterialsListTextesList.Count > MatsArray.Length)
                    {
                        for (int i = m_MaterialsListTextesList.Count - 1; i >= m_MaterialsListTextesList.Count - DiffNum; i--)
                        {
                            if (i < m_MaterialsListTextesList.Count)
                            {
                                GameObject TextObj = m_MaterialsListTextesList[i].gameObject;
                                Destroy(TextObj);
                            }
                        }
                        m_MaterialsListTextesList.RemoveRange(m_MaterialsListTextesList.Count - DiffNum, DiffNum);
                    }
                    else
                    {
                        for (int i = 0; i < DiffNum; i++)
                        {
                            m_MaterialsListTextesList.Add(InstantiateMeshMatListEntry());
                        }
                    }
                }

                // Display mest mats list
                for (int i = 0; i < MatsArray.Length; i++)
                {
                    m_MaterialsListTextesList[i].SetText("Mat (" + i + "): " + MatsArray[i].name);
                    m_MaterialsListTextesList[i].GetComponent<RectTransform>().SetAsLastSibling();
                }
            }
            else
            {
                if (m_RaycastMeshMatsDebugInfosParent.gameObject.activeInHierarchy)
                    m_RaycastMeshMatsDebugInfosParent.gameObject.SetActive(false);
            }
        }
		else
		{
			if (m_ColliderRaycastPanel.gameObject.activeInHierarchy)			
				m_ColliderRaycastPanel.gameObject.SetActive(false);
			if (m_RaycastMeshMatsDebugInfosParent.gameObject.activeInHierarchy)
				m_RaycastMeshMatsDebugInfosParent.gameObject.SetActive(false);
		}

		// Delta time
		m_DeltaTimeText.text = "delta time: " + Time.deltaTime;
		m_TimeScaleText.text = "time scale: " + Time.timeScale;
	}

	private DDTDebugValueText InstantiateMeshMatListEntry()
	{
		GameObject MeshMatNameText = GameObject.Instantiate(m_MaterialListEntryPrefab);
		MeshMatNameText.transform.SetParent(m_RaycastMeshMatsDebugInfosParent);
		MeshMatNameText.transform.SetAsFirstSibling();
		MeshMatNameText.name = "MeshMat_ValText_" + MeshMatNameText.GetInstanceID();
		return MeshMatNameText.GetComponent<DDTDebugValueText>();
	}

	public void PauseTime()
	{
		Time.timeScale = 0.0f;
	}

	public void ResumeTime()
	{
		Time.timeScale = 1.0f;
	}

	public void SpeedUpTime()
	{
		float NewTimeScale = Time.timeScale;
		NewTimeScale += m_TimeControlStep;
		Time.timeScale = Mathf.Clamp(NewTimeScale, 0.0f, m_MaxTimeControlScale);
	}

	public void SlowDownTime()
	{
		float NewTimeScale = Time.timeScale;
		NewTimeScale -= m_TimeControlStep;
		Time.timeScale = Mathf.Clamp(NewTimeScale, 0.0f, m_MaxTimeControlScale);
	}
}
