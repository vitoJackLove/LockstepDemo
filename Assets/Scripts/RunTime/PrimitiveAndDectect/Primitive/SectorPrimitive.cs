using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace PrimitiveDetection
{

    /// <summary>
    /// 对称轴为自身坐标系的x轴的扇形
    /// </summary>
    public class SectorPrimitive : BasePrimitive
    {

        /// <summary>
        /// 中心点
        /// </summary>
        private fp3 _sectorCenter;

        public fp3 SectorCenter => _sectorCenter;

        /// <summary>
        /// 扇形对称轴的旋转角
        /// </summary>
        private fpquaternion _quaternion;

        public fpquaternion Quaternion => _quaternion;

        private fp _angle;

        /// <summary>
        /// 圆心角(弧度）
        /// </summary>
        public fp Angle => _angle;

        /// <summary>
        /// 半径 
        /// </summary>
        private fp _radius;

        public fp Radius => _radius;

        /// <summary>
        /// 世界坐标下扇形的对称轴
        /// </summary>
        public fp3 LocalUnitX => this.Transform * fpmath1.right() - _sectorCenter;

        /// <summary>
        /// 世界坐标下扇形本地坐标系Y轴,扇形平面的法向量
        /// </summary>
        public fp3 LocalUnitY => this.Transform * fpmath1.up() - _sectorCenter;

        /// <summary>
        /// 世界坐标下扇形本地坐标系z轴
        /// </summary>
        public fp3 LocalUnitZ => this.Transform * fpmath1.forward() - _sectorCenter;

        /// <summary>
        /// 扇形两个顶点，本地坐标
        /// </summary>
        private fp3[] _localEdgeVers = new fp3[2];

        /// <summary>
        /// 扇形两个顶点，世界坐标
        /// </summary>
        public fp3[] EdgeVertices{ get; private set; }

        public SectorPrimitive(){
        }

        public static SectorPrimitive Create(fp3 sectorCenter, fpquaternion quaternion, fp angle, fp radius)
        {
            SectorPrimitive data = FPoolHelper.Get<SectorPrimitive>();
            data._sectorCenter = sectorCenter;
            data._quaternion = quaternion;
            data._angle = angle / 180 * fpmath.PI;
            data._radius = radius;
            data.Transform.SetOrientationAndPos(data._quaternion, data._sectorCenter);
            data._localEdgeVers = new fp3[2];
            data.EdgeVertices = new fp3[2];
            data.GetLocalVertices();
            data.PrimitiveType = PrimitiveEnum.SectorPrimitive;
            return data;
        }

        public override void OnInit(PrimitiveInfo info, out bool result){
            base.OnInit(info, out result);
            this._sectorCenter = info.Center;
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

            this._angle = this._angle / 180 * fpmath.PI;
            this._radius = info.Radius;

            if (this._radius <= 0){
                this._radius = (fp)0.001f;
                result = false;
            }

            Transform.SetOrientationAndPos(_quaternion, _sectorCenter);
            _localEdgeVers = new fp3[2];
            EdgeVertices = new fp3[2];
            GetLocalVertices();
            PrimitiveType = PrimitiveEnum.SectorPrimitive;
        }

        public override void UpdateSelf(PrimitiveInfo info)
        {
            this._sectorCenter = info.Center;
            this._quaternion = info.Quaternion;
            this._quaternion *= Quaternion.Euler(new fp3(0, -90, 0));
            this._angle = info.Angle;

            if (info.Angle <= 0){
                this._angle = (fp)0.001f;
            }

            if (info.Angle > 360){
                this._angle = 360;
            }

            this._angle = this._angle / 180 * fpmath.PI;
            this._radius = info.Radius;

            if (this._radius <= 0){
                this._radius = (fp)0.001f;
            }

            Transform.SetOrientationAndPos(_quaternion, _sectorCenter);
            GetLocalVertices();
            PrimitiveType = PrimitiveEnum.SectorPrimitive;
            base.UpdateSelf(info);
        }

        private void GetLocalVertices(){
            fp3 ver1 = new fp3(_radius * fpmath.cos(_angle / 2), 0, _radius * fpmath.sin(_angle / 2));
            fp3 ver2 = new fp3(_radius * fpmath.cos(_angle / 2), 0, -(_radius * fpmath.sin(_angle / 2)));
            _localEdgeVers[0] = ver1;
            _localEdgeVers[1] = ver2;
            EdgeVertices[0] = Transform * ver1;
            EdgeVertices[1] = Transform * ver2;
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
#if UNITY_EDITOR

        public override void PrimitiveDebug(Color color){
            base.PrimitiveDebug(color);

            // 扇形两边
            for (int i = 0; i < _localEdgeVers.Length; i++){
                DrawDebugTools.DrawLine(fpmath1.Fp3ToVector3(_sectorCenter), fpmath1.Fp3ToVector3(EdgeVertices[i]), color, this.leftTime);
            }

            // 扇形的圆弧
            int VertexCount = 10;
            fp deltaTheta = _angle / VertexCount;
            fp theta = -_angle / 2;
            fp3 oldPos = _localEdgeVers[1];

            for (int i = 0; i < VertexCount + 1; i++){
                fp3 pos = new fp3(_radius * fpmath.cos(theta), 0, _radius * fpmath.sin(theta));
                DrawDebugTools.DrawLine(fpmath1.Fp3ToVector3(Transform * oldPos), fpmath1.Fp3ToVector3(Transform * pos), color, this.leftTime);
                oldPos = pos;
                theta += deltaTheta;
            }
        }

        public override void OnDrawGizmos()
        {
            if (!ShowInfo.isDrawShow || EdgeVertices == null || _localEdgeVers == null)
            {
                return;
            }

            Gizmos.color = ShowInfo.drawColor;

            Vector3 center = fpmath1.Fp3ToVector3(_sectorCenter);
            Gizmos.DrawLine(center, fpmath1.Fp3ToVector3(EdgeVertices[0]));
            Gizmos.DrawLine(center, fpmath1.Fp3ToVector3(EdgeVertices[1]));

            int vertexCount = 24;
            fp deltaTheta = _angle / vertexCount;
            fp theta = -_angle / 2;
            fp3 oldPos = _localEdgeVers[1];

            for (int i = 0; i <= vertexCount; i++)
            {
                fp3 pos = new fp3(_radius * fpmath.cos(theta), 0, _radius * fpmath.sin(theta));
                Gizmos.DrawLine(fpmath1.Fp3ToVector3(Transform * oldPos), fpmath1.Fp3ToVector3(Transform * pos));
                oldPos = pos;
                theta += deltaTheta;
            }
        }
#endif
        public override bool InternalCheckPrimitive(){
            if (_radius <= 0 || _angle <= 0){
                //Log.Error("扇形几何形状的半径参数或扫掠角参数不符合规范。");
                return false;
            }

            return true;
        }

        public override void OnDispose()
        {
            FPoolHelper.Release<SectorPrimitive>(this);
        }

        public override void Clear(){
            base.Clear();
            _localEdgeVers = null;
            EdgeVertices = null;
        }
    }

}
