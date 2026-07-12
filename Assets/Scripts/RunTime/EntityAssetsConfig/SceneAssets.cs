using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/SceneAssets")]
public class SceneAssets : ScriptableObject, IAssetsConfig
{
    public List<SceneAssetsConfig> SceneAssetsConfigList = new List<SceneAssetsConfig>();

    public Type GetDataTableType()
    {
        return typeof(SceneAssetsConfig);
    }

    public EntityAssetsConfig GetDataTable(int id)
    {
        for (int i = 0; i < SceneAssetsConfigList.Count; i++)
        {
            if (SceneAssetsConfigList[i].assetsId == id)
            {
                return SceneAssetsConfigList[i];
            }
        }

        return null;
    }

    public List<EntityAssetsConfig> GetAllDataTable()
    {
        List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();
        for (int i = 0; i < SceneAssetsConfigList.Count; i++)
        {
            list.Add(SceneAssetsConfigList[i]);
        }

        return list;
    }

    public SceneAssetsConfig GetBySceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return null;
        }

        for (int i = 0; i < SceneAssetsConfigList.Count; i++)
        {
            SceneAssetsConfig config = SceneAssetsConfigList[i];
            if (config != null &&
                string.Equals(config.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return config;
            }
        }

        return null;
    }
}

[Serializable]
public class SceneAssetsConfig : EntityAssetsConfig
{
    [LabelText("场景名")]
    [Tooltip("与 CreateWorldData.SceneName 一致，例如 RougeBattle")]
    public string sceneName = "RougeBattle";

    [LabelText("竞技场模板")]
    public SceneArenaTemplate arenaTemplate = new SceneArenaTemplate();

    [LabelText("额外碰撞体")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "key")]
    public List<SceneColliderPiece> extraColliderPieces = new List<SceneColliderPiece>();
}

[Serializable]
public class SceneArenaTemplate
{
    [LabelText("启用模板")]
    public bool enabled = true;

    [LabelText("中心")]
    [Tooltip("地板顶面高度由 center.y 决定；英雄/怪物出生点 Y 需与此一致（脚底贴地）。")]
    public Vector3 center;

    /// <summary>竞技场地板顶面世界 Y（与 <see cref="center"/>.y 相同）。</summary>
    public float FloorTopY => center.y;

    [LabelText("宽度 (X)")]
    [MinValue(1f)]
    public float width = 20f;

    [LabelText("深度 (Z)")]
    [MinValue(1f)]
    public float depth = 20f;

    [LabelText("墙高")]
    [MinValue(0.5f)]
    public float wallHeight = 3f;

    [LabelText("地板厚度")]
    [MinValue(0.1f)]
    public float floorThickness = 0.5f;

    [LabelText("墙厚")]
    [MinValue(0.1f)]
    public float wallThickness = 0.5f;

    [LabelText("地板视觉 Prefab")]
    [Tooltip("留空则仅生成物理碰撞体")]
    public string floorViewPrefabPath;

    [LabelText("墙体视觉 Prefab")]
    [Tooltip("留空则仅生成物理碰撞体；四面墙共用")]
    public string wallViewPrefabPath;

