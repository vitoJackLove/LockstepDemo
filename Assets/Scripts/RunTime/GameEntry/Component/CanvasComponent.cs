using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 画布组件 预制一些画布
/// </summary>
public class CanvasComponent : RunTimeComponent
{
    [Serializable]
    public sealed class CanvasGroup
    {
        [SerializeField] private string m_Name = null;

        [SerializeField] private string m_Desc;
            
        [SerializeField] private Transform m_Root = null;
            
        [SerializeField] private Canvas m_Canvas = null;

        public string Name => m_Name;

        public Transform Root => m_Root;
            
        public Canvas Canvas => m_Canvas;
    }
    
    [SerializeField] private CanvasGroup[] m_CanvasGroups = null;

    private Dictionary<string, CanvasGroup> m_CacheCanvasGroups;

    /// <summary>
    /// 获取画布数量。
    /// </summary>
    public int CanvasGroupCount => m_CacheCanvasGroups.Count;

    public override void Init()
    {
        m_CacheCanvasGroups = new Dictionary<string, CanvasGroup>();

        for (int i = 0; i < m_CanvasGroups.Length; i++)
        {
            if (this.HasSoundGroup(m_CanvasGroups[i].Name))
            {
                continue;
            }

            this.m_CacheCanvasGroups.Add(m_CanvasGroups[i].Name, m_CanvasGroups[i]);
        }
    }

    /// <summary>
    /// 是否存在指定画布组。
    /// </summary>
    /// <param name="canvasGroupName">画布名称。</param>
    public bool HasSoundGroup(string canvasGroupName)
    {
        if (string.IsNullOrEmpty(canvasGroupName))
        {
            GameLog.Error(GameLogChannel.UI, "Canvas group name is invalid.");
        }

        return m_CacheCanvasGroups.ContainsKey(canvasGroupName);
    }

    /// <summary>
    /// 获取指定画布组。
    /// </summary>
    /// <param name="canvasGroupName">声音组名称。</param>
    public CanvasGroup GetCanvasGroup(string canvasGroupName)
    {
        if (string.IsNullOrEmpty(canvasGroupName))
        {
            GameLog.Error(GameLogChannel.UI, "Canvas group name is invalid.");
        }

        if (m_CacheCanvasGroups.TryGetValue(canvasGroupName, out var canvasGroup))
        {
            return canvasGroup;
        }

        return null;
    }

    public override void Shutdown()
    {
        this.m_CacheCanvasGroups.Clear();
        this.m_CacheCanvasGroups = null;
    }
}
