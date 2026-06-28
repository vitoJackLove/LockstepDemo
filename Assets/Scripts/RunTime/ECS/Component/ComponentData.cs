using System;
using System.Collections.Generic;

public sealed class ComponentData : IComponentData
{
    private IDictionary<string, object> data = new Dictionary<string, object>();

    public ComponentData()
    {
    }

    public ComponentData(IComponentData componentData)
    {
        this.PutAll(componentData);
    }

    public IDictionary<string, object> Data => this.data;

    public void Clear()
    {
        this.data.Clear();
    }

    public bool ContainsKey(string key)
    {
        return this.data.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        return this.data.Remove(key);
    }

    public T Get<T>(string key)
    {
        return Get(key, default(T));
    }

    public Type GetKeyType(string key)
    {
        object value;
        if (this.data.TryGetValue(key, out value))
            return value.GetType();
        return null;
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (this.data.TryGetValue(key, out var value))
            return (T)value;

        return defaultValue;
    }

    public void Put<T>(string key, T value)
    {
        if (!IsValidType(value))
            throw new ArgumentException("Value must be serializable!");

        this.data[key] = value;
    }

    public void PutAll(IComponentData bundle)
    {
        foreach (KeyValuePair<string, object> kv in bundle.Data)
        {
            if (!IsValidType(kv.Value))
                throw new ArgumentException("Value must be serializable!");

            this.data[kv.Key] = kv.Value;
        }
    }

    private bool IsValidType(object value)
    {
        return true;
    }
}