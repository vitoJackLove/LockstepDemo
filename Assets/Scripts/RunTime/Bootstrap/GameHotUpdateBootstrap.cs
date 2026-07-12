using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 热更层启动实现：注册 UI 分组并打开开始界面。
    /// </summary>
    public sealed class GameHotUpdateBootstrap : IGameHotUpdateBootstrap
    {
        public async UniTask InitializeDataTablesAsync(DataTableComponent dataTable)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            for (int i = 0; i < DataTableHelper.DataTableNames.Length; i++)
            {
                string tableName = DataTableHelper.DataTableNames[i];
                string address = AssetsPathHelper.GameAssetsConfigHelper(tableName);
                ScriptableObject loadedObject = await GameEntry.Resource.AsyncLoadAsset<ScriptableObject>(address);
                if (loadedObject == null)
                {
                    throw new InvalidOperationException(
                        $"DataTable load failed. address={address}, table={tableName}");
                }

                if (loadedObject is not IAssetsConfig assetsConfig)
                {
                    throw new InvalidOperationException(
                        $"DataTable type mismatch. address={address}, loadedType={loadedObject.GetType().FullName}");
                }

                dataTable.RegisterAssetsConfig(assetsConfig);
            }
        }

        public void RegisterUiGroups()
        {
            GameEntry.UI.AddUIGroup(Content.UI.UIDefaultGroup);
            GameEntry.UI.AddUIGroup(Content.UI.UILoadingGroup);
        }

        public void OpenStartForm()
        {
            OpenStartFormAsync().Forget();
        }

        public void ApplyLogicFrameRate(int logicFrameRate)
        {
            fpmath1.ApplyLogicFrameRate(logicFrameRate);
            GameLog.Info(GameLogChannel.Bootstrap,
                $"逻辑帧率已应用: {fpmath1.LogicFrameRate} FPS, deltaTime={fpmath1.LogicDeltaTime}");
        }

        public void Update()
        {
            Game.Update();
        }

        public void FixedUpdate()
        {
            Game.FixedUpdate();
        }

        public void LateUpdate()
        {
            Game.LateUpdate();
        }

        public void Close()
        {
            Game.Close();
        }

        private static async UniTaskVoid OpenStartFormAsync()
        {
            await GameEntry.UI.OpenUIWindow<GameStartUpWindow>(
                AssetsPathHelper.UIWindowPathHelper("GameStartUp"),
                Content.UI.UIDefaultGroup,
                null,
                null);
        }
    }
}
