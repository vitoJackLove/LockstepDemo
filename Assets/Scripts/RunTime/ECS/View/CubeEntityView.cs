public class BulletEntityView : EntityView
{
    public override void OnEntityDead()
    {
        base.OnEntityDead();
        
        BaseEntity.GameObject.SetActive(false);
    }
}
