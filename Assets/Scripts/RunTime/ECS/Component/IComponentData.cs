using System;
using System.Collections.Generic;

/// <summary>
/// 组件数据接口
/// </summary>
public interface IComponentData : IPool
{
    IDictionary<string, object> Data { get; }

    bool ContainsKey(string key);

    bool Remove(string key);

    T Get<T>(string key);

    Type GetKeyType(string key);

    T Get<T>(string key, T defaultValue);

    void Put<T>(string key, T value);

    void PutAll(IComponentData data);
}
