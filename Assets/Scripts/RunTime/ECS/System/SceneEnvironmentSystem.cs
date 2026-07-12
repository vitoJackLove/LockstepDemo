using System.Collections.Generic;
using System.Threading.Tasks;
using Rogue;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 根据 <see cref="SceneAssets"/> 在世界启动时生成地板、空气墙等场景碰撞体。
/// </summary>
public class SceneEnvironmentSystem : BaseSystem
{
    private readonly List<BaseEntity> _spawnedEntities = new List<BaseEntity>();
    private readonly List<SceneEnvironmentPieceHandle> _pieces = new List<SceneEnvironmentPieceHandle>();
    private int _debugPieceCounter;
    private int _debugPlatformCounter;

    /// <summary>当前场景地板顶面世界 Y；无竞技场模板时为 0。</summary>
    public fp FloorTopY { get; private set; }

    /// <summary>已生成的场景碰撞体句柄（地板、墙体、额外碰撞体）。</summary>
    public IReadOnlyList<SceneEnvironmentPieceHandle> Pieces => _pieces;

    public async Task<bool> SpawnEnvironmentAsync(string sceneName)
    {
        SceneAssetsConfig sceneConfig = ResolveSceneConfig(sceneName);
        if (sceneConfig == null)
        {
            FloorTopY = (fp)0;
            GameLog.Warn(GameLogChannel.Battle,
                $"Scene environment skipped. Scene config not found. sceneName={sceneName}");
            return true;
        }

        FloorTopY = ResolveFloorTopY(sceneConfig);
        List<SceneColliderSpawnData> spawnPieces = CollectSpawnPieces(sceneConfig);
        for (int i = 0; i < spawnPieces.Count; i++)
        {
            CubeEntity entity = await SpawnPieceAsync(spawnPieces[i]);
            if (entity == null)
            {
                GameLog.Error(GameLogChannel.Battle,
                    $"Scene environment spawn failed. key={spawnPieces[i].key}, sceneName={sceneName}");
                return false;
            }

            _spawnedEntities.Add(entity);
            _pieces.Add(new SceneEnvironmentPieceHandle(
                spawnPieces[i].key,
                entity,
                IsArenaPieceKey(spawnPieces[i].key)));
        }

        GameLog.Info(GameLogChannel.Battle,
            $"Scene environment spawned. sceneName={sceneName}, pieceCount={spawnPieces.Count}");
        return true;
    }

    private static SceneAssetsConfig ResolveSceneConfig(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return null;
        }

        List<SceneAssetsConfig> configs = GameEntry.DataTable.GetAllDataTable<SceneAssetsConfig>();
        if (configs == null)
        {
            return null;
        }

        for (int i = 0; i < configs.Count; i++)
        {
            SceneAssetsConfig config = configs[i];
            if (config != null &&
                string.Equals(config.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return config;
            }
        }

        return null;
    }

    private static fp ResolveFloorTopY(SceneAssetsConfig sceneConfig)
    {
        if (sceneConfig?.arenaTemplate != null && sceneConfig.arenaTemplate.enabled)
        {
            return (fp)sceneConfig.arenaTemplate.FloorTopY;
        }

        return (fp)0;
    }

    private static List<SceneColliderSpawnData> CollectSpawnPieces(SceneAssetsConfig sceneConfig)
    {
        var pieces = new List<SceneColliderSpawnData>();
        if (sceneConfig.arenaTemplate != null)
        {
            pieces.AddRange(sceneConfig.arenaTemplate.BuildSpawnPieces());
        }

        if (sceneConfig.extraColliderPieces != null)
        {
            for (int i = 0; i < sceneConfig.extraColliderPieces.Count; i++)
            {
                SceneColliderPiece piece = sceneConfig.extraColliderPieces[i];
                if (piece == null)
                {
                    continue;
                }

                pieces.Add(piece.ToSpawnData());
            }
        }

        return pieces;
    }

