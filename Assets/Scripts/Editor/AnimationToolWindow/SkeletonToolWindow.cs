using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class SkeletonToolWindow
{
    public const string SkeletonCurvePath = "GameAssetConfig/EntitySkeletonCurve"; 

    [TabGroup("骨骼路径")]
    [ListDrawerSettings(ShowIndexLabels = true)] [LabelText("骨骼路径")]
    public List<string> allSkeletonPath = new List<string>();
    
    [TabGroup("骨骼路径")]
    [LabelText("动画剪辑")] public AnimationClip clip;
    
    [TabGroup("骨骼路径")]
    [LabelText("游戏物体")] public GameObject animationGo;
    
    [TabGroup("骨骼路径")]
    [LabelText("游戏物体根节点和动画根节点子父级关系")] public string rootPath;
    
    [TabGroup("骨骼路径")]
    [Button("获取该动画作用于位移旋转的骨骼节点路径")]
    public void GetSkeletonPath()
    {
        allSkeletonPath.Clear();

        // 获取所有曲线绑定
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

        foreach (var binding in bindings)
        {
            if (binding.type == typeof(Transform))
            {
                string bonePath = binding.path;

                bonePath = rootPath + bonePath;

                if (!allSkeletonPath.Contains(bonePath))
                {
                    allSkeletonPath.Add(bonePath);
                }
            }
        }
    }

    [TabGroup("制作骨骼曲线")]
    [LabelText("曲线索引")] public int SkeletonIndex;
    [TabGroup("制作骨骼曲线")]
    [LabelText("骨骼名字（用于关联曲线）")] public string SkeletonName;
    [TabGroup("制作骨骼曲线")]
    [LabelText("实体的配置ID")] public int EntityConfigId;

    [TabGroup("制作骨骼曲线")]
    [Button("根据索引生成骨骼世界坐标曲线")]

    public void GenerateSkeletonCurve()
    {
        WorldSpaceData data = ConvertBoneAnimationToWorldSpace();

        if (data == null)
        {
            return;
        }
        
        SkeletonWorldSpaceCurveData curveData = InitAssets<SkeletonWorldSpaceCurveData>
            ($"Assets/{SkeletonCurvePath}/{EntityConfigId}.asset", EntityConfigId.ToString());
        
        curveData.skeletonWorldSpaceData.Add(data);
        
        AssetDatabase.SaveAssets();
        
        AssetDatabase.Refresh();
    }
    
    private WorldSpaceData ConvertBoneAnimationToWorldSpace()
    {
        if (allSkeletonPath.Count == 0)
        {
            return null;
        }
        
        if (SkeletonIndex > allSkeletonPath.Count)
        {
            return null;
        }

        string bonePath = allSkeletonPath[SkeletonIndex];

        // 进入动画模式
        AnimationMode.StartAnimationMode();

        WorldSpaceData data = new WorldSpaceData();

        data.skeletonName = SkeletonName;

        try
        {
            Transform bone = FindBoneTransform(animationGo.transform, bonePath);
            if (bone == null) return null;

            // 采样动画
            float sampleRate = 30f;
            
            int frameCount = Mathf.CeilToInt(clip.length * sampleRate);

            for (int i = 0; i <= frameCount; i++)
            {
                float time = i / sampleRate;
                
                if (time > clip.length) time = clip.length;

                // 在动画模式下采样
                AnimationMode.SampleAnimationClip(animationGo, clip, time);

                // 获取世界坐标
                Vector3 worldPos = bone.position;

                data.AddPositionCurve(time, worldPos);
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();
        }

        return data;
    }

    private Transform FindBoneTransform(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path)) return root;

        string[] boneNames = path.Split('/');
        Transform current = root;

        foreach (string boneName in boneNames)
        {
            current = current.Find(boneName);
            if (current == null) return null;
        }

        return current;
    }
    
    /// <summary>
    /// 初始化本地配置
    /// </summary>
    private T InitAssets<T>(string assetPath, string assetName) where T : ScriptableObject
    {
        var scriptableObject = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        if (scriptableObject == null)
        {
            scriptableObject = ScriptableObject.CreateInstance<T>();

            scriptableObject.name = assetName;

            AssetDatabase.CreateAsset(scriptableObject, assetPath);
        }

        if (scriptableObject == null)
        {
            Debug.LogError($"Buff制作工具初始化错误：{assetName}未能正常生成本地数据...");
        }

        return scriptableObject;
    }
}