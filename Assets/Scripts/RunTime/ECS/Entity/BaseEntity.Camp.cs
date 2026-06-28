
public partial class BaseEntity 
{
    public virtual CampEnum CampEnum { get; }
    
    /// <summary>
    /// 检查是否是敌对关系
    /// </summary>
    /// <param name="one"></param>
    /// <returns></returns>
    public bool CheckIsAdversarial(BaseEntity one)
    {
        RelationsBetweenCamps relations = _baseWorld.CampConfig.ReadOnlyMatrix[(int)one.CampEnum, (int)CampEnum];

        return relations == RelationsBetweenCamps.Adversarial;
    }
}