    private async Task<CubeEntity> SpawnPieceAsync(SceneColliderSpawnData piece)
    {
        if (piece.physicsConfig == null)
        {
            GameLog.Error(GameLogChannel.Battle, $"Scene piece physics config is null. key={piece.key}");
            return null;
        }

        GameObject viewObject = null;
        if (!string.IsNullOrEmpty(piece.viewPrefabPath))
        {
            viewObject = await GetSystem<EntityViewSystem>()
                .SyncGetEntityView(piece.viewPrefabPath, CurrentWorld.MapRoot);
            if (viewObject == null)
            {
                GameLog.Warn(GameLogChannel.Battle,
                    $"Scene piece view load failed. key={piece.key}, path={piece.viewPrefabPath}");
            }
        }

        fp3 position = ToFp3(piece.position);
        fp3 eulerAngles = ToFp3(piece.eulerAngles);
        fp3 scale = ToFp3(piece.scale);
        bool hasView = viewObject != null;

        EntityCreateData createData = EntityCreateData.Create(
            null,
            position,
            eulerAngles,
            scale,
            hasView,
            viewObject,
            null,
            piece.physicsConfig);

        return GetSystem<EntitySystem>().CreateStaticEntity<CubeEntity>(createData, EntityUpdateType.LocalEntity);
    }

    private static fp3 ToFp3(Vector3 value)
    {
        return new fp3((fp)value.x, (fp)value.y, (fp)value.z);
    }

    /// <summary>
    /// 运行时调试：动态添加临时碰撞核（Play Mode 有效，退出后随世界销毁）。
    /// </summary>
    /// <returns>新碰撞体在 <see cref="Pieces"/> 中的索引；失败返回 -1。</returns>
    public async Task<int> AddDebugPieceAsync(SceneColliderShapeParams shapeParams)
    {
        string key = GenerateDebugPieceKey();
        SceneColliderSpawnData spawnData = new SceneColliderSpawnData
        {
            key = key,
            position = shapeParams.position,
            eulerAngles = shapeParams.eulerAngles,
            scale = Vector3.one,
            viewPrefabPath = null,
            physicsConfig = SceneArenaTemplate.CreateStaticShapeConfig(key, shapeParams),
        };

        CubeEntity entity = await SpawnPieceAsync(spawnData);
        if (entity == null)
        {
            return -1;
        }

        _spawnedEntities.Add(entity);
        _pieces.Add(new SceneEnvironmentPieceHandle(key, entity, false));
        return _pieces.Count - 1;
    }

    /// <summary>
    /// 运行时调试：动态添加临时运动学平台（Play Mode 有效，退出后随世界销毁）。
    /// </summary>
    /// <returns>新平台在 <see cref="Pieces"/> 中的索引；失败返回 -1。</returns>
    public async Task<int> AddDebugPlatformAsync(SceneDebugPlatformCreateData createData)
    {
        string key = GenerateDebugPlatformKey();
        var spawnPayload = new ScenePlatformSpawnData
        {
            physicsConfig = SceneArenaTemplate.CreateKinematicPlatformConfig(key, createData.shapeParams),
            motionData = new SceneDebugPlatformMotionData
            {
                moveDirection = createData.moveDirection,
                moveSpeed = createData.moveSpeed,
                travelDistance = createData.travelDistance,
            },
        };

        SceneColliderSpawnData spawnData = new SceneColliderSpawnData
        {
            key = key,
            position = createData.shapeParams.position,
            eulerAngles = createData.shapeParams.eulerAngles,
            scale = Vector3.one,
            viewPrefabPath = null,
            physicsConfig = spawnPayload.physicsConfig,
        };

        ScenePlatformCubeEntity entity = await SpawnPlatformAsync(spawnData, spawnPayload);
        if (entity == null)
        {
            return -1;
        }

        _spawnedEntities.Add(entity);
        _pieces.Add(new SceneEnvironmentPieceHandle(key, entity, false, isDebugPlatform: true));
        return _pieces.Count - 1;
    }

    /// <summary>
    /// 运行时调试：更新已生成移动平台的往复运动参数。
    /// </summary>
    public bool ApplyDebugPlatformMotion(int index, SceneDebugPlatformMotionData motionData)
    {
        if (index < 0 || index >= _pieces.Count || motionData == null)
        {
            return false;
        }

        SceneEnvironmentPieceHandle piece = _pieces[index];
        if (!piece.IsDebugPlatform)
        {
            return false;
        }

        SceneDebugPlatformMotionComponent motion =
            piece.Entity?.GetComponent<SceneDebugPlatformMotionComponent>();
        if (motion == null)
        {
            return false;
        }

        motion.ApplyMotionData(motionData);
        return true;
    }

