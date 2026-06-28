// 方法3：射线与OBB（定向包围盒）相交

using UnityEngine;

public static class RayOBBIntersection
{
    // OBB结构体
    public struct OBB
    {
        public Vector3 center;
        public Vector3 size;      // 立方体尺寸
        public Quaternion rotation; // 旋转
        public Vector3[] axes;     // 三个本地轴
        
        public OBB(Vector3 center, Vector3 size, Quaternion rotation)
        {
            this.center = center;
            this.size = size;
            this.rotation = rotation;
            
            // 计算三个本地轴
            axes = new Vector3[3];
            axes[0] = rotation * Vector3.right;
            axes[1] = rotation * Vector3.up;
            axes[2] = rotation * Vector3.forward;
        }
        
        // 获取8个顶点
        public Vector3[] GetVertices()
        {
            Vector3[] vertices = new Vector3[8];
            Vector3 halfSize = size * 0.5f;
            
            // 8个顶点的本地坐标
            Vector3[] localVertices = new Vector3[8]
            {
                new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x,  halfSize.y,  halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y,  halfSize.z)
            };
            
            // 变换到世界坐标
            for (int i = 0; i < 8; i++)
            {
                vertices[i] = rotation * localVertices[i] + center;
            }
            
            return vertices;
        }
    }
    
    // 分离轴定理（SAT）检测射线与OBB相交
    public static bool RayIntersectOBB(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        OBB obb,
        out float tMin,
        out Vector3 intersectionPoint,
        out Vector3 normal)
    {
        tMin = float.MaxValue;
        intersectionPoint = Vector3.zero;
        normal = Vector3.zero;
        
        // 将射线变换到OBB的局部坐标系
        Matrix4x4 worldToLocal = Matrix4x4.TRS(obb.center, obb.rotation, Vector3.one).inverse;
        Vector3 localOrigin = worldToLocal.MultiplyPoint(rayOrigin);
        Vector3 localDirection = worldToLocal.MultiplyVector(rayDirection).normalized;
        
        // 现在在局部坐标系中，OBB是一个AABB
        Vector3 halfSize = obb.size * 0.5f;
        Vector3 boxMin = -halfSize;
        Vector3 boxMax = halfSize;
        
        // 使用slab方法计算局部坐标系中的交点
        if (RayIntersectAABB(localOrigin, localDirection, boxMin, boxMax, out tMin, out float tMax))
        {
            // 计算局部交点
            Vector3 localIntersection = localOrigin + localDirection * tMin;
            
            // 变换回世界坐标系
            intersectionPoint = obb.rotation * localIntersection + obb.center;
            
            // 计算碰撞法线（在局部坐标系中判断）
            normal = GetOBBColissionNormal(localIntersection, halfSize);
            
            // 将法线变换到世界坐标系
            normal = obb.rotation * normal;
            
            return true;
        }
        
        return false;
    }
    
    // 计算OBB碰撞法线（局部坐标系）
    private static Vector3 GetOBBColissionNormal(Vector3 localHit, Vector3 halfSize)
    {
        // 计算到每个面的距离
        float distX = Mathf.Abs(Mathf.Abs(localHit.x) - halfSize.x);
        float distY = Mathf.Abs(Mathf.Abs(localHit.y) - halfSize.y);
        float distZ = Mathf.Abs(Mathf.Abs(localHit.z) - halfSize.z);
        
        Vector3 normal = Vector3.zero;
        
        // 找到最小的距离
        if (distX < distY && distX < distZ)
        {
            normal.x = Mathf.Sign(localHit.x);
        }
        else if (distY < distX && distY < distZ)
        {
            normal.y = Mathf.Sign(localHit.y);
        }
        else
        {
            normal.z = Mathf.Sign(localHit.z);
        }
        
        return normal;
    }
    
    // 简化版的射线与OBB相交（不使用矩阵变换）
    public static bool RayIntersectOBBFast(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        OBB obb,
        out float t)
    {
        t = 0;
        float tMin = float.MinValue;
        float tMax = float.MaxValue;
        
        Vector3 p = obb.center - rayOrigin;
        
        // 对每个轴进行测试
        for (int i = 0; i < 3; i++)
        {
            Vector3 axis = obb.axes[i];
            float e = Vector3.Dot(axis, p);
            float f = Vector3.Dot(rayDirection, axis);
            
            if (Mathf.Abs(f) > 0.001f)
            {
                float t1 = (e + obb.size[i] * 0.5f) / f;
                float t2 = (e - obb.size[i] * 0.5f) / f;
                
                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }
                
                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);
                
                if (tMin > tMax) return false;
                if (tMax < 0) return false;
            }
            else
            {
                // 射线与轴平行
                if (-e - obb.size[i] * 0.5f > 0 || -e + obb.size[i] * 0.5f < 0)
                    return false;
            }
        }
        
        t = tMin > 0 ? tMin : tMax;
        return true;
    }
    
     // 方法1：使用slab方法计算射线与AABB的交点
    public static bool RayIntersectAABB(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        Vector3 boxMin,
        Vector3 boxMax,
        out float tMin,
        out float tMax)
    {
        tMin = 0;
        tMax = float.MaxValue;
        
        // 处理射线方向分量为0的情况
        Vector3 invDir = new Vector3(
            Mathf.Abs(rayDirection.x) > Mathf.Epsilon ? 1.0f / rayDirection.x : float.MaxValue,
            Mathf.Abs(rayDirection.y) > Mathf.Epsilon ? 1.0f / rayDirection.y : float.MaxValue,
            Mathf.Abs(rayDirection.z) > Mathf.Epsilon ? 1.0f / rayDirection.z : float.MaxValue
        );
        
        // 分别计算三个轴的交点参数
        float tx1 = (boxMin.x - rayOrigin.x) * invDir.x;
        float tx2 = (boxMax.x - rayOrigin.x) * invDir.x;
        
        float ty1 = (boxMin.y - rayOrigin.y) * invDir.y;
        float ty2 = (boxMax.y - rayOrigin.y) * invDir.y;
        
        float tz1 = (boxMin.z - rayOrigin.z) * invDir.z;
        float tz2 = (boxMax.z - rayOrigin.z) * invDir.z;
        
        // 获取每个轴的最小和最大t值
        float tminX = Mathf.Min(tx1, tx2);
        float tmaxX = Mathf.Max(tx1, tx2);
        
        float tminY = Mathf.Min(ty1, ty2);
        float tmaxY = Mathf.Max(ty1, ty2);
        
        float tminZ = Mathf.Min(tz1, tz2);
        float tmaxZ = Mathf.Max(tz1, tz2);
        
        // 计算全局的tMin和tMax
        tMin = Mathf.Max(tminX, tminY, tminZ);
        tMax = Mathf.Min(tmaxX, tmaxY, tmaxZ);
        
        // 检查是否相交
        if (tMin > tMax) return false;
        if (tMax < 0) return false; // 立方体在射线后面
        
        // 如果tMin < 0，说明射线起点在立方体内部
        if (tMin < 0) tMin = tMax;
        
        return true;
    }
    
    // 方法2：获取具体的交点坐标
    public static bool GetRayCubeIntersectionPoint(
        Vector3 rayOrigin,
        Vector3 rayDirection,
        Vector3 cubeCenter,
        Vector3 cubeSize,
        out Vector3 intersectionPoint,
        out Vector3 normal)
    {
        intersectionPoint = Vector3.zero;
        normal = Vector3.zero;
        
        // 计算立方体的最小和最大点
        Vector3 halfSize = cubeSize * 0.5f;
        Vector3 boxMin = cubeCenter - halfSize;
        Vector3 boxMax = cubeCenter + halfSize;
        
        // 计算交点参数
        if (RayIntersectAABB(rayOrigin, rayDirection, boxMin, boxMax, out float tMin, out float tMax))
        {
            // 使用最近的交点
            intersectionPoint = rayOrigin + rayDirection * tMin;
            
            // 计算碰撞法线（判断撞到了哪个面）
            normal = GetCollisionNormal(intersectionPoint, cubeCenter, cubeSize);
            
            return true;
        }
        
        return false;
    }
    
    // 计算碰撞法线
    public static Vector3 GetCollisionNormal(Vector3 hitPoint, Vector3 cubeCenter, Vector3 cubeSize)
    {
        Vector3 halfSize = cubeSize * 0.5f;
        Vector3 localHit = hitPoint - cubeCenter;
        Vector3 normal = Vector3.zero;
        
        // 计算到每个面的距离
        float distX = Mathf.Abs(Mathf.Abs(localHit.x) - halfSize.x);
        float distY = Mathf.Abs(Mathf.Abs(localHit.y) - halfSize.y);
        float distZ = Mathf.Abs(Mathf.Abs(localHit.z) - halfSize.z);
        
        // 找到最小的距离（最接近的面）
        if (distX < distY && distX < distZ)
        {
            normal = new Vector3(Mathf.Sign(localHit.x), 0, 0);
        }
        else if (distY < distX && distY < distZ)
        {
            normal = new Vector3(0, Mathf.Sign(localHit.y), 0);
        }
        else
        {
            normal = new Vector3(0, 0, Mathf.Sign(localHit.z));
        }
        
        return normal;
    }
}