    public List<SceneColliderSpawnData> BuildSpawnPieces()
    {
        var pieces = new List<SceneColliderSpawnData>(5);
        if (!enabled)
        {
            return pieces;
        }

        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float halfFloor = floorThickness * 0.5f;
        float halfWallHeight = wallHeight * 0.5f;
        float halfWallThickness = wallThickness * 0.5f;
        float floorTopY = center.y;
        float floorCenterY = floorTopY - halfFloor;
        float wallCenterY = floorTopY + halfWallHeight;

        pieces.Add(CreateBoxPiece(
            "arena_floor",
            new Vector3(center.x, floorCenterY, center.z),
            new Vector3(halfWidth, halfFloor, halfDepth),
            FPCollisionLayer.Wall,
            floorViewPrefabPath));

        pieces.Add(CreateBoxPiece(
            "arena_wall_north",
            new Vector3(center.x, wallCenterY, center.z + halfDepth),
            new Vector3(halfWidth, halfWallHeight, halfWallThickness),
            FPCollisionLayer.Wall,
            wallViewPrefabPath));

        pieces.Add(CreateBoxPiece(
            "arena_wall_south",
            new Vector3(center.x, wallCenterY, center.z - halfDepth),
            new Vector3(halfWidth, halfWallHeight, halfWallThickness),
            FPCollisionLayer.Wall,
            wallViewPrefabPath));

        pieces.Add(CreateBoxPiece(
            "arena_wall_east",
            new Vector3(center.x + halfWidth, wallCenterY, center.z),
            new Vector3(halfWallThickness, halfWallHeight, halfDepth),
            FPCollisionLayer.Wall,
            wallViewPrefabPath));

        pieces.Add(CreateBoxPiece(
            "arena_wall_west",
            new Vector3(center.x - halfWidth, wallCenterY, center.z),
            new Vector3(halfWallThickness, halfWallHeight, halfDepth),
            FPCollisionLayer.Wall,
            wallViewPrefabPath));

        return pieces;
    }

    private static SceneColliderSpawnData CreateBoxPiece(
        string key,
        Vector3 position,
        Vector3 halfExtents,
        FPCollisionLayer layer,
        string viewPrefabPath)
    {
        return new SceneColliderSpawnData
        {
            key = key,
            position = position,
            eulerAngles = Vector3.zero,
            scale = Vector3.one,
            viewPrefabPath = viewPrefabPath,
            physicsConfig = CreateStaticBoxConfig(key, halfExtents, layer),
        };
    }

    internal static PhysicsEntityConfig CreateStaticBoxConfig(
        string key,
        Vector3 halfExtents,
        FPCollisionLayer layer)
    {
        return CreateStaticShapeConfig(key, new SceneColliderShapeParams
        {
            shape = PhysicsShapeType.Box,
            halfExtents = halfExtents,
            layer = layer,
        });
    }

    internal static PhysicsEntityConfig CreateStaticShapeConfig(string key, SceneColliderShapeParams data)
    {
        return new PhysicsEntityConfig
        {
            MovementMode = PhysicsMovementMode.Rigidbody,
            PhysicsBody = new PhysicsBodyConfig
            {
                bodyType = PhysicsBodyType.Static,
                colliders = new List<PhysicsColliderSetting>
                {
                    data.ToColliderSetting(key),
                }
            }
        };
    }

    internal static PhysicsEntityConfig CreateKinematicPlatformConfig(string key, SceneColliderShapeParams data)
    {
        SceneColliderShapeParams platformShape = data;
        platformShape.layer = FPCollisionLayer.KinematicPlatform;
        platformShape.isTrigger = false;

        return new PhysicsEntityConfig
        {
            MovementMode = PhysicsMovementMode.Rigidbody,
            PhysicsBody = new PhysicsBodyConfig
            {
                bodyType = PhysicsBodyType.Static,
                useGravity = false,
                usePhysicsMover = true,
                colliders = new List<PhysicsColliderSetting>
                {
                    platformShape.ToColliderSetting(key),
                }
            }
        };
    }
}

[Serializable]
public class SceneColliderPiece
{
    [LabelText("标识")]
    public string key = "extra_collider";

    [LabelText("位置")]
    public Vector3 position;

    [LabelText("欧拉角")]
    public Vector3 eulerAngles;

    [LabelText("缩放")]
    public Vector3 scale = Vector3.one;

    [LabelText("物理体")]
    [InlineProperty(LabelWidth = 120)]
    public PhysicsBodyConfig physicsBody = new PhysicsBodyConfig
    {
        bodyType = PhysicsBodyType.Static,
        colliders = new List<PhysicsColliderSetting>
        {
            new PhysicsColliderSetting
            {
                key = "body",
                shape = PhysicsShapeType.Box,
                halfExtents = new Vector3(0.5f, 0.5f, 0.5f),
                layer = FPCollisionLayer.Wall,
            }
        }
    };

