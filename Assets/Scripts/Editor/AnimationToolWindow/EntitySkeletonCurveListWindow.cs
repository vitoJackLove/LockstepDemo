using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class EntitySkeletonCurveListWindow
{
    public List<SkeletonWorldSpaceCurveData> SkeletonDataList = new();

    public EntitySkeletonCurveListWindow()
    {
        List<string> dataPathList = FindAllAssetFiles($"Assets/{SkeletonToolWindow.SkeletonCurvePath}");

        for (int i = 0; i < dataPathList.Count; i++)
        {
            SkeletonWorldSpaceCurveData curveData = InitAssets<SkeletonWorldSpaceCurveData>(dataPathList[i]);

            SkeletonDataList.Add(curveData);
        }
    }
    
    private List<string> FindAllAssetFiles(string path)
    {
        List<string> assetFiles = new List<string>();
        
        try
        {
            // 检查路径是否存在
            if (!Directory.Exists(path))
            {
                Debug.LogWarning($"路径不存在: {path}");
                return assetFiles;
            }
            
            // 递归搜索所有.asset文件
            SearchAssetFiles(path, assetFiles);
            
            Debug.Log($"找到 {assetFiles.Count} 个.asset文件");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"搜索文件时出错: {e.Message}");
        }
        
        return assetFiles;
    }
    
    private void SearchAssetFiles(string directoryPath, List<string> resultList)
    {
        // 搜索当前目录下的.asset文件
        string[] files = Directory.GetFiles(directoryPath, "*.asset");
        resultList.AddRange(files);
        
        // 递归搜索子目录
        string[] subDirectories = Directory.GetDirectories(directoryPath);
        foreach (string subDir in subDirectories)
        {
            SearchAssetFiles(subDir, resultList);
        }
    }
    
    /// <summary>
    /// 初始化本地配置
    /// </summary>
    private T InitAssets<T>(string assetPath) where T : ScriptableObject
    {
        var scriptableObject = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        return scriptableObject;
    }
}
