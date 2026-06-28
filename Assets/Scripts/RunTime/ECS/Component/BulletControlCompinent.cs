using System.Collections.Generic;
using Ase.Serializing;

/// <summary>
/// 子弹控制组件
/// </summary>
public class BulletControlCompinent : BaseComponent
{
    /// <summary>
    /// 实体的子弹集合
    /// </summary>
    private List<BulletEntity> _bulletList = new ();

    /// <summary>
    /// 注册子弹
    /// </summary>
    /// <param name="entity"></param>
    public void RegisterBullet(BulletEntity entity)
    {
        _bulletList.Add(entity);
    }

    /// <summary>
    /// 移除子弹
    /// </summary>
    /// <param name="entity"></param>
    public void UnRegisterBullet(BulletEntity entity)
    {
        if (entity.EntityState == EntityState.Dead)
        {
            if (_bulletList.Contains(entity))
            {
                _bulletList.Remove(entity);
            }   
        }
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        
    }
}
