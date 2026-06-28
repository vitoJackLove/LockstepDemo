using Ase.Serializing;

/// <summary>
/// 预测回滚
/// </summary>
public partial class SkillTimelineLauncher
{
    /// <summary>
    /// 快照
    /// </summary>
    /// <param name="hardWriter"></param>
    /// <param name="softWriter"></param>
    public void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriteString($"_skillTimeAssets = {_skillTimeAssets.name}");
        hardWriter.WriteInt32Data("SkillTimeAssets_stateEnum", (int)_stateEnum);
        hardWriter.WriteInt32Data("SkillTimeAssets_currentFrameID", _currentFrameID);
        
        for (var trackIndex = 0; trackIndex < _skillTimeAssets.tracks.Count; trackIndex++)
        {
            var track = _skillTimeAssets.tracks[trackIndex];
            
            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var behavior = _behaviours[trackIndex][clipIndex];

                if (behavior != null)
                {
                    behavior.TakeSnapShot(hardWriter,softWriter);
                }
            }
        }
    }

    /// <summary>
    /// 回滚
    /// </summary>
    /// <param name="authoritySnapShot"></param>
    public void RollBackTo(PooledReader authoritySnapShot)
    {
        PlayableStateEnum authorityState = (PlayableStateEnum)authoritySnapShot.ReadInt32();
        int authorityFrameID = authoritySnapShot.ReadInt32();
        _currentFrameID = authorityFrameID;
        
        for (var trackIndex = 0; trackIndex < _skillTimeAssets.tracks.Count; trackIndex++)
        {
            var track = _skillTimeAssets.tracks[trackIndex];
            
            for (var clipIndex = 0; clipIndex < track.taskClips.Count; clipIndex++)
            {
                var clip = track.taskClips[clipIndex];
                
                if (clip != null)
                {
                    //回滚到的帧号还没开始运行这个节点 但是该节点还在执行 需要执行节点的结束 把创建的东西回收掉
                    if (_currentFrameID <= clip.taskStartID || _currentFrameID > clip.taskStartID + clip.taskDuration)
                    {
                        if (clip.State == PlayableStateEnum.Running)
                        {
                            clip.RollBackExit(_executeEntity);
                        }
                    }

                    //回滚到的帧号在节点执行中， 但是该节点已经执行完毕 有可能需要再执行一次进入
                    if (_currentFrameID > clip.taskStartID && _currentFrameID <= clip.taskStartID + clip.taskDuration)
                    {
                        if (clip.State != PlayableStateEnum.Running)
                        {
                            clip.RollBackEnter(_executeEntity, _skillTimeAssets.fps);
                        }
                    }
                    
                    clip.RollBackTo(authoritySnapShot);
                }
            }
        }

        if (_stateEnum == PlayableStateEnum.Running && authorityState == PlayableStateEnum.Exit)
        {
            OnTimelineExit(_executeEntity);
        }
        
        _stateEnum = authorityState;
    }
}
