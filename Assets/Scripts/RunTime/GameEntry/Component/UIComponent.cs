using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Views;
using Unity.VisualScripting;
using UnityEngine;

public class UIComponent : RunTimeComponent
{
    /// <summary>
    /// 窗口集合
    /// </summary>
    private Dictionary<string, WindowContainer> _uiGroups = new Dictionary<string, WindowContainer>();

    /// <summary>
    /// UI的根节点
    /// </summary>
    public Transform uiRoot;

    /// <summary>
    /// UI相机
    /// </summary>
    public Camera uiCamera;

    private ResourceComponent _resourceComponent;

    private GlobalWindowManager _windowManager;

    public override void Init()
    {
        _resourceComponent = GameEntryRunTime.GetComponent<ResourceComponent>();

        _windowManager = uiRoot.AddComponent<GlobalWindowManager>();

        BindingServiceBundle bundle =
            new BindingServiceBundle(GameEntryRunTime.GetComponent<GameSettingComponent>().ApplicationContext.GetContainer());

        bundle.Start();
    }

    public bool HasUIGroup(string uiGroupName) => this._uiGroups.ContainsKey(uiGroupName);


    public bool AddUIGroup(string uiGroupName)
    {
        if (this.HasUIGroup(uiGroupName))
            return false;
        this._uiGroups.Add(uiGroupName, WindowContainer.Create(uiGroupName));
        return true;
    }

    public WindowContainer GetUIGroup(string uiGroupName)
    {
        WindowContainer uiGroup;
        this._uiGroups.TryGetValue(uiGroupName, out uiGroup);
        return uiGroup;
    }

    public async UniTask<T> LoadWindow<T>(
        string uiFormAssetName,
        string uiGroupName,
        object userData)
        where T : IWindow
    {
        T obj = await this.LoadWindowAsync<T>((IWindowManager)this.GetUIGroup(uiGroupName), uiFormAssetName);
        T loadWindow = obj;
        obj = default(T);
        IBundle bundle = (IBundle)new Bundle();
        if (userData != null)
        {
            bundle.Put<object>("windowData", userData);
            bundle.Put<string>("assetPath", uiFormAssetName);
            bundle.Put<string>("uiGroup", uiGroupName);
        }

        loadWindow.Create(bundle);
        T obj1 = loadWindow;
        loadWindow = default(T);
        bundle = (IBundle)null;
        return obj1;
    }

    private async UniTask<T> LoadWindowAsync<T>(
        IWindowManager windowManager,
        string uiFormAssetName)
        where T : IWindow
    {
        if (windowManager == null)
            windowManager = (IWindowManager)this._windowManager;
        T obj = await this.DoLoadWindowAsync<T>(uiFormAssetName);
        T target = obj;
        obj = default(T);
        if ((object)target != null)
            target.WindowManager = windowManager;
        T obj1 = target;
        target = default(T);
        return obj1;
    }

    private async UniTask<T> DoLoadWindowAsync<T>(string uiFormAssetName) where T : IWindow
    {
        GameObject viewTemplateGo = await this._resourceComponent.AsyncLoadAsset<GameObject>(uiFormAssetName);

        if (viewTemplateGo == null)
        {
            GameLog.Error(GameLogChannel.UI, $"Load window failed. prefab is null, path={uiFormAssetName}");
            return default(T);
        }

        GameObject go = Instantiate(viewTemplateGo);

        go.name = viewTemplateGo.name;
        viewTemplateGo = (GameObject)null;
        T view = go.GetComponent<T>();
        if ((object)view == null && (UnityEngine.Object)go != (UnityEngine.Object)null)
            UnityEngine.Object.Destroy((UnityEngine.Object)go);
        T obj = view;
        viewTemplateGo = (GameObject)null;
        go = (GameObject)null;
        view = default(T);
        return obj;
    }

    public async UniTask<T> OpenUIWindow<T>(
        string uiFormAssetName,
        string uiGroupName,
        object userData,
        Action callback)
        where T : IWindow
    {
        T obj = await this.LoadWindowAsync<T>((IWindowManager)this.GetUIGroup(uiGroupName), uiFormAssetName);

        T loadWindow = obj;

        obj = default(T);

        if ((object)loadWindow == null)
        {
            GameLog.Error(GameLogChannel.UI, "OpenUIWindow error : window == null...");

            return default(T);
        }

        if (callback != null)
        {
            void Handler(object window, EventArgs e)
            {
                // ISSUE: method pointer
                ((Window)window).OnDismissed -= Handler;
                callback();
            }
            
            loadWindow.OnDismissed += Handler;
        }

        IBundle bundle = (IBundle)new Bundle();
        if (userData != null)
        {
            bundle.Put<object>(BindDataKey.WindowData, userData);
            bundle.Put<string>("assetPath", uiFormAssetName);
            bundle.Put<string>("uiGroup", uiGroupName);
        }

        loadWindow.Create(bundle);
        loadWindow.AssetPath = uiFormAssetName;
        await loadWindow.Show(false);
        T obj2 = loadWindow;
        loadWindow = default(T);
        bundle = (IBundle)null;
        return obj2;
    }

    public override void Shutdown()
    {
    }
}