    [LabelText("视觉 Prefab")]
    [Tooltip("留空则仅生成物理碰撞体")]
    public string viewPrefabPath;

    public SceneColliderSpawnData ToSpawnData()
    {
        return new SceneColliderSpawnData
        {
            key = key,
            position = position,
            eulerAngles = eulerAngles,
            scale = scale,
            viewPrefabPath = viewPrefabPath,
            physicsConfig = new PhysicsEntityConfig
            {
                MovementMode = PhysicsMovementMode.Rigidbody,
                PhysicsBody = physicsBody,
            }
        };
    }
}

public struct SceneColliderSpawnData
{
    public string key;
    public Vector3 position;
    public Vector3 eulerAngles;
    public Vector3 scale;
    public PhysicsEntityConfig physicsConfig;
    public string viewPrefabPath;
}

/// <summary>
/// 场景碰撞体形状参数，供运行时调试添加与编辑 Static 碰撞核。
/// </summary>
public struct SceneColliderShapeParams
{
    public PhysicsShapeType shape;
    public Vector3 position;
    public Vector3 eulerAngles;
    public FPCollisionLayer layer;
    public bool isTrigger;
    public Vector3 halfExtents;
    public float radius;
    public float capsuleRadius;
    public float capsuleHeight;
    public int directionAxis;

    public static SceneColliderShapeParams CreateDefault(PhysicsShapeType shapeType)
    {
        return new SceneColliderShapeParams
        {
            shape = shapeType,
            position = Vector3.zero,
            eulerAngles = Vector3.zero,
            layer = FPCollisionLayer.Wall,
            isTrigger = false,
            halfExtents = Vector3.one * 0.5f,
            radius = 0.5f,
            capsuleRadius = 0.4f,
            capsuleHeight = 1.8f,
            directionAxis = 1,
        };
    }

    public PhysicsColliderSetting ToColliderSetting(string key)
    {
        return new PhysicsColliderSetting
        {
            key = key,
            shape = shape,
            layer = layer,
            isTrigger = isTrigger,
            halfExtents = halfExtents,
            radius = radius,
            capsuleRadius = capsuleRadius,
            capsuleHeight = capsuleHeight,
            directionAxis = directionAxis,
        };
    }

    public static SceneColliderShapeParams FromSpawnData(
        SceneColliderSpawnData spawnData,
        PhysicsColliderSetting colliderSetting)
    {
        return new SceneColliderShapeParams
        {
            shape = colliderSetting.shape,
            position = spawnData.position,
            eulerAngles = spawnData.eulerAngles,
            layer = colliderSetting.layer,
            isTrigger = colliderSetting.isTrigger,
            halfExtents = colliderSetting.halfExtents,
            radius = colliderSetting.radius,
            capsuleRadius = colliderSetting.capsuleRadius,
            capsuleHeight = colliderSetting.capsuleHeight,
            directionAxis = colliderSetting.directionAxis,
        };
    }
}

/// <summary>
/// 场景调试移动平台创建参数（碰撞形状 + 往复运动）。
/// </summary>
public struct SceneDebugPlatformCreateData
{
    public SceneColliderShapeParams shapeParams;
    public Vector3 moveDirection;
    public float moveSpeed;
    public float travelDistance;

    public static SceneDebugPlatformCreateData CreateDefault()
    {
        SceneColliderShapeParams shape = SceneColliderShapeParams.CreateDefault(PhysicsShapeType.Box);
        shape.position = new Vector3(0f, 1f, 0f);
        shape.halfExtents = new Vector3(2f, 0.15f, 1f);
        shape.layer = FPCollisionLayer.KinematicPlatform;

        return new SceneDebugPlatformCreateData
        {
            shapeParams = shape,
            moveDirection = Vector3.right,
            moveSpeed = 2f,
            travelDistance = 5f,
        };
    }
}
