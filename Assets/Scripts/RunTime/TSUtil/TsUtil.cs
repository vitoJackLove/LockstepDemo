using System;
using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public static class TsUtil
{
    /// <summary>
    /// 获取二进制数据的某段
    /// </summary>
    /// <param name="aBinary">原始值</param>
    /// <param name="bitRemove">去除不需要的尾巴 位数</param>
    /// <param name="bitNeed">所需数据的位数</param>
    /// <returns></returns>
    public static int GetLongByBinary(int aBinary, int bitRemove, int bitNeed)
    {
        return (aBinary >> bitRemove) - (aBinary >> (bitRemove + bitNeed) << bitNeed);
    }

    public static float Harf2Full(float harf)
    {
        if (harf < 0)
            return harf + 360;
        if (harf > 360)
            return harf - 360;

        return harf;
    }

    /// <summary>
    /// 根据中心点旋转和缩放求新的世界坐标
    /// </summary>
    /// <param name="centerPos">中心点</param>
    /// <param name="rotation">旋转</param>
    /// <param name="scale">缩放</param>
    /// <param name="pos">坐标</param>
    /// <returns></returns>
    public static fp3 TransformPoint(fp3 centerPos, fp3 rotation, fp3 scale, fp3 pos)
    {
        fpquaternion rotate = fpmath1.EulerXYZ(rotation);
        
        fp4x4 matrix = fpmath1.TRS(centerPos, rotate, scale);

        return fpmath1.MultiplyPoint3x4(matrix, pos);
    }

    /// <summary>
    /// 根据中心点旋转和缩放求新的世界坐标
    /// </summary>
    /// <param name="centerPos">中心点</param>
    /// <param name="rotation">旋转</param>
    /// <param name="scale">缩放</param>
    /// <param name="pos">坐标</param>
    /// <returns></returns>
    public static Vector3 TransformPoint(Vector3 centerPos, Vector3 rotation, Vector3 scale, Vector3 pos)
    {
        Quaternion rotate = Quaternion.Euler(rotation);
        
        Matrix4x4 matrix = Matrix4x4.TRS(centerPos, rotate, scale);

        return matrix.MultiplyPoint3x4(pos);
    }
    
    /// <summary>
    /// 向某个方向移动一段距离
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static fp3 MoveForward2D(fp3 position, fp rotation, fp distance)
    {
        fp rotationRad = (rotation * (fpmath.PI / 180));
        fp moveX = fpmath.sin(rotationRad) * distance;
        fp moveY = fpmath.cos(rotationRad) * distance;
        position.x += moveX;
        position.z += moveY;
        return position;
    }

      
    /// <summary>
    /// 向某个方向移动一段距离
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    public static Vector3 MoveForward2DVec(Vector3 position, float rotation, float distance)
    {
        float rotationRad = rotation * (math.PI / 180);
        float moveX = math.sin(rotationRad) * distance;
        float moveY = math.cos(rotationRad) * distance;
        position.x += moveX;
        position.z += moveY;
        return position;
    }

    /// <summary>
    /// 返回相对自己的目标位置的角度(-179到180)
    /// 注意很多地方selfRot都要-180朝向是由问题的
    /// </summary>
    /// <param name="selfPos"></param>
    /// <param name="selfRot"></param>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    public static float TargetFwdAngleToSelf(Vector3 selfPos, float selfRot, Vector3 targetPos)
    {
        Vector2 diff = new Vector2(selfPos.x - targetPos.x, selfPos.z - targetPos.z);
        selfRot = Angle2Limit(selfRot);
        float diffAngle = Vector2Angle(diff);
        float selfAngle = Full2Half(selfRot);
        float angle = Full2Half(Angle2Limit(diffAngle - selfAngle));
        return angle;
    }

    /// <summary>
    /// 获取目标相对于自身的角度（-180 到 180）
    /// </summary>
    public static float GetAngleBetween(Vector2 selfPos, float selfRot, Vector2 targetPos)
    {
        // 自身朝向的方向向量
        float rad = selfRot * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)); // Unity 中正前方是 Z+

        // 指向目标的方向向量
        Vector2 toTarget = (targetPos - selfPos).normalized;

        // 计算叉乘和点乘
        float dot = Vector2.Dot(forward, toTarget);
        float cross = forward.x * toTarget.y - forward.y * toTarget.x;

        // atan2(叉, 点) 得到[-180, 180] 夹角
        float angle = Mathf.Atan2(cross, dot) * Mathf.Rad2Deg;
        return angle;
    }

    /// <summary>
    /// 规范化角度为 [-180, 180] 区间
    /// </summary>
    public static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// 代码有问题，可能导致很多地方都要-180
    /// </summary>
    public static float Vector2Angle(Vector2 diff)
    {
        diff.Normalize();
        return Mathf.Rad2Deg * Mathf.Atan2(diff.x, diff.y);
    }

    /// <summary>
    /// 把任意角重置为0-359的区间
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static float Angle2Limit(float value)
    {
        if (value > 360)
        {
            return value % 360;
        }

        while (true)
        {
            if (value < 0)
            {
                value = value + 360;
            }
            else if (value > 360)
            {
                value = value - 360;
            }
            else
            {
                return value;
            }
        }
    }

    /// <summary>
    /// 将0-359的度数转换成-179到180的度数,-179到180度则保持原样
    ///  1     179
    /// 0---------x轴,左正右负，z轴上正下负
    /// 359    181
    /// </summary>
    /// <param name="full"></param>
    /// <returns></returns>
    public static float Full2Half(float full)
    {
        if (full > 180)
        {
            return full - 360;
        }

        return full;
    }

    /// <summary>
    /// 让二维向量旋转某个角度,注（传入的角度是顺时针增加的）
    /// </summary>
    /// <param name="targetVec"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static Vector2 Vec2RotateAAngle(Vector2 targetVec, float angle)
    {
        if (targetVec.Equals(Vector2.zero))
        {
            return targetVec;
        }

        //数学中角度逆时针增加，故用负数
        float rad = Mathf.Deg2Rad * -angle;
        return new Vector2(
            targetVec.x * Mathf.Cos(rad) - targetVec.y * Mathf.Sin(rad),
            targetVec.x * Mathf.Sin(rad) + targetVec.y * Mathf.Cos(rad));
    }

    /// <summary>
    /// 判断点是否在长方体内
    /// </summary>
    /// <param name="centerX">中心点X</param>
    /// <param name="centerY">中心点Y</param>
    /// <param name="centerZ">中心点Z</param>
    /// <param name="halfLength">半长</param>
    /// <param name="halfWidth">半宽</param>
    /// <param name="halfHeight">半高</param>
    /// <param name="pointX">点的X值</param>
    /// <param name="pointY">点的Y值</param>
    /// <param name="pointZ">点的Z值</param>
    /// <returns></returns>
    public static bool IsPointInsideBox(double centerX, double centerY, double centerZ, double halfLength,
        double halfWidth, double halfHeight, double pointX, double pointY, double pointZ)
    {
        double minX = centerX - halfLength;
        double maxX = centerX + halfLength;
        double minY = centerY - halfWidth;
        double maxY = centerY + halfWidth;
        double minZ = centerZ - halfHeight;
        double maxZ = centerZ + halfHeight;

        if (pointX >= minX && pointX <= maxX && pointY >= minY && pointY <= maxY && pointZ >= minZ && pointZ <= maxZ)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 设置value的第Index的值为0或者1
    /// </summary>
    /// <param name="index">第几位</param>
    /// <param name="setValue">设置为0或者1</param>
    /// <param name="value">要修改的值</param>
    /// <returns></returns>
    public static ulong SetBitValue(int index, int setValue, ulong value)
    {
        if (setValue == 0)
        {
            value &= ~(1uL << index);
        }
        else
        {
            value |= (1uL << index);
        }

        return value;
    }

    /// <summary>
    /// 获取第index的值
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <returns>0或者1</returns>
    public static int GetBitValue(int index, ulong value)
    {
        return (value & (1uL << index)) != 0 ? 1 : 0;
    }

    /// <summary>
    /// 设置value的第Index的值为0或者1
    /// </summary>
    /// <param name="index">第几位</param>
    /// <param name="setValue">设置为0或者1</param>
    /// <param name="value">要修改的值</param>
    /// <returns></returns>
    public static int SetBitValue(int index, int setValue, int value)
    {
        if (setValue == 0)
        {
            value &= ~(1 << index);
        }
        else
        {
            value |= (1 << index);
        }

        return value;
    }

    /// <summary>
    /// 获取第index的值
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <returns>0或者1</returns>
    public static int GetBitValue(int index, int value)
    {
        return (value & (1 << index)) != 0 ? 1 : 0;
    }

    public static string GetFullPath(this Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        GetAllPath(transform, ref path);
        return path;
    }

    private static void GetAllPath(Transform transform, ref string path)
    {
        if (transform.parent != null)
        {
            path = transform.parent.name + "/" + path;
            GetAllPath(transform.parent, ref path);
        }
    }

    public static fp MoveSpeedLerp(fp speed1, fp speed2, fp t)
    {
        fp value = fpmath.lerp(speed1, speed2, t);

        //过渡到临界点
        if (fpmath.abs(value - speed1)< (fp)0.0001f)
        {
            value = speed2;
        }

        return value;
    }
    
    public static float MoveSpeedLerp(float speed1, float speed2, float t)
    {
        float value = Mathf.Lerp(speed1, speed2, t);

        //过渡到临界点
        if (MathF.Abs(value - speed1)< 0.0001f)
        {
            value = speed2;
        }

        return value;
    }
}