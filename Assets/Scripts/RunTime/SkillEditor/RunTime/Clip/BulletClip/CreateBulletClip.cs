using Rogue;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("创建固定位置子弹")]
public class CreateBulletClip : TaskClip
{
    [VariableName("子弹ID")] public int id;

    [VariableName("偏移")] public Vector3 offset;

    [EditorVariable("子弹Editor")] public GameObject go;

    [VariableName("是否随技能结束销毁子弹")] public bool isSkillEndDestroyBullet;

    private GameObject _bulletGo;

    private BulletEntity _bulletEntity;

    public override void EditorEnter(GameObject context, int fps, int currentFrameID)
    {
        base.EditorEnter(context, fps, currentFrameID);

        if (go == null || context == null)
        {
            return;
        }

        fpquaternion fpquaternion = context.transform.rotation;
        
        fp3 e = fpmath1.toEulerAngles(fpquaternion);
        
        fp3 position = TsUtil.TransformPoint(fpmath1.Vector3ToFp3(context.transform.position), 
            e, new fp3(1,1,1),
            fpmath1.Vector3ToFp3(offset));

        _bulletGo = GameObject.Instantiate(go, fpmath1.Fp3ToVector3(position), Quaternion.identity);
    }

    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        BulletAssetsConfig config = GameEntry.DataTable.GetDataTable<BulletAssetsConfig>(id);

        if (config == null)
        {
            return;
        }
        
        fp3 position = TsUtil.TransformPoint(context.transform.Position, 
            context.transform.EulerAngles, new fp3(1,1,1),
            fpmath1.Vector3ToFp3(offset));
        
        EntityCreateData createData = EntityCreateData.Create(config, position,
            context.transform.EulerAngles, new fp3(1,1,1), context.IsNeedExecuteView, 
            null, context);

        uint frame = context.EntityUpdateType == EntityUpdateType.AuthorityEntity
            ? context.BaseWorld.AuthorityTick
            : context.BaseWorld.LocalTick;

        //指纹
        int fingerprints = FingerprintsGenerate.GenerateFingerprint((int)frame, context.ConfigId,
            id, position, context.transform.EulerAngles);

        _bulletEntity = context.GetSystem<EntitySystem>()
            .CreateDynamicEntity<BulletEntity>(createData, context.EntityUpdateType, fingerprints);

        context.EntityDebug($"创建固定位置子弹");
    }
    
    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);

        _bulletEntity?.DoEntityDead();
            
        if (_bulletGo)
        {
            GameObject.DestroyImmediate(_bulletGo);
        }
    }

    public override void EditorExit(GameObject context, int fps, int currentFrameID)
    {
        base.EditorExit(context, fps, currentFrameID);

        if (_bulletGo)
        {
            GameObject.DestroyImmediate(_bulletGo);
        }
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