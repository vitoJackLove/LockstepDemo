using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能Line 资源工厂
/// </summary>
public class SkillTimeLineAssetsFactory : MonoBehaviour
{
    #region 单例

    /// <summary>
    /// 单例
    /// </summary>
    private static SkillTimeLineAssetsFactory _current;

    public static SkillTimeLineAssetsFactory Current
    {
        get
        {
            if (_current == null )
            {
                _current = FindObjectOfType<SkillTimeLineAssetsFactory>();
                    
                if (_current == null)
                {
                    _current = new GameObject("SkillTimeLineAssetsFactory").AddComponent<SkillTimeLineAssetsFactory>();
                }
            }

            return _current;
        }
    }

    #endregion

    /// <summary>
    /// 技能TimeLine资源缓存
    /// </summary>
    private Dictionary<string, Queue<SkillLineAsset>> _skillTimeLineAssetDic =
        new Dictionary<string, Queue<SkillLineAsset>>();

    /// <summary>
    /// 检查Graph是否初始化
    /// </summary>
    public int CheckGraphInitialized(SkillLineAsset originalLine, int number)
    {
        if (originalLine == null)
        {
            Debug.LogError($"初始化行为树失败：行为树文件为空!");

            return -1;
        }

        if (_skillTimeLineAssetDic.TryGetValue(originalLine.name, out var data))
        {
            if (data.Count >= number)
            {
                return 0;
            }
            else
            {
                return number - data.Count;
            }
        }

        return number;
    }

    public SkillLineAsset GetInstance(SkillLineAsset skillLineAsset)
    {
        if (skillLineAsset == null)
        {
            return null;
        }

        if (!_skillTimeLineAssetDic.TryGetValue(skillLineAsset.name, out var instance))
        {
            instance = new Queue<SkillLineAsset>();

            var newSkillLine = SkillLineAsset.Clone(skillLineAsset);

            instance.Enqueue(newSkillLine);

            _skillTimeLineAssetDic[skillLineAsset.name] = instance;
        }

        if (instance.Count == 0)
        {
            var newSkillLine = SkillLineAsset.Clone(skillLineAsset);

            instance.Enqueue(newSkillLine);

            _skillTimeLineAssetDic[skillLineAsset.name] = instance;
        }

        var copyGraph = instance.Dequeue();

        return copyGraph;
    }

    /// <summary>
    /// 设置单例
    /// </summary>
    /// <param name="lineName"></param>
    /// <param name="lineClone"></param>
    public void SetInstance(string lineName, SkillLineAsset lineClone)
    {
        if (_skillTimeLineAssetDic.ContainsKey(lineClone.name))
        {
            _skillTimeLineAssetDic[lineName].Enqueue(lineClone);
        }
        else
        {
            Queue<SkillLineAsset> copyLine = new Queue<SkillLineAsset>();

            copyLine.Enqueue(lineClone);

            _skillTimeLineAssetDic.Add(lineName, copyLine);
        }
    }

    /// <summary>
    /// 回收树
    /// </summary>
    public void RecycleTree(SkillLineAsset lineClone)
    {
        if (_skillTimeLineAssetDic.TryGetValue(lineClone.name, out var instance))
        {
            instance.Enqueue(lineClone);
        }
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        foreach (var instanceGraph in _skillTimeLineAssetDic.Values)
        {
            foreach (var graph in instanceGraph)
            {
                DestroyImmediate(graph);
            }
        }
        
        _skillTimeLineAssetDic.Clear();

        GC.Collect();
    }
}