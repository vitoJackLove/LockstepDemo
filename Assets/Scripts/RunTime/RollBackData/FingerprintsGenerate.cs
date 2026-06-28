using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 指纹生成
/// </summary>
public static class FingerprintsGenerate
{
    // 位分配方案
    private const int BITS_INT_A = 5; // 5位 (0-31)
    private const int BITS_INT_B = 4; // 4位 (0-15)
    private const int BITS_INT_C = 3; // 3位 (0-7)

    // Vector1分量分配
    private const int BITS_V1_X = 4; // 4位 (0-15)
    private const int BITS_V1_Y = 3; // 3位 (0-7)
    private const int BITS_V1_Z = 3; // 3位 (0-7)

    // Vector2分量分配
    private const int BITS_V2_X = 3; // 3位 (0-7)
    private const int BITS_V2_Y = 3; // 3位 (0-7)
    private const int BITS_V2_Z = 2; // 2位 (0-3)

    // 最大值计算
    private const int MAX_INT_A = (1 << BITS_INT_A) - 1;
    private const int MAX_INT_B = (1 << BITS_INT_B) - 1;
    private const int MAX_INT_C = (1 << BITS_INT_C) - 1;

    // Vector1范围配置
    private const float V1_X_MIN = -10f, V1_X_MAX = 10f;
    private const float V1_Y_MIN = -5f, V1_Y_MAX = 5f;
    private const float V1_Z_MIN = -5f, V1_Z_MAX = 5f;

    // Vector2范围配置
    private const float V2_X_MIN = -4f, V2_X_MAX = 4f;
    private const float V2_Y_MIN = -4f, V2_Y_MAX = 4f;
    private const float V2_Z_MIN = -2f, V2_Z_MAX = 2f;

    /// <summary>
    /// 生成指纹
    /// </summary>
    public static int GenerateFingerprint(int intA, int intB, int intC, fp3 vector1, fp3 vector2)
    {
        // 确保输入值在有效范围内
        intA = Mathf.Clamp(intA, 0, MAX_INT_A);
        intB = Mathf.Clamp(intB, 0, MAX_INT_B);
        intC = Mathf.Clamp(intC, 0, MAX_INT_C);

        // 离散化Vector1分量
        uint v1x = (uint)(short)(vector1.x * 100) * ((1 << BITS_V1_X) - 1);
        uint v1y = (uint)(short)(vector1.y * 100) * ((1 << BITS_V1_Y) - 1);
        uint v1z = (uint)(short)(vector1.z * 100) * ((1 << BITS_V1_Z) - 1);

        // 离散化Vector2分量
        uint v2x = (uint)(short)(vector2.x * 500) * ((1 << BITS_V2_X) - 1);
        uint v2y = (uint)(short)(vector2.y * 500) * ((1 << BITS_V2_Y) - 1);
        uint v2z = (uint)(short)(vector2.z * 500) * ((1 << BITS_V2_Z) - 1);

        // 组合所有值到单个int
        uint packed = 0;
        packed |= (uint)intA;
        packed |= (uint)intB << BITS_INT_A;
        packed |= (uint)intC << (BITS_INT_A + BITS_INT_B);

        packed |= v1x << (BITS_INT_A + BITS_INT_B + BITS_INT_C);
        packed |= v1y << (BITS_INT_A + BITS_INT_B + BITS_INT_C + BITS_V1_X);
        packed |= v1z << (BITS_INT_A + BITS_INT_B + BITS_INT_C + BITS_V1_X + BITS_V1_Y);

        packed |= v2x << (BITS_INT_A + BITS_INT_B + BITS_INT_C + BITS_V1_X + BITS_V1_Y + BITS_V1_Z);
        packed |= v2y << (BITS_INT_A + BITS_INT_B + BITS_INT_C + BITS_V1_X + BITS_V1_Y + BITS_V1_Z + BITS_V2_X);
        packed |= v2z << (BITS_INT_A + BITS_INT_B + BITS_INT_C + BITS_V1_X + BITS_V1_Y + BITS_V1_Z + BITS_V2_X + BITS_V2_Y);

        return (int)packed;
    }

    /*/// <summary>
    /// 生成子弹指纹
    /// </summary>
    /// <param name="frame">生成帧号</param>
    /// <param name="ownerId">生成者配置ID</param>
    /// <param name="skillId">技能ID</param>
    /// <param name="initPosition">初始位置</param>
    /// <param name="initDeg">初始角度</param>
    /// <returns></returns>
    public static byte [] GenerateBulletFingerprint(int frame, int ownerId, int skillId, Vector3 initPosition, Vector3 initDeg)
    {
        //核心标识
        var core = new byte[8];
        BinaryPrimitives.WriteInt32BigEndian(core, frame ^ ownerId);  // 帧号与拥有者交织
        BinaryPrimitives.WriteInt32BigEndian(core.AsSpan(4), skillId);
        
        //空间信息
        var spaceTime = new byte[12];
        WriteQuantizedVector(spaceTime, initPosition, 100); // 0.01精度量化
        WriteQuantizedVector(spaceTime.AsSpan(6), initDeg, 500);  // 0.01精度量化
        
        // 混沌因子（1字节）
        var entropy = new byte[1];
        entropy[0] = (byte)(frame * 37 % 256);  // 确定性随机
        
        // 组合指纹
        return core.Concat(spaceTime).Concat(entropy).ToArray();
    }

    /// <summary>
    /// 生成子弹指纹
    /// </summary>
    /// <param name="frame">生成帧号</param>
    /// <param name="creator">创建者</param>
    /// <param name="ownerId">持有者</param>
    /// <param name="buffId">技能ID</param>
    /// <returns></returns>
    public static byte[] GenerateBuffFingerprint(int frame, int creator, int ownerId, int buffId)
    {
        //核心标识
        var core = new byte[12];
        BinaryPrimitives.WriteInt32BigEndian(core, frame ^ ownerId ^ creator);  // 帧号与拥有者交织
        BinaryPrimitives.WriteInt32BigEndian(core.AsSpan(8), buffId);
        // 混沌因子（1字节）
        var entropy = new byte[1];
        entropy[0] = (byte)(frame * 37 % 256);  // 确定性随机
        
        // 组合指纹
        return core.Concat(entropy).ToArray();
    }*/

    // 向量量化写入器
    static void WriteQuantizedVector(Span<byte> dest, Vector3 v, float precision)
    {
        short x = (short)(v.x * precision);
        short y = (short)(v.y * precision);
        short z = (short)(v.z * precision);
        BinaryPrimitives.WriteInt16BigEndian(dest, x);
        BinaryPrimitives.WriteInt16BigEndian(dest.Slice(2), y);
        BinaryPrimitives.WriteInt16BigEndian(dest.Slice(4), z);
    }

    public static bool IsContains(List<byte[]> fingerprintsList, byte[] fingerprints)
    {
        for (int i = 0; i < fingerprintsList.Count; i++)
        {
            if (fingerprintsList[i].AsSpan().SequenceEqual(fingerprints))
            {
                return true;
            }
        }

        return false;
    }

    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] x, byte[] y)
        {
            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            // 高性能哈希计算
            var span = obj.AsSpan();
            if (span.IsEmpty) return 0;

            // 混合哈希算法
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < span.Length; i++)
                {
                    hash = hash * 31 + span[i];
                }

                return hash;
            }
        }
    }
}
