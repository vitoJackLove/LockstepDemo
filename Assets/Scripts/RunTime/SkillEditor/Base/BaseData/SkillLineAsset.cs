using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] 
public class SkillLineAsset : ScriptableObject
{
    [SerializeReference] public List<StandardTrack> tracks;
    [SerializeReference] public BlackBoardVariable blackBoardVariable;
    
    /// <summary>
    /// 时长 （帧）
    /// </summary>
    public int duration;

    public int fps;

    public SkillLineAsset()
    {
        tracks = new List<StandardTrack>();
        blackBoardVariable = new BlackBoardVariable();
    }
    
    /// <summary>
    /// 克隆一个对象
    /// </summary>
    /// <param name="skillLineAsset"></param>
    /// <returns></returns>
    public static SkillLineAsset Clone(SkillLineAsset skillLineAsset)
    {
        if (skillLineAsset == null)
        {
            return null;
        }   
        
        SkillLineAsset skillAssetsClone = ScriptableObject.Instantiate(skillLineAsset);

        skillAssetsClone.name = skillLineAsset.name;
        skillAssetsClone.tracks = new List<StandardTrack>();

        if (skillLineAsset.tracks == null)
        {
            Debug.LogError($"SkillLineAsset '{skillLineAsset.name}' tracks is null.");
            return skillAssetsClone;
        }

        for (int i = 0; i < skillLineAsset.tracks.Count; i++)
        {
            StandardTrack standardTrack = skillLineAsset.tracks[i];
            if (standardTrack == null)
            {
                Debug.LogError(
                    $"SkillLineAsset '{skillLineAsset.name}' track index {i} is null. Check SerializeReference assembly (expected Game.Runtime).");
                continue;
            }

            StandardTrack standardTrackClone = (StandardTrack)Activator.CreateInstance(standardTrack.GetType());
            
            skillAssetsClone.tracks.Add(standardTrackClone);

            if (standardTrack.taskClips == null)
            {
                standardTrackClone.taskClips = new List<TaskClip>();
                continue;
            }
            
            for (int j = 0; j < standardTrack.taskClips.Count; j++)
            {
                TaskClip clip = standardTrack.taskClips[j];
                if (clip == null)
                {
                    Debug.LogError($"SkillLineAsset '{skillLineAsset.name}' track {i} clip {j} is null.");
                    continue;
                }

                TaskClip clipClone = ScriptableObject.Instantiate<TaskClip>(clip);
                
                skillAssetsClone.tracks[i].taskClips.Add(clipClone);
            }
        }
        
        return skillAssetsClone;
    }
}