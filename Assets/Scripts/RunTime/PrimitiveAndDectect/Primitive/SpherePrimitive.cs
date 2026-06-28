using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace PrimitiveDetection
{

    public class SpherePrimitive : BasePrimitive
    {
        /// <summary>
        /// 中心点
        /// </summary>
        private fp3 _sphereCenter;

        public fp3 SphereCenter => _sphereCenter;

        /// <summary>
        /// 半径
        /// </summary>
        private fp _radius;

        /// <summary>
        /// 方向
        /// </summary>
        private fpquaternion _sphereQuaternion;

        private fp3 scale;
        public fp3 Scale => scale;

        public fp Radius => _radius;

        public SpherePrimitive()
        {
        }

        public static SpherePrimitive Create(fp3 sphereCenter, fp radius, fpquaternion sphereQuaternion)
        {
            SpherePrimitive data = FPoolHelper.Get<SpherePrimitive>();
            data._sphereCenter = sphereCenter;
            data._radius = radius;
            data._sphereQuaternion = sphereQuaternion;
            data.Transform.SetOrientationAndPos(data._sphereQuaternion, data._sphereCenter);
            data.PrimitiveType = PrimitiveEnum.SpherePrimitive;
            return data;
        }

        public override void OnInit(PrimitiveInfo info, out bool result)
        {
            base.OnInit(info, out result);
            this._sphereCenter = info.Center;
            this._radius = info.Radius;

            if (_radius <= 0)
            {
                _radius = (fp)0.001f;
                result = false;
            }

            this._sphereQuaternion = info.Quaternion;
            Transform.SetOrientationAndPos(this._sphereQuaternion, this._sphereCenter);
            PrimitiveType = PrimitiveEnum.SpherePrimitive;
        }

        public override void UpdateSelf(PrimitiveInfo info)
        {
            this._sphereCenter = info.Center;
            this._radius = info.Radius;

            if (_radius <= 0)
            {
                _radius = (fp)0.001f;
            }

            this._sphereQuaternion = info.Quaternion;
            Transform.SetOrientationAndPos(this._sphereQuaternion, this._sphereCenter);
            PrimitiveType = PrimitiveEnum.SpherePrimitive;
            base.UpdateSelf(info);
        }

#if UNITY_EDITOR

        public override void PrimitiveDebug(Color color)
        {
            base.PrimitiveDebug(color);
            DrawDebugTools.DrawSphere(fpmath1.Fp3ToVector3(_sphereCenter), (float)_radius, 10, color, this.leftTime);
        }

        public override void OnDrawGizmos()
        {
            if (!this.ShowInfo.isDrawShow)
            {
                return;
            }
            Gizmos.color = this.ShowInfo.drawColor;

            Gizmos.DrawWireSphere(fpmath1.Fp3ToVector3(_sphereCenter), (float)_radius);
        }
#endif
        public override bool InternalCheckPrimitive()
        {
            if (_radius <= 0)
            {
                //Log.Error("球体几何形状的半径参数不符合规范。");
                return false;
            }

            return true;
        }

        public override void OnDispose()
        {
            FPoolHelper.Release<SpherePrimitive>(this);
        }
    }
}