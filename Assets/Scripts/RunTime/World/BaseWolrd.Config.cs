using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rogue;
using UnityEngine;

public abstract partial class BaseWorld 
{
    /// <summary>
    /// 实体阵营配置
    /// </summary>
    private EntityCampConfig _entityCampConfig;

    /// <summary>
    /// 游戏回滚数据
    /// </summary>
    private GameRollBackContent _gameRollBackContent;
    
    public async Task InitGameConfig()
    {
        _entityCampConfig = await GameEntry.Resource.AsyncLoadAsset<EntityCampConfig>(
            AssetsPathHelper.GameAssetsConfigHelper("EntityCampConfig"));
        
        _gameRollBackContent = await GameEntry.Resource.AsyncLoadAsset<GameRollBackContent>(
            AssetsPathHelper.GameAssetsConfigHelper("RollBackGameConfig"));
    }

    public EntityCampConfig CampConfig => _entityCampConfig;

    public GameRollBackContent RollBackConfig => _gameRollBackContent;
}