    private async Task<ScenePlatformCubeEntity> SpawnPlatformAsync(
        SceneColliderSpawnData piece,
        ScenePlatformSpawnData spawnPayload)
    {
        if (piece.physicsConfig == null || spawnPayload == null)
        {
            return null;
        }

        fp3 position = ToFp3(piece.position);
        fp3 eulerAngles = ToFp3(piece.eulerAngles);
        fp3 scale = ToFp3(piece.scale);

        EntityCreateData createData = EntityCreateData.Create(
            null,
            position,
            eulerAngles,
            scale,
            false,
            null,
            null,
            spawnPayload);

        ScenePlatformCubeEntity entity = GetSystem<EntitySystem>().CreateStaticEntity<ScenePlatformCubeEntity>(
            createData,
            EntityUpdateType.LocalEntity);
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// 运行时调试：将单个碰撞体更新到指定位姿与形状参数。
    /// </summary>
    public bool ApplyPiece(int index, SceneColliderShapeParams shapeParams)
    {
        if (index < 0 || index >= _pieces.Count)
        {
            return false;
        }

        SceneEnvironmentPieceHandle piece = _pieces[index];
        PhysicsBodyComponent physics = piece.Entity?.GetComponent<PhysicsBodyComponent>();
        if (physics == null)
        {
            return false;
        }

        physics.ApplyStaticShape(
            ToFp3(shapeParams.position),
            ToFp3(shapeParams.eulerAngles),
            shapeParams.ToColliderSetting(piece.Key));
        return true;
    }

    /// <summary>
    /// 运行时调试：按竞技场模板重新计算并应用地板与四面墙。
    /// </summary>
    public bool ApplyArenaTemplate(SceneArenaTemplate template)
    {
        if (template == null || !template.enabled)
        {
            return false;
        }

        List<SceneColliderSpawnData> spawnPieces = template.BuildSpawnPieces();
        bool appliedAny = false;
        for (int i = 0; i < spawnPieces.Count; i++)
        {
            SceneColliderSpawnData spawnData = spawnPieces[i];
            int index = FindPieceIndex(spawnData.key);
            if (index < 0)
            {
                continue;
            }

            PhysicsColliderSetting colliderSetting = spawnData.physicsConfig?.PhysicsBody?.colliders?[0];
            if (colliderSetting == null)
            {
                continue;
            }

            if (ApplyPiece(index, SceneColliderShapeParams.FromSpawnData(spawnData, colliderSetting)))
            {
                appliedAny = true;
            }
        }

        if (appliedAny)
        {
            FloorTopY = (fp)template.FloorTopY;
        }

        return appliedAny;
    }

    /// <summary>按标识查找已生成碰撞体索引，未找到返回 -1。</summary>
    public int FindPieceIndex(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return -1;
        }

        for (int i = 0; i < _pieces.Count; i++)
        {
            if (string.Equals(_pieces[i].Key, key, System.StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsArenaPieceKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key.StartsWith("arena_", System.StringComparison.Ordinal);
    }

    private string GenerateDebugPieceKey()
    {
        do
        {
            _debugPieceCounter++;
        }
        while (FindPieceIndex($"debug_piece_{_debugPieceCounter:D3}") >= 0);

        return $"debug_piece_{_debugPieceCounter:D3}";
    }

    private string GenerateDebugPlatformKey()
    {
        do
        {
            _debugPlatformCounter++;
        }
        while (FindPieceIndex($"debug_platform_{_debugPlatformCounter:D3}") >= 0);

        return $"debug_platform_{_debugPlatformCounter:D3}";
    }
}

/// <summary>
/// 场景环境系统生成的可编辑碰撞体句柄，供运行时调试工具读写位姿与尺寸。
/// </summary>
public sealed class SceneEnvironmentPieceHandle
{
    public string Key { get; }

    public BaseEntity Entity { get; }

    public bool IsArenaPiece { get; }

    public bool IsDebugPlatform { get; }

    internal SceneEnvironmentPieceHandle(
        string key,
        BaseEntity entity,
        bool isArenaPiece,
        bool isDebugPlatform = false)
    {
        Key = key;
        Entity = entity;
        IsArenaPiece = isArenaPiece;
        IsDebugPlatform = isDebugPlatform;
    }
}
