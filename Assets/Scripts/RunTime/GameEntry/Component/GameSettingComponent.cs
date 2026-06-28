using Loxodon.Framework.Contexts;

/// <summary>
/// 游戏设置组件 处理一些游戏性的逻辑
/// </summary>
public class GameSettingComponent : RunTimeComponent
{
    private ApplicationContext _context;

    public ApplicationContext ApplicationContext => _context;

    public override void Init()
    {
        base.Init();
        
        _context = Context.GetApplicationContext();
    }
}
