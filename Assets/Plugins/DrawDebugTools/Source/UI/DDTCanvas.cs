using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DDTCanvas : MonoBehaviour {

    // Log
	public Transform m_LogTextesParent;    
    public GameObject m_LogTextePrefab;
    public List<Text> m_LogTextesList;

    // Float graph
    public Transform m_FloatGraphParent;
    public GameObject m_FloatGraphPrefab;
    public List<DDTFloatGraph> m_FloatGraphsList;

    // Debug camera
    public GameObject m_DebugCameraInfosPrefab;
    private DDTDebugCameraInfos m_DebugCameraInfos;

    public DDTDebugCameraInfos DebugCameraInfos
    {
        get
        {
            return m_DebugCameraInfos;
        }

        set
        {
            m_DebugCameraInfos = value;
        }
    }

    void Start () {
        // Init lists
        m_LogTextesList = new List<Text>();
        m_FloatGraphsList = new List<DDTFloatGraph>();
        
        // Set sort order to higher value so the canvas is always drawn on top
        GetComponent<Canvas>().sortingOrder = 1000;
    }

    public void UpdateLogTextes(List<DebugLogMessage> LogMessagesList)
    {
        int DiffNum = Mathf.Abs(m_LogTextesList.Count - LogMessagesList.Count);

        // Clean log text container
        if (DiffNum != 0)
        {
            if (m_LogTextesList.Count > LogMessagesList.Count)
            {
                for (int i = m_LogTextesList.Count - 1; i >= m_LogTextesList.Count - DiffNum; i--)
                {
                    if (i < m_LogTextesList.Count)
                    {
                        GameObject TextObj = m_LogTextesList[i].gameObject;
                        Destroy(TextObj);
                    }
                }
                m_LogTextesList.RemoveRange(m_LogTextesList.Count - DiffNum, DiffNum);
            }
            else
            {
                for (int i = 0; i < DiffNum; i++)
                {
                    m_LogTextesList.Add(InstantiateLogText());
                }
            }
        }

        // Update text and color
        for (int i = 0; i < LogMessagesList.Count; i++)
        {
            m_LogTextesList[i].text = LogMessagesList[i].m_LogMessageText;
            m_LogTextesList[i].color = LogMessagesList[i].m_Color;
        }
    }

    public void UpdateGraphFloats(List<DebugFloatGraph> FloatGraphsList)
    {
        int DiffNum = Mathf.Abs(m_FloatGraphsList.Count - FloatGraphsList.Count);

        // Clean log text container
        if (DiffNum != 0)
        {
            if (m_FloatGraphsList.Count > FloatGraphsList.Count)
            {
                for (int i = m_FloatGraphsList.Count - 1; i >= m_FloatGraphsList.Count - DiffNum; i--)
                {
                    if (i < m_FloatGraphsList.Count)
                    {
                        GameObject TextObj = m_FloatGraphsList[i].gameObject;
                        Destroy(TextObj);
                    }
                }
                m_FloatGraphsList.RemoveRange(m_FloatGraphsList.Count - DiffNum, DiffNum);
            }
            else
            {
                for (int i = 0; i < DiffNum; i++)
                {
                    m_FloatGraphsList.Add(InstantiateFloatGrpah());
                }
            }
        }

        // Update text and color
        for (int i = 0; i < FloatGraphsList.Count; i++)
        {
            if (FloatGraphsList[i].m_FloatValuesList.Count == 0)
                break;
            m_FloatGraphsList[i].m_GraphName.text = FloatGraphsList[i].m_UniqueFloatName;
            m_FloatGraphsList[i].m_Value.text = FloatGraphsList[i].m_FloatValuesList[FloatGraphsList[i].m_FloatValuesList.Count - 1].ToString();
            m_FloatGraphsList[i].m_MinValue.text = FloatGraphsList[i].m_MinValue.ToString();
            m_FloatGraphsList[i].m_MaxValue.text = FloatGraphsList[i].m_MaxValue.ToString();
            m_FloatGraphsList[i].UpdateGraphPoints(FloatGraphsList[i].m_FloatValuesList, FloatGraphsList[i].m_GraphValueLengh);
        }
    }

    public void UpdateDebugCamera(RaycastHit HitInfos)
    {
        if (m_DebugCameraInfos == null)
            m_DebugCameraInfos = GameObject.Instantiate(m_DebugCameraInfosPrefab, transform).GetComponent<DDTDebugCameraInfos>();
        else if (!m_DebugCameraInfos.gameObject.activeInHierarchy)
            m_DebugCameraInfos.gameObject.SetActive(true);

        m_DebugCameraInfos.UpdateInfos(HitInfos);
    }

    private Text InstantiateLogText()
    {
        GameObject LogText = GameObject.Instantiate(m_LogTextePrefab);
        LogText.transform.SetParent(m_LogTextesParent);
        LogText.transform.SetAsFirstSibling();
        LogText.name = "LogText-" + LogText.GetInstanceID();
        return LogText.GetComponent<Text>();
    }

    private DDTFloatGraph InstantiateFloatGrpah()
    {
        GameObject LogText = GameObject.Instantiate(m_FloatGraphPrefab);
        LogText.transform.SetParent(m_FloatGraphParent);
        LogText.name = "FloatGraph-" + LogText.GetInstanceID();
        return LogText.GetComponent<DDTFloatGraph>();
    }
}