using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PrimitiveDetection
{
    public class CapsulePrimitive : BasePrimitive
    {
        // 中心点
        private fp3 _capsuleCenter;
        public fp3 CapsuleCenter => _capsuleCenter;

        // 旋转角
        private fpquaternion _capsuleQuaternion;
        public fpquaternion CapsuleQuaternion => _capsuleQuaternion;

        // 半径
        private fp _radius;
        public fp Radius => _radius;

        // 两端圆心距离
        private fp _height;
        public fp Height => _height;

        // 半高向量
        private fp3 _halfHeight;

        public fp3 HalfHeight => _halfHeight;

        // 一端圆心
        public fp3 CenterOne;

        // 另一端圆心
        public fp3 CenterTwo;

        // 圆心一到圆心二向量
        public fp3 CenterOneToTwo;

        // 圆心二到圆心一向量
        public fp3 CenterTwoToOne;

        public CapsulePrimitive()
        {
        }

        public static CapsulePrimitive Create(fp3 center, fpquaternion quaternion, fp radius, fp height)
        {
            CapsulePrimitive data = FPoolHelper.Get<CapsulePrimitive>();
            data._capsuleCenter = center;
            data._capsuleQuaternion = quaternion;
            data._radius = radius;
            data._height = height;
            data._halfHeight = new fp3(0, (fp)0.5f * height, 0);
            data.Transform.SetOrientationAndPos(quaternion, center);
            data.CenterOne = data.Transform * data._halfHeight;
            data.CenterTwo = data.Transform * -data._halfHeight;
            data.CenterOneToTwo = data.CenterTwo - data.CenterOne;
            data.CenterTwoToOne = data.CenterOne - data.CenterTwo;
            data.PrimitiveType = PrimitiveEnum.CapsulePrimitive;
            return data;
        }

        public override void OnInit(PrimitiveInfo info, out bool result)
        {
            base.OnInit(info, out result);
            this._capsuleCenter = info.Center;
            this._capsuleQuaternion = info.Quaternion;

            this._radius = info.Radius;

            if (this._radius <= 0)
            {
                this._radius = (fp)0.001f;
                result = false;
            }

            this._height = info.Height;

            if (this._height <= 0)
            {
                this._height = (fp)0.001f;
                result = false;
            }

            this._halfHeight = new fp3(0, (fp)0.5f * info.Height, 0);
            Transform.SetOrientationAndPos(info.Quaternion, info.Center);
            CenterOne = Transform * _halfHeight;
            CenterTwo = Transform * -_halfHeight;
            CenterOneToTwo = CenterTwo - CenterOne;
            CenterTwoToOne = CenterOne - CenterTwo;
            PrimitiveType = PrimitiveEnum.CapsulePrimitive;
        }

        public override void UpdateSelf(PrimitiveInfo info)
        {
            this._capsuleCenter = info.Center;
            this._capsuleQuaternion = info.Quaternion;

            this._radius = info.Radius;

            if (this._radius <= 0)
            {
                this._radius = (fp)0.001f;
            }

            this._height = info.Height;

            if (this._height <= 0)
            {
                this._height = (fp)0.001f;
            }

            this._halfHeight.x = 0;
            this._halfHeight.y = (fp)0.5f * info.Height;
            this._halfHeight.z = 0;
            Transform.SetOrientationAndPos(info.Quaternion, info.Center);
            CenterOne = Transform * _halfHeight;
            CenterTwo = Transform * -_halfHeight;
            CenterOneToTwo = CenterTwo - CenterOne;
            CenterTwoToOne = CenterOne - CenterTwo;
            PrimitiveType = PrimitiveEnum.CapsulePrimitive;
            base.UpdateSelf(info);
        }

#if UNITY_EDITOR

        public override void PrimitiveDebug(Color color)
        {
            base.PrimitiveDebug(color);

            /*
            if (!this.ShowInfo.isDrawShow)
            {
                return;
            }
            */
            
            DrawDebugTools.DrawCapsule(fpmath1.Fp3ToVector3(_capsuleCenter), (float)(_height / 2 + _radius), 
                (float)_radius, _capsuleQuaternion, color,
                this.leftTime);
        }

        public override void OnDrawGizmos()
        {
            //base.OnDrawGizmos(color);
            if (!this.ShowInfo.isDrawShow)
            {
                return;
            }

            Handles.color = this.ShowInfo.drawColor;

            // 绘制垂直于顶部和底部圆的两个圆
            fp3 sideCenter1 = this.CenterOne + this.CapsuleQuaternion * fp3.zero * Radius;
            fp3 sideCenter2 = this.CenterTwo + this.CapsuleQuaternion * fp3.zero * Radius;
            Handles.DrawWireDisc(fpmath1.Fp3ToVector3(sideCenter1), fpmath1.Fp3ToVector3(this.CapsuleQuaternion * fpmath1.up()), (float)Radius);
            Handles.DrawWireDisc(fpmath1.Fp3ToVector3(sideCenter2), fpmath1.Fp3ToVector3(this.CapsuleQuaternion * fpmath1.up()), (float)Radius);

            Vector3 centerOne = fpmath1.Fp3ToVector3(CenterOne);
            Vector3 centerTwo = fpmath1.Fp3ToVector3(CenterTwo);
            Vector3 axisUp = fpmath1.Fp3ToVector3(CapsuleQuaternion * fpmath1.up()).normalized;
            Vector3 axisRight = fpmath1.Fp3ToVector3(CapsuleQuaternion * fpmath1.right()).normalized;
            Vector3 axisForward = fpmath1.Fp3ToVector3(CapsuleQuaternion * fpmath1.forward()).normalized;
            float radius = (float)Radius;

            Handles.DrawLine(centerOne + axisForward * radius, centerTwo + axisForward * radius);
            Handles.DrawLine(centerOne - axisForward * radius, centerTwo - axisForward * radius);
            Handles.DrawLine(centerOne + axisRight * radius, centerTwo + axisRight * radius);
            Handles.DrawLine(centerOne - axisRight * radius, centerTwo - axisRight * radius);

            Handles.DrawWireArc(centerOne, axisRight, axisUp, -180f, radius);
            Handles.DrawWireArc(centerOne, axisForward, axisUp, 180f, radius);
            Handles.DrawWireArc(centerTwo, axisRight, -axisUp, -180f, radius);
            Handles.DrawWireArc(centerTwo, axisForward, -axisUp, 180f, radius);
        }

#endif
        public override bool InternalCheckPrimitive()
        {
            if (_height <= 0 || _radius <= 0)
            {
                //Log.Error("胶囊体形状的高度参数或半径参数不符合规范。");
                return false;
            }

            return true;
        }

        public override void OnDispose()
        {
            FPoolHelper.Release<CapsulePrimitive>(this);
        }
    }
}
