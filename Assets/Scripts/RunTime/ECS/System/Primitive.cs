using System;
using System.Collections.Generic;
using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 几何相交检测系统
/// </summary>
public class Primitive
{
    private static bool CheckPrimitiveType(PrimitiveEnum primitiveEnum, out BasePrimitive basePrimitive)
    {
        switch (primitiveEnum)
        {
            case PrimitiveEnum.NONE:
                basePrimitive = null;
                return false;
            case PrimitiveEnum.BoxPrimitive:
                basePrimitive = FPoolHelper.Get<BoxPrimitive>();
                return true;
            case PrimitiveEnum.CapsulePrimitive:
                basePrimitive = FPoolHelper.Get<CapsulePrimitive>();
                return true;
            case PrimitiveEnum.SpherePrimitive:
                basePrimitive = FPoolHelper.Get<SpherePrimitive>();
                return true;
            case PrimitiveEnum.SectorPrimitive:
                basePrimitive = FPoolHelper.Get<SectorPrimitive>();
                return true;
            case PrimitiveEnum.AnnulusPrimitive:
                basePrimitive = FPoolHelper.Get<AnnulusPrimitive>();
                return true;
            default:
                basePrimitive = null;
                return false;
        }
    }

    public static bool IsIntersect(BasePrimitive one, BasePrimitive two)
    {
        bool result = false;

        if (one == null || two == null)
        {
            return false;
        }

        if (!one.InternalCheckPrimitive() || !two.InternalCheckPrimitive())
        {
            Debug.LogError("相交检测失败，生成了不符合规范的几何形状，检查参数。");
            
            return false;
        }

        switch (one.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogError("无几何形状");
                return false;
            case PrimitiveEnum.SectorPrimitive:
                SectorDetect((SectorPrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.BoxPrimitive:
                BoxDetect((BoxPrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                CapsuleDetect((CapsulePrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.SpherePrimitive:
                SphereDetect((SpherePrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.AnnulusPrimitive:
                AnnulusDetect((AnnulusPrimitive)one, two, ref result);
                break;
            default:
                Debug.LogWarning($"{one}几何形状未知，无法检测");
                return false;
        }

        return result;
    }

    public static bool IsIntersect(PrimitiveInfo p1, PrimitiveInfo p2)
    {
        BasePrimitive one = CreatePrimitive(p1);
        BasePrimitive two = CreatePrimitive(p2);
        bool result = false;

        // PrimitiveLog.Log(one, Color.yellow);
        // PrimitiveLog.Log(two, Color.red);

        // one.PrimitiveDebug(Color.yellow);
        // two.PrimitiveDebug(Color.red);

        if (one == null || two == null)
        {
            return false;
        }

        if (!one.InternalCheckPrimitive() || !two.InternalCheckPrimitive())
        {
            Debug.LogError("相交检测失败，生成了不符合规范的几何形状，检查参数。");
            return false;
        }

        switch (one.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogError($"{p1}上无几何形状");
                return false;
            case PrimitiveEnum.SectorPrimitive:
                SectorDetect((SectorPrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.BoxPrimitive:
                BoxDetect((BoxPrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                CapsuleDetect((CapsulePrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.SpherePrimitive:
                SphereDetect((SpherePrimitive)one, two, ref result);
                break;
            case PrimitiveEnum.AnnulusPrimitive:
                AnnulusDetect((AnnulusPrimitive)one, two, ref result);
                break;
            default:
                Debug.LogWarning($"{one}几何形状未知，无法检测");
                return false;
        }

        one.OnDispose();
        two.OnDispose();

        return result;
    }

    public static bool IsIntersect(fp3 point, fp3 direct, BasePrimitive one)
    {
        // BasePrimitive one = CreatePrimitive(p1);
        bool result = false;

        if (one == null)
        {
            return false;
        }

        if (!one.InternalCheckPrimitive())
        {
            Debug.LogError("相交检测失败，生成了不符合规范的几何形状，检查参数。");
            return false;
        }

        switch (one.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogError("无几何形状");
                return false;
            case PrimitiveEnum.SectorPrimitive:
                Debug.LogWarning("扇形与射线无碰撞检测");
                break;
            case PrimitiveEnum.BoxPrimitive:
                result = IntersectionDetection.BoxAndRay(point, direct, (BoxPrimitive)one);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                result = IntersectionDetection.CapsuleAndRay(point, direct, (CapsulePrimitive)one);
                break;
            case PrimitiveEnum.SpherePrimitive:
                result = IntersectionDetection.SphereAndRay(point, direct, (SpherePrimitive)one);
                break;
            case PrimitiveEnum.AnnulusPrimitive:
                Debug.LogWarning("环形与射线无碰撞检测");
                break;
            default:
                Debug.LogWarning($"{one}几何形状未知，无法检测");
                return false;
        }

        // one.OnDispose();

        return result;
    }

    public static BasePrimitive CreatePrimitive(PrimitiveInfo info)
    {
        if (!CheckPrimitiveType(info.Type, out BasePrimitive one))
        {
            return null;
        }

        one.OnInit(info, out bool result);

        if (!result)
        {
            Debug.LogError($"相交检测中创建{one.PrimitiveType}出错，检查配置的半径、角度等参数信息。");
        }

        return one;
    }

    private static void BoxDetect(BoxPrimitive box, BasePrimitive p2, ref bool result)
    {
        switch (p2.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogWarning($"{p2}上无几何形状");
                return;
            case PrimitiveEnum.SectorPrimitive:
                result = IntersectionDetection.SectorAndBox((SectorPrimitive)p2, box,
                    out PolygonPrimitive polygonPrimitive);
                break;
            case PrimitiveEnum.BoxPrimitive:
                result = IntersectionDetection.BoxAndBox(box, (BoxPrimitive)p2);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                result = IntersectionDetection.CapsuleAndBox((CapsulePrimitive)p2, box);
                break;
            case PrimitiveEnum.SpherePrimitive:
                result = IntersectionDetection.BoxAndSphere(box, (SpherePrimitive)p2);
                break;
            case PrimitiveEnum.AnnulusPrimitive:
                result = IntersectionDetection.AnnulusAndBox((AnnulusPrimitive)p2, box);
                break;
            default:
                Debug.LogWarning($"{p2}几何形状未知，无法检测");
                return;
        }
    }

    private static void SectorDetect(SectorPrimitive sector, BasePrimitive p2, ref bool result)
    {
        switch (p2.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogWarning($"{p2}上无几何形状");
                return;
            case PrimitiveEnum.SectorPrimitive:
                // result = IntersectionDetection.SectorAndSector((SectorPrimitive) p2, sector);
                Debug.LogWarning($"扇形与扇形无检测。");
                break;
            case PrimitiveEnum.BoxPrimitive:
                result = IntersectionDetection.SectorAndBox(sector, (BoxPrimitive)p2,
                    out PolygonPrimitive polygonPrimitive);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                result = IntersectionDetection.SectorAndCapsule(sector, (CapsulePrimitive)p2, out fp3 bestA);
                break;
            case PrimitiveEnum.SpherePrimitive:
                result = IntersectionDetection.SectorAndSphere(sector, (SpherePrimitive)p2);
                break;
            default:
                Debug.LogWarning($"{p2}几何形状未知，无法检测");
                break;
        }
    }

    private static void CapsuleDetect(CapsulePrimitive capsule, BasePrimitive p2, ref bool result)
    {
        switch (p2.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogWarning($"{p2}上无几何形状");
                return;
            case PrimitiveEnum.SectorPrimitive:
                result = IntersectionDetection.SectorAndCapsule((SectorPrimitive)p2, capsule, out fp3 bestA);
                break;
            case PrimitiveEnum.BoxPrimitive:
                result = IntersectionDetection.CapsuleAndBox(capsule, (BoxPrimitive)p2);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                result = IntersectionDetection.CapsuleAndCapsule((CapsulePrimitive)p2, capsule);
                break;
            case PrimitiveEnum.SpherePrimitive:
                result = IntersectionDetection.CapsuleAndSphere(capsule, (SpherePrimitive)p2);
                break;
            case PrimitiveEnum.AnnulusPrimitive:
                result = IntersectionDetection.AnnulusAndCapusle((AnnulusPrimitive)p2, capsule);
                break;
            default:
                Debug.LogWarning($"{p2}几何形状未知，无法检测");
                break;
        }
    }

    private static void SphereDetect(SpherePrimitive sphere, BasePrimitive p2, ref bool result)
    {
        switch (p2.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogWarning($"{p2}上无几何形状");
                return;
            case PrimitiveEnum.SectorPrimitive:
                result = IntersectionDetection.SectorAndSphere((SectorPrimitive)p2, sphere);
                break;
            case PrimitiveEnum.BoxPrimitive:
                result = IntersectionDetection.BoxAndSphere((BoxPrimitive)p2, sphere);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                result = IntersectionDetection.CapsuleAndSphere((CapsulePrimitive)p2, sphere);
                break;
            case PrimitiveEnum.SpherePrimitive:
                result = IntersectionDetection.SphereAndSphere(sphere, (SpherePrimitive)p2);
                break;
            case PrimitiveEnum.AnnulusPrimitive:
                result = IntersectionDetection.AnnulusAndSphere((AnnulusPrimitive)p2, sphere);
                break;
            default:
                Debug.LogWarning($"{p2}几何形状未知，无法检测");
                break;
        }
    }

    private static void AnnulusDetect(AnnulusPrimitive p1, BasePrimitive p2, ref bool result)
    {
        switch (p2.PrimitiveType)
        {
            case PrimitiveEnum.NONE:
                Debug.LogWarning($"{p2}上无几何形状");
                return;
            case PrimitiveEnum.BoxPrimitive:
                result = IntersectionDetection.AnnulusAndBox(p1, (BoxPrimitive)p2);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                result = IntersectionDetection.AnnulusAndCapusle(p1, (CapsulePrimitive)p2);
                break;
            case PrimitiveEnum.SpherePrimitive:
                result = IntersectionDetection.AnnulusAndSphere(p1, (SpherePrimitive)p2);
                break;
            default:
                Debug.LogWarning($"{p2}几何形状未知，无法检测");
                break;
        }
    }

    /// <summary>
    /// 初始化形状的参数
    /// </summary>
    /// <param name="info"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public static PrimitiveInfo InitPrimitiveInfo(ref PrimitiveInfo info, List<fp> param)
    {
        switch (info.Type)
        {
            case PrimitiveEnum.NONE:
                
                Debug.LogWarning($"几何形状未知");
                
                break;
            case PrimitiveEnum.BoxPrimitive:
                if (param.Count < 3)
                {
                    Debug.LogError($"方形受击盒参数少于3个");
                    
                    break;
                }

                info.BoxSize = new fp3(param[0], param[1], param[2]);
                break;
            case PrimitiveEnum.CapsulePrimitive:
                if (param.Count < 2)
                {
                    Debug.LogError($"胶囊受击盒参数少于2个");
                    break;
                }

                info.Radius = param[0];
                info.Height = param[1];
                break;
            case PrimitiveEnum.SpherePrimitive:
                if (param.Count < 1)
                {
                    Debug.LogError($"球形受击盒参数少于1个");
                    break;
                }

                info.Radius = param[0];
                break;
            case PrimitiveEnum.SectorPrimitive:
                if (param.Count < 2)
                {
                    Debug.LogError($"扇形受击盒参数少于2个");
                    break;
                }

                info.Radius = param[0];
                info.Angle = param[1];
                break;
            case PrimitiveEnum.AnnulusPrimitive:
                if (param.Count < 3)
                {
                    Debug.LogError($"环形参数少于3个");
                    break;
                }

                info.InternalRadius = param[0];
                info.Radius = param[1];
                info.Angle = param[2];
                break;
        }

        return info;
    }
}