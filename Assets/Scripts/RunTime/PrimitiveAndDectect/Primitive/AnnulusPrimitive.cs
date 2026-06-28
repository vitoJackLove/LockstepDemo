using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace PrimitiveDetection
{
    /// <summary>
    /// 对称轴为自身坐标系的x轴的环形
    /// </summary>
    public class AnnulusPrimitive : BasePrimitive
    {
        /// <summary>
        /// 中心点
        /// </summary>
        private fp3 _annulusCenter;

        public fp3 AnnulusCenter => _annulusCenter;

        /// <summary>
        /// 环形对称轴的旋转角
        /// </summary>
        private fpquaternion _quaternion;

        public fpquaternion Quaternion => _quaternion;

        private fp _angle;

        /// <summary>
        /// 圆心角(弧度）
        /// </summary>
        public fp Angle => _angle;

        /// <summary>
        /// 外径
        /// </summary>
        private fp _internalDiameter;

        public fp InternalDiameter => _internalDiameter;

        /// <summary>
        /// 内径
        /// </summary>
        private fp _outerDiameter;

        public fp OuterDiameter => _outerDiameter;

        /// <summary>
        /// 世界坐标下环形的对称轴
        /// </summary>
        public fp3 LocalUnitX => this.Transform * fpmath1.right() - _annulusCenter;

        /// <summary>
        /// 世界坐标下环形本地坐标系Y轴,环形平面的法向量
        /// </summary>
        public fp3 LocalUnitY => this.Transform * fpmath1.up() - _annulusCenter;

        /// <summary>
        /// 世界坐标下环形本地坐标系z轴
        /// </summary>
        public fp3 LocalUnitZ => this.Transform * fpmath1.forward() - _annulusCenter;

        /// <summary>
        /// 环形内径外径的四个顶点，本地坐标
        /// [0,x]:内径 [1,x]:外径
        /// </summary>
        private fp3[,] _localEdgeVers;

        /// <summary>
        /// 环形内径外径的四个顶点，世界坐标
        /// [0,x]:内径 [1,x]:外径
        /// </summary>
        public fp3[,] EdgeVertices{ get; private set; }

        public static AnnulusPrimitive Create(fp3 annulusCenter, fpquaternion quaternion,
            fp angle, fp outerDiameter, fp internalDiameter)
        {
            AnnulusPrimitive data = FPoolHelper.Get<AnnulusPrimitive>();
            data._annulusCenter = annulusCenter;
            data._quaternion = quaternion;
            data._angle = angle;

            if (angle < 0){
                data._angle = (fp)0.001f;
            }

            if (angle > 360){
                data._angle = 360;
            }

            data._angle = data._angle / 180 * (fp)Mathf.PI;
            data._outerDiameter = outerDiameter < 0 ? (fp)0.001f : outerDiameter;
            data._internalDiameter = internalDiameter < 0 ? (fp)0.001f : outerDiameter;
            data.Transform.SetOrientationAndPos(data._quaternion, annulusCenter);
            data._localEdgeVers = new fp3[2, 2];
            data.EdgeVertices = new fp3[2, 2];
            data.GetLocalVertices();
            data.PrimitiveType = PrimitiveEnum.AnnulusPrimitive;
            return data;
        }

        public override void OnInit(PrimitiveInfo info, out bool result){
            base.OnInit(info, out result);
            this._annulusCenter = info.Center;
            this._quaternion = info.Quaternion;
            this._quaternion *= Quaternion.Euler(new fp3(0, -90, 0));
            this._angle = info.Angle;

            if (info.Angle <= 0){
                this._angle = (fp)0.001f;
                result = false;
            }

            if (info.Angle > 360){
                this._angle = 360;
                result = false;
            }

            this._angle = this._angle / 180 * (fp)Mathf.PI;

            if (info.Radius < 0){
                result = false;
            }

            this._outerDiameter = info.Radius < 0 ? (fp)0.001f : info.Radius;

            if (info.InternalRadius < 0){
                result = false;
            }

            this._internalDiameter = info.InternalRadius < 0 ? (fp)0.001f : info.InternalRadius;
            Transform.SetOrientationAndPos(_quaternion, _annulusCenter);
            _localEdgeVers = new fp3[2, 2];
            EdgeVertices = new fp3[2, 2];
            GetLocalVertices();
            PrimitiveType = PrimitiveEnum.AnnulusPrimitive;
        }

        public override void UpdateSelf(PrimitiveInfo info)
        {
            this._annulusCenter = info.Center;
            this._quaternion = info.Quaternion;
            this._quaternion *= Quaternion.Euler(new fp3(0, -90, 0));
            this._angle = info.Angle;

            if (info.Angle <= 0){
                this._angle = (fp)0.001f;
            }

            if (info.Angle > 360){
                this._angle = 360;
            }

            this._angle = this._angle / 180 * (fp)Mathf.PI;

            this._outerDiameter = info.Radius < 0 ? (fp)0.001f : info.Radius;

            this._internalDiameter = info.InternalRadius < 0 ? (fp)0.001f : info.InternalRadius;
            Transform.SetOrientationAndPos(_quaternion, _annulusCenter);
            GetLocalVertices();
            PrimitiveType = PrimitiveEnum.AnnulusPrimitive;
            base.UpdateSelf(info);
        }

        /// <summary>
        /// 将世界坐标系下的坐标转换为自身坐标系下的坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public fp3 Transform2Local(fp3 point){
            return Transform.TransformInverse(point);
        }

        /// <summary>
        /// 将自身坐标系下某个坐标转换为世界坐标系下的坐标
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public fp3 Transform2World(fp3 point){
            return Transform.Transform(point);
        }

        private void GetLocalVertices(){
            // 内径
            fp3 ver1 = new fp3(_internalDiameter * fpmath.cos(_angle / 2), 0, _internalDiameter * fpmath.sin(_angle / 2));
            fp3 ver2 = new fp3(_internalDiameter * fpmath.cos(_angle / 2), 0, -(_internalDiameter * fpmath.sin(_angle / 2)));
            _localEdgeVers[0, 0] = ver1;
            _localEdgeVers[0, 1] = ver2;
            EdgeVertices[0, 0] = Transform * ver1;
            EdgeVertices[0, 1] = Transform * ver2;

            // 外径
            fp3 ver3 = new fp3(_outerDiameter * fpmath.cos(_angle / 2), 0, _outerDiameter * fpmath.sin(_angle / 2));
            fp3 ver4 = new fp3(_outerDiameter * fpmath.cos(_angle / 2), 0, -(_outerDiameter * fpmath.sin(_angle / 2)));
            _localEdgeVers[1, 0] = ver3;
            _localEdgeVers[1, 1] = ver4;
            EdgeVertices[1, 0] = Transform * ver3;
            EdgeVertices[1, 1] = Transform * ver4;
        }
#if UNITY_EDITOR

        public override void PrimitiveDebug(Color color){
            base.PrimitiveDebug(color);

            if (EdgeVertices == null || _localEdgeVers == null)
            {
                return;
            }

            DrawDebugTools.DrawLine(fpmath1.Fp3ToVector3(EdgeVertices[0, 0]),
                fpmath1.Fp3ToVector3(EdgeVertices[1, 0]), color, this.leftTime);
            DrawDebugTools.DrawLine(fpmath1.Fp3ToVector3(EdgeVertices[0, 1]),
                fpmath1.Fp3ToVector3(EdgeVertices[1, 1]), color, this.leftTime);

            // 环形的内外径圆弧
            int vertexCount = 10;
            fp deltaTheta = _angle / vertexCount;
            fp theta = -_angle / 2;
            fp3 oldPos = _localEdgeVers[0, 1];
            fp3 oldPosOuter = _localEdgeVers[1, 1];

            for (int i = 0; i <= vertexCount; i++){
                fp3 pos = new fp3(_internalDiameter * fpmath.cos(theta), 0, _internalDiameter * fpmath.sin(theta));
                DrawDebugTools.DrawLine(fpmath1.Fp3ToVector3(Transform * oldPos),
                    fpmath1.Fp3ToVector3(Transform * pos), color, this.leftTime);
                oldPos = pos;

                fp3 posOuter = new fp3(_outerDiameter * fpmath.cos(theta), 0, _outerDiameter * fpmath.sin(theta));
                DrawDebugTools.DrawLine(fpmath1.Fp3ToVector3(Transform * oldPosOuter),
                    fpmath1.Fp3ToVector3(Transform * posOuter), color, this.leftTime);
                oldPosOuter = posOuter;

                theta += deltaTheta;
            }
        }

        public override void OnDrawGizmos(){
            if (!ShowInfo.isDrawShow || EdgeVertices == null || _localEdgeVers == null)
            {
                return;
            }

            Gizmos.color = ShowInfo.drawColor;

            Gizmos.DrawLine(fpmath1.Fp3ToVector3(EdgeVertices[0, 0]), fpmath1.Fp3ToVector3(EdgeVertices[1, 0]));
            Gizmos.DrawLine(fpmath1.Fp3ToVector3(EdgeVertices[0, 1]), fpmath1.Fp3ToVector3(EdgeVertices[1, 1]));

            // 环形的内外径圆弧
            int vertexCount = 24;
            fp deltaTheta = _angle / vertexCount;
            fp theta = -_angle / 2;
            fp3 oldPos = _localEdgeVers[0, 1];
            fp3 oldPosOuter = _localEdgeVers[1, 1];

            for (int i = 0; i <= vertexCount; i++){
                fp3 pos = new fp3(_internalDiameter * fpmath.cos(theta), 0, _internalDiameter * fpmath.sin(theta));
                Gizmos.DrawLine(fpmath1.Fp3ToVector3(Transform * oldPos), fpmath1.Fp3ToVector3(Transform * pos));
                oldPos = pos;

                fp3 posOuter = new fp3(_outerDiameter * fpmath.cos(theta), 0, _outerDiameter * fpmath.sin(theta));
                Gizmos.DrawLine(fpmath1.Fp3ToVector3(Transform * oldPosOuter),
                    fpmath1.Fp3ToVector3(Transform * posOuter));
                oldPosOuter = posOuter;

                theta += deltaTheta;
            }

            // 环形的本地坐标系xyz
            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(_annulusCenter, _outerDiameter * LocalUnitX + _annulusCenter);

            //Gizmos.color = color;
            //Gizmos.DrawLine(_annulusCenter, _outerDiameter * LocalUnitY + _annulusCenter);
        }
#endif
        public override bool InternalCheckPrimitive(){
            if (_angle <= 0 || _internalDiameter <= 0 || _outerDiameter <= 0 || _internalDiameter > _outerDiameter){
                //Log.Error("环形几何形状的扫掠角参数、内径参数或外径参数不符合规范。");
                return false;
            }

            return true;
        }
        
        public override void OnDispose()
        {
            FPoolHelper.Release<AnnulusPrimitive>(this);
        }

        public override void Clear(){
            base.Clear();
            _localEdgeVers = null;
            EdgeVertices = null;
        }
    }

}
