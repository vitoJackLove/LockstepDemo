using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace PrimitiveDetection
{
    public abstract class BasePrimitive : IPool
    {
        public PrimitiveEnum PrimitiveType { get; protected set; }
        public PrimitiveShowInfo ShowInfo { get; protected set; }

        protected virtual float leftTime => .25f;

        public BasePrimitive() { }

        ///<summary>
        /// 变换矩阵
        ///</summary>
        public Matrix4 Transform = Matrix4.NewIdentity();

        public virtual void PrimitiveDebug(Color color) {}

        public virtual void OnDrawGizmos() { }

        public virtual void OnInit(PrimitiveInfo info, out bool result)
        {
            this.ShowInfo = info.showInfo;
            result = true;
        }

        public virtual void UpdateSelf(PrimitiveInfo info)
        {
        }

        public abstract bool InternalCheckPrimitive();

        public abstract void OnDispose();

        ///<summary>
        /// 获取某方向轴向世界坐标方向
        ///</summary>
        public fp3 GetAxis(int index)
        {
            return Transform.GetAxisVector(index);
        }

        public virtual void Clear()
        {
            Transform.Clear();
            PrimitiveType = PrimitiveEnum.NONE;
        }
    }
}