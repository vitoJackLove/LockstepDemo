using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 快照数据
/// </summary>
public class BaseSnapShotData
{
    public static bool UseDebugStringCompare { get; set; }

    /// <summary>
    /// 正式
    /// </summary>
    private PooledWriter _pooledWriter;

    public PooledWriter PooledWriter => _pooledWriter;

    public BaseSnapShotData()
    {
        _pooledWriter = WriterPool.GetWriter();
    }
    
    /// <summary>
    /// 开发版本
    /// </summary>
    private string _develSnapShotData;

    public string DevelSnapShotData => _develSnapShotData;

    public void WriteString(string key)
    {
        _develSnapShotData += $" key = {key}";
    }
    
    public void WriteInt32Data(string key, int value)
    {
        _develSnapShotData += $"key = {key}  value = {value}";
        
        _pooledWriter.WriteInt32(value);
    }

    public void WriterFpData(string key, fp value)
    {
        long rawValue = value.RawValue;
        _develSnapShotData += $"key = {key}  value = {value}  raw = {rawValue}";

        _pooledWriter.WriteInt64(rawValue);
    }

    public void WriterByteData(string key, byte [] value)
    {
        _develSnapShotData += $"key = {key}  value = {value}";

        _pooledWriter.WriteBytes(value, 0, value.Length);
    }

    public void WriterBoolData(string key, bool value)
    {
        _develSnapShotData += $"key = {key}  value = {value}";
        
        _pooledWriter.WriteBoolean(value);
    }


    public void WriterFp3Data(string key, fp3 value)
    {
        long rawX = value.x.RawValue;
        long rawY = value.y.RawValue;
        long rawZ = value.z.RawValue;
        _develSnapShotData +=
            $"key = {key}  value = {value}  raw = ({rawX}, {rawY}, {rawZ})";

        _pooledWriter.WriteInt64(rawX);
        _pooledWriter.WriteInt64(rawY);
        _pooledWriter.WriteInt64(rawZ);
    }

    public static fp ReadFpData(PooledReader reader)
    {
        return fp.FromRaw(reader.ReadInt64());
    }

    public static fp3 ReadFp3Data(PooledReader reader)
    {
        return new fp3(
            fp.FromRaw(reader.ReadInt64()),
            fp.FromRaw(reader.ReadInt64()),
            fp.FromRaw(reader.ReadInt64()));
    }

    public void WriterFpQuaternionData(string key, fpquaternion value)
    {
        long rawX = value.valuex.RawValue;
        long rawY = value.valuey.RawValue;
        long rawZ = value.valuez.RawValue;
        long rawW = value.valuew.RawValue;
        _develSnapShotData +=
            $"key = {key}  value = {value}  raw = ({rawX}, {rawY}, {rawZ}, {rawW})";

        _pooledWriter.WriteInt64(rawX);
        _pooledWriter.WriteInt64(rawY);
        _pooledWriter.WriteInt64(rawZ);
        _pooledWriter.WriteInt64(rawW);
    }

    public static fpquaternion ReadFpQuaternionData(PooledReader reader)
    {
        return new fpquaternion(
            fp.FromRaw(reader.ReadInt64()),
            fp.FromRaw(reader.ReadInt64()),
            fp.FromRaw(reader.ReadInt64()),
            fp.FromRaw(reader.ReadInt64()));
    }

    public bool IsSameData(BaseSnapShotData snapShotData)
    {
        if (snapShotData == null)
        {
            return false;
        }

        if (UseDebugStringCompare)
        {
            return string.Equals(snapShotData.DevelSnapShotData, _develSnapShotData);
        }

        return _pooledWriter.SequenceEqual(snapShotData.PooledWriter);
    }

    public bool Equals(BaseSnapShotData snapShotData)
    {
        return IsSameData(snapShotData);
    }
}
