using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

//创建对应资产
public static class SkillTimelineFactory
{
    public static SkillLineAsset LoadTimeLineAsset(string path)
    {
        return AssetDatabase.LoadAssetAtPath<SkillLineAsset>(path);
    }

    public static void CreatTrack(SkillLineAsset asset, SkillTimelineEditorTick editorTick, string path,
        int trackIndex, Type type, bool autoSave = true)
    {
        var track = Activator.CreateInstance(type) as StandardTrack;
        asset.tracks.Insert(trackIndex, track);
        editorTick.AddTrack(trackIndex);
        SaveIfAutoSave(asset, path, autoSave);
    }

    public static void RemoveTrack(SkillLineAsset asset, SkillTimelineEditorTick editorTick, string path,
        int trackIndex, bool autoSave = true)
    {
        for (int i = asset.tracks[trackIndex].taskClips.Count - 1; i >= 0; i--)
        {
            RemoveClip(asset, editorTick, path, trackIndex, i, false);
        }

        asset.tracks.RemoveAt(trackIndex);
        editorTick.RemoveTrack(trackIndex);
        SaveIfAutoSave(asset, path, autoSave);
    }

    public static void CreateClip(SkillLineAsset asset, SkillTimelineEditorTick editorTick, string path,
        int trackIndex, int startIndex, Type clipType, bool autoSave = true)
    {
        var clip = ScriptableObject.CreateInstance(clipType) as TaskClip;

        if (clip == null)
        {
            return;
        }
        
        clip.taskStartID = startIndex;
        clip.taskDuration = 100;
        clip.taskName = clipType.GetCustomAttribute<ClipNameAttribute>().ClipName;
        AddClip(asset, editorTick, path, trackIndex, startIndex, clip, autoSave);
    }

    public static void AddClip(SkillLineAsset asset, SkillTimelineEditorTick editorTick, string path, int trackIndex,
        int startIndex, TaskClip clip, bool autoSave = true)
    {
        if (clip == null) return;
        //这里必须先得到length
        clip.taskStartID = startIndex;
        //找到左边边第一个小于他的数  找到右边第一个大于他的数 
        int left = -1;
        if (asset.tracks[trackIndex].taskClips.Count >= 1)
        {
            int leftStartID = Int32.MinValue;

            int rightStartID = Int32.MaxValue;
            int right = -1;

            for (int i = 0; i < asset.tracks[trackIndex].taskClips.Count; i++)
            {
                if (asset.tracks[trackIndex].taskClips[i].taskStartID <
                    clip.taskStartID)
                {
                    if (asset.tracks[trackIndex].taskClips[i].taskStartID > leftStartID)
                    {
                        leftStartID = asset.tracks[trackIndex].taskClips[i].taskStartID;
                        left = i;
                    }
                }

                if (asset.tracks[trackIndex].taskClips[i].taskStartID >
                    clip.taskStartID)
                {
                    if (asset.tracks[trackIndex].taskClips[i].taskStartID < rightStartID)
                    {
                        rightStartID = asset.tracks[trackIndex].taskClips[i].taskStartID;
                        right = i;
                    }
                }
            }

            if (left != -1 &&
                clip.taskStartID <
                asset.tracks[trackIndex].taskClips[left].taskStartID + asset.tracks[trackIndex].taskClips[left].taskDuration)
            {
                return;
            }

            if (right != -1 &&
                clip.taskStartID + clip.taskDuration >
                asset.tracks[trackIndex].taskClips[right].taskStartID)
            {
                clip.taskDuration = asset.tracks[trackIndex].taskClips[right].taskStartID -
                                clip.taskStartID;
            }
        }

        //附加到父资产上
        AssetDatabase.AddObjectToAsset(clip, asset);
        asset.tracks[trackIndex].taskClips.Add(clip);
        int clipIndex = left == -1 ? 0 : left + 1;
        // Debug.Log(clipIndex);
        editorTick.AddBehaviour(trackIndex, clipIndex);
        SaveIfAutoSave(asset, path, autoSave);
    }

    public static TaskClip CopyClip(SkillLineAsset asset, int trackIndex, int clipIndex)
    {
        TaskClip newClip = Object.Instantiate(asset.tracks[trackIndex].taskClips[clipIndex]);
        newClip.name = asset.tracks[trackIndex].taskClips[clipIndex].name;
        return newClip;
    }

    public static void RemoveClip(SkillLineAsset asset, SkillTimelineEditorTick editorTick, string path,
        int trackIndex, int clipIndex, bool autoSave = true)
    {
        // 从父资产中删除
        AssetDatabase.RemoveObjectFromAsset(asset.tracks[trackIndex].taskClips[clipIndex]);
        asset.tracks[trackIndex].taskClips.RemoveAt(clipIndex);
        editorTick.RemoveBehaviour(trackIndex, clipIndex);
        SaveIfAutoSave(asset, path, autoSave);
    }

