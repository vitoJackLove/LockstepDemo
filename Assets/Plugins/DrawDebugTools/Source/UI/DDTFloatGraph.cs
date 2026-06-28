using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DDTFloatGraph : MonoBehaviour 
{
	public GameObject m_LinePointPrefab;
	public Text m_GraphName;
	public Text m_Value;
	public Text m_MinValue;
	public Text m_MaxValue;
	public Transform m_LinePointsParent;
	public List<RectTransform> m_LinePointsList;

	private float m_Width;
	private float m_Height;
	private int m_SamplesCount;
	private float m_ValuePosX;

	IEnumerator Start () 
	{
		yield return new WaitForEndOfFrame();
		m_Width = transform.GetComponent<RectTransform>().sizeDelta.x;
		m_Height = transform.GetComponent<RectTransform>().sizeDelta.y;
		m_SamplesCount = DrawDebugTools.Instance.GetFloatGraphSamplesCount();

		m_ValuePosX = m_Value.GetComponent<RectTransform>().anchoredPosition.x;

		for (int i = 0; i < m_SamplesCount; i++)
        {
			GameObject LinePointObj = GameObject.Instantiate(m_LinePointPrefab);
			LinePointObj.transform.SetParent(m_LinePointsParent);
			LinePointObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.0f, 0.5f);
			LinePointObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.0f, 0.5f);
			LinePointObj.GetComponent<RectTransform>().anchoredPosition = new Vector2((m_Width / m_SamplesCount) * i, 0.0f);
			m_LinePointsList.Add(LinePointObj.GetComponent<RectTransform>());
		}
	}

    internal void UpdateGraphPoints(List<float> m_FloatValuesList, float ValueLenght)
    {
        for (int i = 0; i < m_FloatValuesList.Count; i++)
        {
			float HalfHeight = m_Height / 2.0f;
			m_LinePointsList[m_LinePointsList.Count - (1 + i)].anchoredPosition = new Vector2(m_LinePointsList[m_LinePointsList.Count - (1 + i)].anchoredPosition.x, m_FloatValuesList[i] * 50.0f / ValueLenght);
        }
		m_Value.GetComponent<RectTransform>().anchoredPosition = new Vector2(m_ValuePosX, m_LinePointsList[m_LinePointsList.Count - 1].anchoredPosition.y);

	}
}
