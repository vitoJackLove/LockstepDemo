using System;
using System.Threading.Tasks;
using Rogue;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public class RogueWorld : BaseWorld
{
    private fp _spawnFloorTopY;

    public RogueWorld(CreateWorldData createWorldData) : base(createWorldData) { }

    protected override async Task<bool> GamePreparation(CreateWorldData createWorldData)
    {
        SceneEnvironmentSystem sceneEnvironmentSystem = GetSystem<SceneEnvironmentSystem>();
        if (!await sceneEnvironmentSystem.SpawnEnvironmentAsync(SceneName))
        {
            return false;
        }

        _spawnFloorTopY = sceneEnvironmentSystem.FloorTopY;

        HeroEntity heroEntity = await CreateHero(createWorldData);
        if (heroEntity == null)
        {
            GameLog.Error(GameLogChannel.Battle, "Game preparation failed. Hero entity was not created.");
            return false;
        }

        MonsterEntity monsterEntity = await CreateMonster();
        if (monsterEntity == null)
        {
            GameLog.Error(GameLogChannel.Battle, "Game preparation failed. Monster entity was not created.");
            return false;
        }
        
        GetSystem<BattleUISystem>().RegisterActor(heroEntity, monsterEntity);
        return true;
    }
    
    private async Task<HeroEntity> CreateHero(CreateWorldData createWorldData)
    {
        if (createWorldData.HeroDataList == null)
        {
            GameLog.Error(GameLogChannel.Battle, "Create hero failed. HeroDataList is null.");
            return null;
        }

        HeroEntity selfLogic = null;
        bool hasSelfPlayer = false;
        
        for (int i = 0; i < createWorldData.HeroDataList.Count; i++)
        {
            PlayerData playerData = createWorldData.HeroDataList[i];
            
            if (playerData.IsSelf)
            {
                hasSelfPlayer = true;
                HeroAssetsConfig config = GameEntry.DataTable.GetDataTable<HeroAssetsConfig>(playerData.HeroId);
                if (config == null)
                {
                    GameLog.Error(GameLogChannel.Battle, $"Create hero failed. Hero config not found. heroId={playerData.HeroId}");
                    return null;
                }
            
                GameObject goView = await GetSystem<EntityViewSystem>()
                    .SyncGetEntityView(config.assetsPath, EntityRoot);
                if (goView == null)
                {
                    GameLog.Error(GameLogChannel.Battle, $"Create hero failed. View prefab load failed. heroId={playerData.HeroId}, path={config.assetsPath}");
                    return null;
                }

                EntityCreateData createDataLocal = EntityCreateData.Create(config,new fp3(0, _spawnFloorTopY, 0), 
                    fp3.zero, new fp3(1,1,1), true,goView);
                createDataLocal.EntityData = config;
                HeroEntity heroEntityLocal = GetSystem<EntitySystem>().CreateStaticEntity<HeroEntity>(createDataLocal, EntityUpdateType.LocalEntity);

                if (SessionProfile.Mode == GameSessionModeType.SinglePlayer)
                {
                    SessionProfile.EntitySyncPolicy.RegisterActor(GetSystem<EntitySystem>(), heroEntityLocal, null);
                    GetSystem<CameraSystem>().RegisterActor(heroEntityLocal);
                    GetSystem<ServerCommandSystem>().InitNumber(1);
                    selfLogic = heroEntityLocal;
                    continue;
                }

                string logicAssetsPath = config.assetsPath.Replace("View", "Logic");
                
                GameObject goLogic = await GetSystem<EntityViewSystem>()
                    .SyncGetEntityView(logicAssetsPath, EntityRoot);
                if (goLogic == null)
                {
                    GameLog.Error(GameLogChannel.Battle, $"Create hero failed. Logic prefab load failed. heroId={playerData.HeroId}, path={logicAssetsPath}");
                    return null;
                }

                EntityCreateData createDataLogic = EntityCreateData.Create(config,
                    new fp3(0, _spawnFloorTopY, 0), fp3.zero, new fp3(1,1,1), false,goLogic);
                createDataLogic.EntityData = config;
                HeroEntity heroEntityLogic = GetSystem<EntitySystem>().CreateServerEntity<HeroEntity>(playerData.ServerEntityId,createDataLogic, EntityUpdateType.AuthorityEntity);
                
                SessionProfile.EntitySyncPolicy.RegisterActor(GetSystem<EntitySystem>(), heroEntityLocal, heroEntityLogic);
                GetSystem<CameraSystem>().RegisterActor(heroEntityLocal);
                GetSystem<ServerCommandSystem>().InitNumber(createWorldData.HeroDataList.Count);
                selfLogic = heroEntityLocal;

                GetSystem<EntitySystem>().RegisterStaticEntityMap(heroEntityLocal,heroEntityLogic);
            }
            else
            {
                if (SessionProfile.Mode == GameSessionModeType.SinglePlayer)
                {
                    GameLog.Error(GameLogChannel.Battle, "Create teammate hero failed. Single player world should only contain the self player.");
                    return null;
                }

                HeroAssetsConfig config = GameEntry.DataTable.GetDataTable<HeroAssetsConfig>(playerData.HeroId);
                if (config == null)
                {
                    GameLog.Error(GameLogChannel.Battle, $"Create teammate hero failed. Hero config not found. heroId={playerData.HeroId}");
                    return null;
                }
            
                GameObject goView = await GetSystem<EntityViewSystem>()
                    .SyncGetEntityView(config.assetsPath, EntityRoot);
                if (goView == null)
                {
                    GameLog.Error(GameLogChannel.Battle, $"Create teammate hero failed. View prefab load failed. heroId={playerData.HeroId}, path={config.assetsPath}");
                    return null;
                }

                EntityCreateData createDataLogic = EntityCreateData.Create(config, new fp3(0, _spawnFloorTopY, 0),
                    fp3.zero, new fp3(1,1,1), true, goView);
                createDataLogic.EntityData = config;
                
                GetSystem<EntitySystem>().
                    CreateServerEntity<HeroEntity>((int)playerData.ServerEntityId,createDataLogic, EntityUpdateType.AuthorityEntity);
            }
        }

        if (!hasSelfPlayer)
        {
            GameLog.Error(GameLogChannel.Battle, $"Create hero failed. No self player found. playerCount={createWorldData.HeroDataList.Count}");
        }

        return selfLogic;
    }

    private async Task<MonsterEntity> CreateMonster()
    {
        MonsterAssetsConfig monsterAssetsConfig = GameEntry.DataTable.GetDataTable<MonsterAssetsConfig>(2001);
        if (monsterAssetsConfig == null)
        {
            GameLog.Error(GameLogChannel.Battle, "Create monster failed. Monster config not found. monsterId=2001");
            return null;
        }

        GameObject goView = await GetSystem<EntityViewSystem>().SyncGetEntityView(monsterAssetsConfig.assetsPath);
        if (goView == null)
        {
            GameLog.Error(GameLogChannel.Battle, $"Create monster failed. View prefab load failed. monsterId=2001, path={monsterAssetsConfig.assetsPath}");
            return null;
        }

        EntityCreateData createDataView = EntityCreateData.Create(monsterAssetsConfig,new fp3(0, _spawnFloorTopY, 5),
            fp3.zero, new fp3(1,1,1), true,goView);
        createDataView.EntityData = monsterAssetsConfig;
        MonsterEntity monsterEntityView = GetSystem<EntitySystem>().
            CreateStaticEntity<MonsterEntity>(createDataView, EntityUpdateType.LocalEntity);

        if (SessionProfile.Mode == GameSessionModeType.SinglePlayer)
        {
            return monsterEntityView;
        }
        
        GameObject goLogic = await GetSystem<EntityViewSystem>().SyncGetEntityView("Monster/3020/3020Logic");
        if (goLogic == null)
        {
            GameLog.Error(GameLogChannel.Battle, "Create monster failed. Logic prefab load failed. path=Monster/3020/3020Logic");
            return null;
        }

        EntityCreateData createDataLogic = EntityCreateData.Create(monsterAssetsConfig,new fp3(0, _spawnFloorTopY, 5), 
            fp3.zero, new fp3(1,1,1), false,goLogic);
        createDataLogic.EntityData = monsterAssetsConfig;
        MonsterEntity monsterEntityLogic = GetSystem<EntitySystem>()
            .CreateStaticEntity<MonsterEntity>(createDataLogic, EntityUpdateType.AuthorityEntity);
        GetSystem<EntitySystem>().RegisterStaticEntityMap(monsterEntityView,monsterEntityLogic);
        
        return monsterEntityView;
    }
    
    protected override Type[] GetSystemTypes()
    {
        return new []
        {
            typeof(BattleUISystem),
            typeof(EntityViewSystem),
            typeof(BehaviourTreeSystem),
            typeof(SkillTimeLineSystem),
            typeof(TouchSystem),
            typeof(CommandSystem),
            typeof(CameraSystem),
            //typeof(NavMeshSystem),
            typeof(VolumeSystem),
            typeof(FrameSyncPathfindingSystem),
            typeof(EntitySystem),
            typeof(SceneEnvironmentSystem),
            typeof(ServerCommandSystem),
            typeof(UIDamageTextSystem),
            typeof(BattleObserverSystem),
            typeof(FPKinematicCharacterSystem),
        };
    }
}
