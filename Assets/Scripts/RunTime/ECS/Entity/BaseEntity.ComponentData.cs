public partial class BaseEntity
{
    protected IComponentData componentData;

    private void InitEntityComponentData()
    {
        this.componentData = FPoolHelper.Get<ComponentData>();
    }

    protected void SetData<T>(string key, T value)
    {
        this.componentData.Put(key, value);
    }

    /// <summary>
    /// 获取组件数据
    /// </summary>
    public T GetData<T>(string key)
    {
        return this.componentData.Get<T>(key);
    }

    /// <summary>
    /// 获取组件数据
    /// </summary>
    public T GetData<T>(string key, T defaultValue)
    {
        return this.componentData.Get<T>(key, defaultValue);
    }

    /// <summary>
    /// 组件数据
    /// </summary>
    public IComponentData ComponentData => componentData;
}
