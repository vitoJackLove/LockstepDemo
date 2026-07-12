using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public static partial class fpmath1
{
    // 常量
    public static readonly fpquaternion fpquaternion_identity =
        new fpquaternion((fp)0, (fp)0, (fp)0, (fp)1);
    public static fp Epsilon = (fp)1.401298E-45f;
    /// <summary>
    /// 世界逻辑帧率，默认 30 FPS。
    /// </summary>
    public const int DefaultLogicFrameRate = GameSetting.DefaultLogicFrameRate;

    /// <summary>
    /// 世界逻辑帧 deltaTime（秒）。
    /// </summary>
    public const float LogicDeltaTimeFloat = 1f / DefaultLogicFrameRate;

    public static int LogicFrameRate = DefaultLogicFrameRate;
    public static fp LogicDeltaTime = (fp)LogicDeltaTimeFloat;

    /// <summary>
    /// 应用逻辑帧率，同步更新定点 deltaTime 与 Unity FixedUpdate 步长。
    /// </summary>
    public static void ApplyLogicFrameRate(int frameRate)
    {
        LogicFrameRate = Mathf.Clamp(frameRate, GameSetting.MinLogicFrameRate, GameSetting.MaxLogicFrameRate);
        float deltaTime = 1f / LogicFrameRate;
        LogicDeltaTime = (fp)deltaTime;
        Time.fixedDeltaTime = deltaTime;
    }
    public static fp Rad2Deg = (fp)57.29578f;
    public static fp Deg2Rad = (fp)0.017453292f;
    public static fp PositiveInfinity = 0;
    public static fp NegativeInfinity = 0;

    public static fp3 Vector3ToFp3(Vector3 value)
    {
        return new fp3((fp)value.x, (fp)value.y, (fp)value.z);
    }
    
    public static Vector3 Fp3ToVector3(fp3 value)
    {
        return new Vector3((float)value.x, (float)value.y, (float)value.z);
    }

    public static fp3 Fp2ToVector3(fp2 value)
    {
        return new fp3(value.x, value.y, 0);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion normalize(fpquaternion q)
    {
        fp lengthSq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
    
        // 检查长度是否接近0（避免除0）
        if (lengthSq < (fp)float.Epsilon)
        {
            return fpquaternion.identity;
        }
    
        fp invLength = fp.one / fpmath.sqrt(lengthSq);
        return new fpquaternion(q.x * invLength, q.y * invLength, 
            q.z * invLength, q.w * invLength);
    }
    
    public static fp2 ClampMagnitude(fp2 vector, fp maxLength)
    {
        fp sqrMagnitude = fpmath1.sqrMagnitude(vector);
        if ((fp) sqrMagnitude <= (fp) maxLength * (fp) maxLength)
            return vector;
        fp num1 = (fp) fpmath.sqrt(sqrMagnitude);
        fp num2 = vector.x / num1;
        fp num3 = vector.y / num1;
        return new fp2(num2 * maxLength, num3 * maxLength);
    }

    //TODO 未验证
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp lengthsq(fpquaternion q)
    {
        return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
    }

    //TODO 未验证
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp length(fpquaternion q)
    {
        return fpmath.sqrt(lengthsq(q));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp dot(fpquaternion a, fpquaternion b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    }

    //TODO 未验证
    // 创建四元数
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion axisAngle(fp3 axis, fp angle)
    {
        fp halfAngle = angle * (fp)0.5;
        fp sinHalf = fpmath.sin(halfAngle);
        fp cosHalf = fpmath.cos(halfAngle);

        fp3 normalizedAxis = fpmath.normalize(axis);
        return new fpquaternion(
            normalizedAxis.x * sinHalf,
            normalizedAxis.y * sinHalf,
            normalizedAxis.z * sinHalf,
            cosHalf
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion EulerXYZ(fp3 xyz)
    {
        fp Deg2Rad = fpmath.PI / 180;
        
        // 一次性计算所有半角弧度
        fp pitch = xyz.x * (Deg2Rad * (fp)0.5f);
        fp yaw = xyz.y * (Deg2Rad * (fp)0.5f);
        fp roll = xyz.z * (Deg2Rad * (fp)0.5f);
    
        // 预计算三角函数
        fp sp = fpmath.sin(pitch);
        fp cp = fpmath.cos(pitch);
        fp sy = fpmath.sin(yaw);
        fp cy = fpmath.cos(yaw);
        fp sr = fpmath.sin(roll);
        fp cr = fpmath.cos(roll);
    
        // 展开的计算公式，避免中间变量
        return normalize(new fpquaternion(
            sp * cy * cr - cp * sy * sr, // x
            cp * sy * cr + sp * cy * sr, // y  
            cp * cy * sr - sp * sy * cr, // z
            cp * cy * cr + sp * sy * sr // w
        ));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion EulerX(fp x) => axisAngle(new fp3((fp)1, (fp)0, (fp)0), x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion EulerY(fp y) => axisAngle(new fp3((fp)0, (fp)1, (fp)0), y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion EulerZ(fp z) => axisAngle(new fp3((fp)0, (fp)0, (fp)1), z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion LookRotation(fp3 forward, fp3 up)
    {
        if (fpmath1.sqrMagnitude(forward) <= (fp)0.0000001f)
        {
            return fpquaternion.identity;
        }

        forward = fpmath.normalize(forward);
        fp3 right = fpmath.cross(up, forward);
        if (fpmath1.sqrMagnitude(right) <= (fp)0.0000001f)
        {
            right = fpmath.cross(up, new fp3((fp)0, (fp)0, (fp)1));
        }

        right = fpmath.normalize(right);
        up = fpmath.cross(forward, right);

        fp m00 = right.x;
        fp m01 = right.y;
        fp m02 = right.z;
        fp m10 = up.x;
        fp m11 = up.y;
        fp m12 = up.z;
        fp m20 = forward.x;
        fp m21 = forward.y;
        fp m22 = forward.z;

        fp num8 = (m00 + m11) + m22;

        fpquaternion result = fpquaternion.identity;

        if (num8 > (fp)0)
        {
            fp num = fpmath.sqrt(num8 + (fp)1);
            result.w = num * (fp)0.5;
            num = (fp)0.5 / num;
            result.x = (m12 - m21) * num;
            result.y = (m20 - m02) * num;
            result.z = (m01 - m10) * num;
            return result;
        }

        if ((m00 >= m11) && (m00 >= m22))
        {
            fp num7 = fpmath.sqrt(((fp)1 + m00) - m11 - m22);
            fp num4 = (fp)0.5 / num7;
            result.x = (fp)0.5 * num7;
            result.y = (m01 + m10) * num4;
            result.z = (m02 + m20) * num4;
            result.w = (m12 - m21) * num4;
            return result;
        }

        if (m11 > m22)
        {
            fp num6 = fpmath.sqrt(((fp)1 + m11) - m00 - m22);
            fp num3 = (fp)0.5 / num6;
            result.x = (m10 + m01) * num3;
            result.y = (fp)0.5 * num6;
            result.z = (m21 + m12) * num3;
            result.w = (m20 - m02) * num3;
            return result;
        }

        fp num5 = fpmath.sqrt(((fp)1 + m22) - m00 - m11);
        fp num2 = (fp)0.5 / num5;
        result.x = (m20 + m02) * num2;
        result.y = (m21 + m12) * num2;
        result.z = (fp)0.5 * num5;
        result.w = (m01 - m10) * num2;
        return result;
    }

    //TODO 未验证
    // 插值函数
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion lerp(fpquaternion a, fpquaternion b, fp t)
    {
        t = fpmath.clamp(t, (fp)0, (fp)1);
        return new fpquaternion(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t,
            a.w + (b.w - a.w) * t
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion slerp(fpquaternion a, fpquaternion b, fp t)
    {
        t = fpmath.clamp(t, 0, 1);
    
        // 计算两个四元数的点积
        fp dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    
        // 如果点积为负，反转其中一个四元数以选择最短路径
        if (dot < 0)
        {
            b = new fpquaternion(-b.x, -b.y, -b.z, -b.w);
            dot = -dot;
        }
    
        fp delta = acos(dot);
        fp sinDelta = fpmath.sin(delta);
    
        // 特殊情况处理：两个四元数非常接近时使用线性插值
        if (sinDelta < (fp)1e-3f)
        {
            return lerp(a, b, t);
        }
    
        // 球面线性插值公式
        fp weightA = fpmath.sin((1 - t) * delta) / sinDelta;
        fp weightB = fpmath.sin(t * delta) / sinDelta;
    
        return new fpquaternion(
            a.x * weightA + b.x * weightB,
            a.y * weightA + b.y * weightB,
            a.z * weightA + b.z * weightB,
            a.w * weightA + b.w * weightB
        );
    }

    //TODO 未验证
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion nlerp(fpquaternion a, fpquaternion b, fp t)
    {
        return normalize(lerp(a, b, t));
    }

    // 四元数到欧拉角转换
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 toEulerAngles(fpquaternion q)
    {
        fp Rad2Deg = 180 / fpmath.PI;
    
        // 单位化四元数
        fp magnitude = fpmath.sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (fpmath.abs(magnitude - 1) > (fp)0.001f)
        {
            q.x /= magnitude;
            q.y /= magnitude;
            q.z /= magnitude;
            q.w /= magnitude;
        }
    
        // 计算欧拉角
        fp sinr_cosp = 2 * (q.w * q.z + q.x * q.y);
        fp cosr_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
        fp roll = atan2(sinr_cosp, cosr_cosp);
    
        fp sinp = 2 * (q.w * q.x - q.y * q.z);
        fp pitch;
        if (fpmath.abs(sinp) >= 1)
            pitch = fpmath.PI / 2 * fpmath.sign(sinp);
        else
            pitch = asin(sinp);
    
        fp siny_cosp = 2 * (q.w * q.y + q.z * q.x);
        fp cosy_cosp = 1 - 2 * (q.x * q.x + q.y * q.y);
        fp yaw = atan2(siny_cosp, cosy_cosp);
    
        // 转换为角度
        roll *= Rad2Deg;
        pitch *= Rad2Deg;
        yaw *= Rad2Deg;
    
        // 关键修复：处理万向锁情况
        // 当 pitch 接近 ±90 度时，yaw 和 roll 会耦合，需要特殊处理
        if (fpmath.abs(pitch) > (fp)89.999f)
        {
            // 在万向锁情况下，Unity 会将 roll 设置为 0
            // 并将所有旋转合并到 yaw 中
            yaw = yaw + roll;
            roll = 0;
        
            // 标准化 yaw 到 -180 到 180 范围
            yaw = NormalizeAngle(yaw);
        }
        else
        {
            // 正常情况下的角度标准化
            roll = NormalizeAngle(roll);
            pitch = NormalizeAngle(pitch);
            yaw = NormalizeAngle(yaw);
        }

        if (fpmath.abs(roll - 180)<= (fp)0.001f)
        {
            roll = 0;
        }
    
        return new fp3(pitch, yaw, roll);
    }

    // 角度标准化函数
    private static fp NormalizeAngle(fp angle)
    {
        angle = angle % 360;
        if (angle > 180) angle -= 360;
        if (angle < -180) angle += 360;
        return angle;
    }

    // 反正弦函数 - 使用有理近似
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp asin(fp x)
    {
        // 1. 参数范围检查
        if (x < -1 || x > 1)
        {
            return (fp)float.NaN; // 超出范围返回 NaN
        }
    
        // 2. 特殊值处理
        if (x == 1) return fpmath.PI / 2;    // asin(1) = π/2
        if (x == -1) return -fpmath.PI / 2;  // asin(-1) = -π/2
        if (x == 0) return 0;               // asin(0) = 0
        
        // 泰勒级数展开：asin(x) = x + (1/6)x³ + (3/40)x⁵ + (5/112)x⁷ + ...
    
        fp x2 = x * x;
        fp x3 = x * x2;
        fp x5 = x3 * x2;
        fp x7 = x5 * x2;
    
        // 使用前几项进行近似
        fp result = x;
        result += (fp)0.1666666667f * x3;        // 1/6
        result += (fp)0.075f * x5;               // 3/40
        result += (fp)0.0446428571f * x7;        // 5/112

        return result;
    }

    // 反余弦函数
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp acos(fp x)
    {
        // acos(x) = π/2 - asin(x)
        return fpmath.PI_OVER_2 - asin(x);
    }

    // 反正切函数
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp atan(fp x)
    {
        // 使用对称性：atan(-x) = -atan(x)
        if (x < (fp)0)
            return -atan(-x);

        // 对于大值使用恒等式：atan(x) = π/2 - atan(1/x)
        if (x > (fp)1)
            return fpmath.PI_OVER_2 - atan((fp)1 / x);

        // 使用有理近似 (精度约 1e-6)
        // 基于 Hastings 近似
        fp x2 = x * x;
        return x * ((fp)0.999866 + x2 * (-(fp)0.3302995 + x2 * ((fp)0.180141 - x2 * (fp)0.085133))) /
               ((fp)1 + x2 * (-(fp)0.329427 + x2 * ((fp)0.206858 - x2 * (fp)0.020518)));
    }

    // 反正切2 - 处理所有象限
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp atan2(fp y, fp x)
    {
        if (x > (fp)0)
            return atan(y / x);
        else if (x < (fp)0)
        {
            if (y >= (fp)0)
                return atan(y / x) + fpmath.PI;
            else
                return atan(y / x) - fpmath.PI;
        }
        else // x == 0
        {
            if (y > (fp)0)
                return fpmath.PI_OVER_2;
            else if (y < (fp)0)
                return -fpmath.PI_OVER_2;
            else
                return (fp)0; // 未定义，返回0
        }
    }

    /// <summary>
    /// 返回平方
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp sqrMagnitude(fp3 x)
    {
        return x.x * x.x + x.y * x.y + x.z * x.z;
    }
    
    /// <summary>
    /// 返回平方
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp sqrMagnitude(fp2 x)
    {
        return x.x * x.x + x.y * x.y;
    }
    
    /// <summary>
    /// 模
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp magnitude(fp3 x)
    {
        return fpmath.sqrt(x.x * x.x + x.y * x.y + x.z * x.z);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Bool3ToBool(bool3 value)
    {
        return value is { x: true, y: true ,z : true};
    }

    /// <summary>
    /// 模
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp magnitude(fp2 x)
    {
        return fpmath.sqrt(x.x * x.x + x.y * x.y);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp sign(fp x)
    {
        return  x >= 0 ? 1: -1;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp4x4 TRS(fp3 translation, fpquaternion rotation, fp3 scale)
    {
        fp4x4 result = new fp4x4();
    
        // 从四元数提取分量
        fp x = rotation.x;
        fp y = rotation.y;
        fp z = rotation.z;
        fp w = rotation.w;
    
        // 计算旋转矩阵的常用中间值（优化计算）
        fp x2 = x + x;
        fp y2 = y + y;
        fp z2 = z + z;
    
        fp xx = x * x2;
        fp xy = x * y2;
        fp xz = x * z2;
        fp yy = y * y2;
        fp yz = y * z2;
        fp zz = z * z2;
        fp wx = w * x2;
        fp wy = w * y2;
        fp wz = w * z2;
    
        // 应用缩放到旋转矩阵中
        // 第一列
        result.c0.x = (1 - (yy + zz)) * scale.x;
        result.c1.x = (xy + wz) * scale.x;
        result.c2.x = (xz - wy) * scale.x;
        result.c3.x = 0;
    
        // 第二列  
        result.c0.y = (xy - wz) * scale.y;
        result.c1.y = (1 - (xx + zz)) * scale.y;
        result.c2.y = (yz + wx) * scale.y;
        result.c3.y = 0;
    
        // 第三列
        result.c0.z = (xz + wy) * scale.z;
        result.c1.z = (yz - wx) * scale.z;
        result.c2.z = (1 - (xx + yy)) * scale.z;
        result.c3.z = 0;
    
        // 第四列（平移部分）
        result.c0.w = translation.x;
        result.c1.w = translation.y;
        result.c2.w = translation.z;
        result.c3.w= 1;
        
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 MultiplyPoint3x4(fp4x4 value, fp3 point)
    {
        fp3 vector3;
        vector3.x = (value.c0.x * point.x + value.c0.y * point.y + value.c0.z * point.z) + value.c0.w;
        vector3.y = (value.c1.x * point.x + value.c1.y * point.y + value.c1.z * point.z) + value.c1.w;
        vector3.z = (value.c2.x * point.x + value.c2.y * point.y + value.c2.z * point.z) + value.c2.w;
        return vector3;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion FromToRotation(fp3 fromDirection, fp3 toDirection)
    {
        fp3 v0 = fpmath.normalize(fromDirection);
        fp3 v1 = fpmath.normalize(toDirection);
    
        fp cosTheta = fpmath.dot(v0, v1);
    
        // 处理特殊情况
        if (cosTheta < -(fp)0.99999f)  // 接近180度
        {
            // 找到任意垂直于v0的轴
            fp3 axis = fpmath.cross(fpmath1.forward(), v0);
            if (fpmath1.sqrMagnitude(axis) < (fp)0.00001f)
                axis = fpmath.cross(right(), v0);
            axis = fpmath.normalize(axis);
            return new fpquaternion(axis.x, axis.y, axis.z, 0);
        }
        else if (cosTheta > (fp)0.99999f)  // 接近0度
        {
            return fpquaternion.identity;
        }
        else
        {
            // 标准计算 - 更稳定的方式
            fp s = fpmath.sqrt((1 + cosTheta) * 2);
            fp invs = (fp)1.0f / s;
        
            fp3 cross = fpmath.cross(v0, v1);
        
            return new fpquaternion(
                cross.x * invs,
                cross.y * invs,
                cross.z * invs,
                s * (fp)0.5f
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp LerpAngle(fp a, fp b, fp t)
    {
        fp value = b - a;
        
       fp num  =fpmath.clamp(value - fpmath.floor(value / 360) * 360, 0, 360);
        
        if ((double) num > 180.0)
            num -= 360;
        return a + num * Clamp01(t);
    }

    public static fp Clamp01(fp value)
    {
        if ( value < 0)
            return 0;
        return  value > 1 ? 1 : value;
    }
    
    public static int RoundToInt(fp value)
    {
        // Unity 使用的是 Banker's rounding (银行家舍入法)
        // 但注意：在 Unity 5.6 之前和之后的行为有变化
    
        /*
        // 1. 处理 NaN 和 Infinity
        if (fp.precision(value))
            return 0;
    
        if (float.IsPositiveInfinity(value))
            return int.MaxValue;
    
        if (float.IsNegativeInfinity(value))
            return int.MinValue;*/
    
        // 2. Unity 的实际舍入规则：
        //    - 对于 .5 的情况：向最近的偶数舍入
        //    - 例如：1.5 → 2, 2.5 → 2, 3.5 → 4
    
        // 3. 性能优化的实现方式
        fp rounded;
    
        // 使用编译器内部指令进行优化
        if (value >= 0)
        {
            // 对于正数：加上 0.5 然后截断
            rounded = value + (fp)0.5f;
        }
        else
        {
            // 对于负数：减去 0.5 然后截断
            rounded = value - (fp)0.5f;
        }
    
        // 截断小数部分
        int result = (int)rounded;
    
        // 检查是否为 .5 的情况（需要银行家舍入）
        // 这是一个简化的判断，实际实现更复杂
        if (fpmath.abs(value - result.ToFp()) == (fp)0.5f)
        {
            // 如果是 .5，检查整数部分是否为奇数
            if ((result & 1) == 1)
            {
                // 奇数保持不变
                return result;
            }
            else
            {
                // 偶数：如果原数为正，加1；为负，减1
                if (value >= 0)
                    return result + 1;
                else
                    return result - 1;
            }
        }
    
        return result;
    }

    public static bool Approximately(fp a, fp b)
    {
      return fpmath.abs(b - a) < fpmath.max((fp)1E-06f * fpmath.max(fpmath.abs(a),
          fpmath.abs(b)), (fp)Mathf.Epsilon * (fp)8f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 right() => new fp3(1, 0, 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 up() => new fp3(0, 1, 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 forward() => new fp3(0, 0, 1);
    
    // 工具函数
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion rotateX(fp angle) => axisAngle(new fp3((fp)1, (fp)0, (fp)0), angle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion rotateY(fp angle) => axisAngle(new fp3((fp)0, (fp)1, (fp)0), angle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fpquaternion rotateZ(fp angle) => axisAngle(new fp3((fp)0, (fp)0, (fp)1), angle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 forward(fpquaternion q) => q * new fp3((fp)0, (fp)0, (fp)1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 up(fpquaternion q) => q * new fp3((fp)0, (fp)1, (fp)0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static fp3 right(fpquaternion q) => q * new fp3((fp)1, (fp)0, (fp)0);
}
