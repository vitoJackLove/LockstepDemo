using Cysharp.Threading.Tasks;

namespace Rogue
{
    /// <summary>
    /// 热更层启动接口：AOT 入口通过该接口回调热更逻辑，避免 AOT 直接依赖 Game.Runtime 类型。
    /// </summary>
    public interface IGameHotUpdateBootstrap
    {
        /// <summary>
        /// 加载 GameAssetConfig 数据表。Catalog 中热更 ScriptableObject 常登记为 System.Object，
        /// 需在热更程序集内用 Location 加载后再注册到 <see cref="DataTableComponent"/>。
        /// </summary>
        UniTask InitializeDataTablesAsync(DataTableComponent dataTable);

        void RegisterUiGroups();

        void OpenStartForm();

        /// <summary>
        /// 应用逻辑帧率到运行时定点数学与 Unity FixedUpdate。
        /// </summary>
        void ApplyLogicFrameRate(int logicFrameRate);

        void Update();

        void FixedUpdate();

        void LateUpdate();

        void Close();
    }
}
