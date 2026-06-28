using Rogue;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("创建运动类型子弹")]
public class CreateMovementBulletClip : TaskClip
{
    [VariableName("子弹ID")] public int id;

    [VariableName("位置偏移")] public Vector3 offsetPosition;

    [VariableName("移动类型")] public DisplacementEnum movementType;

    [VariableName("移动时间（帧）")] public int movementTime;

    [VariableName("移动速度")] public float movementSpeed;
    
    [VariableName("旋转偏移")] public float offsetY;
    
    [EditorVariable("子弹Editor")] public GameObject go;

    private BulletEntity _bulletEntity;
    
    private GameObject _bulletGo;

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);
        
        BulletAssetsConfig config = GameEntry.DataTable.GetDataTable<BulletAssetsConfig>(id);

        if (config == null)
        {
            return;
        }

        fp3 position = TsUtil.TransformPoint(context.transform.Position, 
            context.transform.EulerAngles, new fp3(1,1,1), offsetPosition.ToFp3());

        if (context.IsNeedExecuteView)
        {
            _bulletGo = GameObject.Instantiate(go, position.ToVector3(), Quaternion.identity);
        }
        
        EntityCreateData createData = EntityCreateData.Create(config, position,
            new fp3(0, context.transform.EulerAngles.y + (fp)offsetY, 0),
            new fp3(1,1,1), context.IsNeedExecuteView, _bulletGo, context,
            MovementData.Create(movementTime, (fp)movementSpeed, movementType));
        
        uint frame = context.EntityUpdateType == EntityUpdateType.AuthorityEntity
            ? context.BaseWorld.AuthorityTick
            : context.BaseWorld.LocalTick;
        
        //指纹
        int fingerprints = FingerprintsGenerate.GenerateFingerprint((int)frame, context.ConfigId,
            id, position, context.transform.EulerAngles);
        
        _bulletEntity = context.GetSystem<EntitySystem>().
            CreateDynamicEntity<BulletEntity>(createData, context.EntityUpdateType,fingerprints);

        context.EntityDebug($"创建运动类型子弹");
    }
    
    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);

        /*_bulletEntity?.DoEntityDead();
            
        if (_bulletGo)
        {
            GameObject.DestroyImmediate(_bulletGo);
        }*/
    }

    public override void RollBackExit(BaseEntity context)
    {
        base.RollBackExit(context);
        
        _bulletEntity?.DoEntityDead();
            
        if (_bulletGo)
        {
            GameObject.DestroyImmediate(_bulletGo);
        }
    }
}