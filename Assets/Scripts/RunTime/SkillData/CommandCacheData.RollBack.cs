using Ase.Serializing;

public partial class CommandCacheData
{
    public void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriterBoolData($"指令缓存Down?", _down);
        hardWriter.WriterBoolData($"指令缓存Up?", _up);
        hardWriter.WriteInt32Data($"打断技能的数量", _breakSkillTypeEnumList.Count);
        
        for (int i = 0; i < _breakSkillTypeEnumList.Count; i++)
        {
            hardWriter.WriteInt32Data($"可以打断的指令", (int)_breakSkillTypeEnumList[i]);
        }
    }

    public void RollBackTo(PooledReader authoritySnapShot)
    {
        bool authorityDown = authoritySnapShot.ReadBoolean();
        
        bool authorityUp = authoritySnapShot.ReadBoolean();
        
        int authorityNumber = authoritySnapShot.ReadInt32();

        _down = authorityDown;
        _up = authorityUp;
        
        _breakSkillTypeEnumList.Clear();
        
        for (int i = 0; i < authorityNumber; i++)
        {
            CommandType authorityCommandType = (CommandType)authoritySnapShot.ReadInt32();

            _breakSkillTypeEnumList.Add(authorityCommandType);
        }
    }
}
