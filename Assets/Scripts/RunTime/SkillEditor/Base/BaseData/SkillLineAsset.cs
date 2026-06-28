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
        
        SkillLineAsset skillAssetsClone = ScriptableObject.Instantiate<SkillLineAsset>(skillLineAsset);

        skillAssetsClone.name = skillLineAsset.name;
        skillAssetsClone.tracks = new List<StandardTrack>();

        for (int i = 0; i < skillLineAsset.tracks.Count; i++)
        {
            StandardTrack standardTrack = skillLineAsset.tracks[i];

            StandardTrack standardTrackClone = (StandardTrack)Activator.CreateInstance(standardTrack.GetType());
            
            skillAssetsClone.tracks.Add(standardTrackClone);
            
            for (int j = 0; j < standardTrack.taskClips.Count; j++)
            {
                TaskClip clipClone = ScriptableObject.Instantiate<TaskClip>(standardTrack.taskClips[j]);
                
                skillAssetsClone.tracks[i].taskClips.Add(clipClone);
            }
        }
        
        return skillAssetsClone;
    }
}