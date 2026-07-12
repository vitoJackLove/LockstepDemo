using Cysharp.Threading.Tasks;
using Loxodon.Framework.Contexts;
using Rogue;

/// <summary>
/// 游戏设置组件，加载全局 GameSetting 并委托热更层应用逻辑帧率。
/// </summary>
public class GameSettingComponent : RunTimeComponent
{
    private ApplicationContext _context;
    private GameSetting _gameSetting;

    public ApplicationContext ApplicationContext => _context;

    public GameSetting Setting => _gameSetting;

    public int LogicFrameRate =>
        _gameSetting != null ? _gameSetting.GetClampedLogicFrameRate() : GameSetting.DefaultLogicFrameRate;

    public override void Init()
    {
        base.Init();

        _context = Context.GetApplicationContext();
    }

    public override async UniTask InitAsync()
    {
        Init();

        _gameSetting = await GameEntry.Resource.AsyncLoadAsset<GameSetting>(
            AssetsPathHelper.GameAssetsConfigHelper("GameSetting"));

        int logicFrameRate = LogicFrameRate;
        if (_gameSetting == null)
        {
            GameLog.Warn(GameLogChannel.Bootstrap,
                "未找到 GameSetting 配置，使用默认逻辑帧率 30。");
        }

        GameEntry.HotUpdateBootstrap?.ApplyLogicFrameRate(logicFrameRate);
    }
}
