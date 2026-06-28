using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 实体创建数据
/// </summary>
public class EntityCreateData : IPool
{
    /// <summary>
    /// 初始位置
    /// </summary>
    private fp3 _initPosition;

    /// <summary>
    /// 初始角度
    /// </summary>
    private fp3 _initEulerAngles;

    /// <summary>
    /// 初始角度
    /// </summary>
    private fp3 _initScale = fp3.zero;

    /// <summary>
    /// 游戏物体
    /// </summary>
    private GameObject _entityView;

    private EntityAssetsConfig _config;
    
    /// <summary>
    /// 父实体
    /// </summary>
    private BaseEntity _parentEntity;

    /// <summary>
    /// 是否需要执行表现
    /// </summary>
    private bool _isNeedExecuteView;
    
    /// <summary>
    /// 创建实体的数据
    /// </summary>
    public object EntityData;

    public static EntityCreateData Create(EntityAssetsConfig config, fp3 position, fp3 eulerAngles, 
        fp3 scale,bool isNeedExecuteView, GameObject entityGameObject = null,BaseEntity parent = null, object customData = null)
    {
        EntityCreateData data = FPoolHelper.Get<EntityCreateData>();
        data._initPosition = position;
        data._initEulerAngles = eulerAngles;
        data._initScale = scale;
        data._entityView = entityGameObject;
        data._config = config;
        data._parentEntity = parent;
        data._isNeedExecuteView = isNeedExecuteView;
        data.EntityData = customData;
        return data;
    }

    public fp3 Position => _initPosition;

    public fp3 EulerAngles => _initEulerAngles;

    public fp3 Scale => _initScale;

    public GameObject GameObject => _entityView;

    public EntityAssetsConfig Config => _config;

    public BaseEntity ParentEntity => _parentEntity;

    public bool IsNeedExecuteView => _isNeedExecuteView;
    
    public void Clear()
    {
        _initPosition = fp3.zero;
        _initEulerAngles = fp3.zero;
        _initScale = fp3.zero;
        _entityView = null;
        _config = null;
        _parentEntity = null;
        _isNeedExecuteView = false;
        EntityData = null;
    }
}