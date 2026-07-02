using System;
using System.Threading.Tasks;
using Rogue;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 1v1 PVP 格斗世界：双 Hero 单模拟态，配合严格 GGPO 帧同步。
/// </summary>
public class PvpWorld : BaseWorld
{
    private const int RequiredPlayerCount = 2;

    public PvpWorld(CreateWorldData createWorldData) : base(createWorldData) { }

    protected override async Task<bool> GamePreparation(CreateWorldData createWorldData)
    {
        if (createWorldData.HeroDataList == null || createWorldData.HeroDataList.Count < RequiredPlayerCount)
        {
            GameLog.Error(GameLogChannel.Battle, $"PVP 准备失败：需要 {RequiredPlayerCount} 名玩家，当前={createWorldData.HeroDataList?.Count ?? 0}");
            return false;
        }

        HeroEntity localHero = null;
        HeroEntity remoteHero = null;

        for (int i = 0; i < createWorldData.HeroDataList.Count; i++)
        {
            PlayerData playerData = createWorldData.HeroDataList[i];
            HeroEntity hero = await CreateHero(playerData, i);
            if (hero == null)
            {
                GameLog.Error(GameLogChannel.Battle, $"PVP 准备失败：创建英雄失败 playerIndex={playerData.ServerEntityId}");
                return false;
            }

            if (playerData.IsSelf)
            {
                localHero = hero;
                SessionProfile.EntitySyncPolicy.RegisterActor(GetSystem<EntitySystem>(), hero, hero);
                GetSystem<CameraSystem>().RegisterActor(hero);
            }
            else
            {
                remoteHero = hero;
            }
        }

        if (localHero == null || remoteHero == null)
        {
            GameLog.Error(GameLogChannel.Battle, "PVP 准备失败：缺少本地或远端英雄。");
            return false;
        }

        GetSystem<ServerCommandSystem>().InitNumber(RequiredPlayerCount);
        InitializeGgpo(localHero.EntityId, remoteHero.EntityId);
        GetSystem<BattleUISystem>().RegisterActors(localHero, remoteHero);
        return true;
    }

    private async Task<HeroEntity> CreateHero(PlayerData playerData, int slotIndex)
    {
        HeroAssetsConfig config = GameEntry.DataTable.GetDataTable<HeroAssetsConfig>(playerData.HeroId);
        if (config == null)
        {
            GameLog.Error(GameLogChannel.Battle, $"创建英雄失败：配置不存在 heroId={playerData.HeroId}");
            return null;
        }

        GameObject goView = await GetSystem<EntityViewSystem>().SyncGetEntityView(config.assetsPath, EntityRoot);
        if (goView == null)
        {
            GameLog.Error(GameLogChannel.Battle, $"创建英雄失败：View 加载失败 path={config.assetsPath}");
            return null;
        }

        fp3 spawnPosition = slotIndex == 0 ? new fp3(-3, 0, 0) : new fp3(3, 0, 0);
        fp3 right = new fp3(1, 0, 0);
        fpquaternion spawnRotation = slotIndex == 0
            ? fpmath1.LookRotation(right, fpmath1.up())
            : fpmath1.LookRotation(-right, fpmath1.up());

        fp3 spawnEuler = spawnRotation.ToEulerAngles();
        EntityCreateData createData = EntityCreateData.Create(config, spawnPosition, spawnEuler, new fp3(1, 1, 1), true, goView);
        createData.EntityData = config;
        return GetSystem<EntitySystem>().CreateServerEntity<HeroEntity>(playerData.ServerEntityId, createData, EntityUpdateType.LocalEntity);
    }

    protected override Type[] GetSystemTypes()
    {
        return new[]
        {
            typeof(BattleUISystem),
            typeof(EntityViewSystem),
            typeof(SkillTimeLineSystem),
            typeof(TouchSystem),
            typeof(CommandSystem),
            typeof(CameraSystem),
            typeof(VolumeSystem),
            typeof(EntitySystem),
            typeof(ServerCommandSystem),
            typeof(UIDamageTextSystem),
            typeof(BattleObserverSystem),
        };
    }
}
