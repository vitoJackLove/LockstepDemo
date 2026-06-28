
using Rogue;
using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("播放固定位置特效")]
public class PlayEffectClip : TaskClip
{
    [VariableName("特效ID")] public int id;
    [EditorVariable("特效预制体")] public GameObject prefab;
    [VariableName("位置偏移")] public Vector3 offset;
    
    private GameObject _temp;

    private void Init(BaseEntity context)
    {
        EffectAssetsConfig config = GameEntry.DataTable.GetDataTable<EffectAssetsConfig>(id);

        if (config == null)
        {
            return;
        }
        
        fp3 position = TsUtil.TransformPoint(context.transform.Position, 
            context.transform.EulerAngles, new fp3(1,1,1), fpmath1.Vector3ToFp3(offset));
        
        _temp = context.GetSystem<EntityViewSystem>().GetEntityView(config.assetsPath, context.BaseWorld.EntityRoot);

        _temp.transform.position = position.ToVector3();
        _temp.transform.eulerAngles = context.transform.EulerAngles.ToVector3();
    }
    
    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        Init(context);
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);
        
        Init(context);
    }

    public override void EditorEnter(GameObject context, int fps, int currentFrameID)
    {
        base.EditorEnter(context, fps, currentFrameID);

        if (prefab == null) return;
        if (context == null) return;
        
        fp3 position = TsUtil.TransformPoint(fpmath1.Vector3ToFp3(context.transform.position), 
            
            fpmath1.Vector3ToFp3(context.transform.eulerAngles), new fp3(1,1,1), fpmath1.Vector3ToFp3(offset));
            
        _temp = GameObject.Instantiate(prefab, fpmath1.Fp3ToVector3(position), Quaternion.identity);
    }

    public override void RunTimeTick(int currentFrameID, int fps,fp deltaTime, BaseEntity context)
    {
        if (context == null) return;
        if (_temp == null) return;
        
        ParticleSystem[] particleSystems = _temp.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule ps = particleSystems[i].main;
            ps.loop = false; //禁止循环
            particleSystems[i].Simulate((float)currentFrameID / fps, false,true);
        }
    }

    public override void EditorTick(int currentFrameID, int fps,fp deltaTime, GameObject context)
    {
        if (context == null) return;
        
        if (_temp == null) return;
        
        ParticleSystem[] particleSystems = _temp.GetComponentsInChildren<ParticleSystem>();
        
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule ps = particleSystems[i].main;
            ps.loop = false; //禁止循环
            particleSystems[i].Simulate((float)currentFrameID / fps, false,true);
        }
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);
        
        if (_temp != null)
        {
            GameObject.DestroyImmediate(_temp);
        }
    }

    public override void RollBackExit(BaseEntity context)
    {
        base.RollBackExit(context);
        
        if (_temp != null)
        {
            GameObject.DestroyImmediate(_temp);
        }
    }

    public override void EditorExit(GameObject context, int fps, int currentFrameID)
    {
        base.EditorExit(context, fps, currentFrameID);
        
        if (_temp != null) GameObject.DestroyImmediate(_temp);
    }
}