    public static void ResizeClip(SkillLineAsset asset, int trackIndex, int clipIndex, int target)
    {
        // Debug.Log(trackIndex+" " +clipIndex);
        if (target < 1) target = 1;
        //找到右边第一个大于他的数
        if (asset.tracks[trackIndex].taskClips.Count > 1 && target > 1)
        {
            int rightStartID = Int32.MaxValue;
            int right = -1;

            for (int i = 0; i < asset.tracks[trackIndex].taskClips.Count; i++)
            {
                if (asset.tracks[trackIndex].taskClips[i].taskStartID >
                    asset.tracks[trackIndex].taskClips[clipIndex].taskStartID)
                {
                    if (asset.tracks[trackIndex].taskClips[i].taskStartID < rightStartID)
                    {
                        rightStartID = asset.tracks[trackIndex].taskClips[i].taskStartID;
                        right = i;
                    }
                }
            }

            if (right != -1 &&
                asset.tracks[trackIndex].taskClips[clipIndex].taskStartID + target >
                asset.tracks[trackIndex].taskClips[right].taskStartID)
            {
                target = asset.tracks[trackIndex].taskClips[right].taskStartID -
                         asset.tracks[trackIndex].taskClips[clipIndex].taskStartID;
            }
        }

        asset.tracks[trackIndex].taskClips[clipIndex].taskDuration = target;
    }

    public static void MoveClip(SkillLineAsset asset, string path, int trackIndex, int clipIndex, int target)
    {
        if (target <= 0) target = 0;
        //找到左边第一个 小于他的数
        if (asset.tracks[trackIndex].taskClips.Count > 1)
        {
            int leftStartID = Int32.MinValue;
            int left = -1;

            int rightStartID = Int32.MaxValue;
            int right = -1;

            for (int i = 0; i < asset.tracks[trackIndex].taskClips.Count; i++)
            {
                if (asset.tracks[trackIndex].taskClips[i].taskStartID <
                    asset.tracks[trackIndex].taskClips[clipIndex].taskStartID)
                {
                    if (asset.tracks[trackIndex].taskClips[i].taskStartID > leftStartID)
                    {
                        left = i;
                        leftStartID = asset.tracks[trackIndex].taskClips[i].taskStartID;
                    }
                }

                if (asset.tracks[trackIndex].taskClips[i].taskStartID >
                    asset.tracks[trackIndex].taskClips[clipIndex].taskStartID)
                {
                    if (asset.tracks[trackIndex].taskClips[i].taskStartID < rightStartID)
                    {
                        right = i;
                        rightStartID = asset.tracks[trackIndex].taskClips[i].taskStartID;
                    }
                }
            }

            // Debug.Log("left " + leftStartID + " right " + rightStartID + "target" + target);

            if (left != -1 && target < leftStartID + asset.tracks[trackIndex].taskClips[left].taskDuration)
            {
                asset.tracks[trackIndex].taskClips[clipIndex].taskStartID =
                    leftStartID + asset.tracks[trackIndex].taskClips[left].taskDuration;
                return;
            }

            if (right != -1 && target + asset.tracks[trackIndex].taskClips[clipIndex].taskDuration > rightStartID)
            {
                asset.tracks[trackIndex].taskClips[clipIndex].taskStartID =
                    rightStartID - asset.tracks[trackIndex].taskClips[clipIndex].taskDuration;
                return;
            }
        }

        asset.tracks[trackIndex].taskClips[clipIndex].taskStartID = target;
    }

    public static void Save(SkillLineAsset asset, string path)
    {
        //遍历asset的轨道
        //结尾最远的Clip
        int maxFar = 0;
        for (int i = 0; i < asset.tracks.Count; i++)
        {
            var track = asset.tracks[i];
            if (track.taskClips.Count == 0) continue;
            //为clip按照startID排序
            track.taskClips.Sort((TaskClip x, TaskClip y) =>
            {
                if (x.taskStartID > y.taskStartID) return 1;
                else return -1;
            });
            maxFar = Mathf.Max(maxFar, track.taskClips.Last().taskStartID + track.taskClips.Last().taskDuration);
        }

        //时间轴的总体时长
        asset.duration = maxFar;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    private static void SaveIfAutoSave(SkillLineAsset asset, string path, bool autoSave)
    {
        if (!autoSave)
        {
            return;
        }

        Save(asset, path);
    }

    /// <summary>
    /// 添加以哦个
    /// </summary>
    /// <param name="skillBlackVariable"></param>
    /// <param name="asset"></param>
    public static void AddVariable(SkillBlackVariable skillBlackVariable,SkillLineAsset asset, bool autoSave = true)
    {
        //AssetDatabase.AddObjectToAsset(skillBlackVariable, asset);
        asset.blackBoardVariable.AddVariable(skillBlackVariable);
        SaveIfAutoSave(asset, AssetDatabase.GetAssetPath(asset), autoSave);
    }

    /// <summary>
    /// 移除一个变量
    /// </summary>
    /// <param name="indexVariable"></param>
    /// <param name="skillBlackVariable"></param>
    /// <param name="asset"></param>
    public static void RemoveVariable(int indexVariable,SkillBlackVariable skillBlackVariable, SkillLineAsset asset,
        bool autoSave = true)
    {
        //AssetDatabase.RemoveObjectFromAsset(asset.blackBoardVariable.Variables[indexVariable]);
        asset.blackBoardVariable.RemoveVariable(skillBlackVariable);
        SaveIfAutoSave(asset, AssetDatabase.GetAssetPath(asset), autoSave);
    }
}
