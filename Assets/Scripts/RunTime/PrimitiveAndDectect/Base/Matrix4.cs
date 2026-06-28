using Unity.Mathematics.FixedPoint;
using UnityEngine;

namespace PrimitiveDetection
{
    /// <summary>
    /// 4x3矩阵
    /// </summary>
    public struct Matrix4
    {
        public fp3[] _allAxis;

        /// <summary>
        /// 构造单位矩阵
        /// </summary>
        // static readonly public Matrix4 Identity = new Matrix4(1, 0, 0, 0,
        //     0, 1, 0, 0,
        //     0, 0, 1, 0);

        /// <summary>
        /// 构造带偏移量的单位矩阵
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Matrix4 IdentityOffset(fp3 offset)
        {
            var m = NewIdentity();
            m._allAxis[3].x += offset.x;
            m._allAxis[3].y += offset.y;
            m._allAxis[3].z += offset.z;
            return m;
        }


        public Matrix4(fp m0, fp m1, fp m2, fp m3,
            fp m4, fp m5, fp m6, fp m7,
            fp m8, fp m9, fp m10, fp m11)
        {
            _allAxis = new[]
            {
                new fp3(m0, m4, m8),
                new fp3(m1, m5, m9),
                new fp3(m2, m6, m10),
                new fp3(m3, m7, m11)
            };
        }

        private Matrix4(int m0, int m1, int m2, int m3,
            int m4, int m5, int m6, int m7,
            int m8, int m9, int m10, int m11)
        {
            _allAxis = new[]
            {
                new fp3(m0, m4, m8),
                new fp3(m1, m5, m9),
                new fp3(m2, m6, m10),
                new fp3(m3, m7, m11)
            };
        }

        public static Matrix4 NewIdentity()
        {
            Matrix4 result = new Matrix4
            {
                _allAxis = new[]
                    { 
                        new fp3(1, 0, 0), 
                        new fp3(0, 1, 0), 
                        new fp3(0, 0, 1), 
                        new fp3(0, 0, 0) 
                    }
            };

            return result;
        }

        /// <summary>
        /// 矩阵乘法
        /// </summary>
        public static Matrix4 operator *(Matrix4 m1, Matrix4 m2)
        {
            Matrix4 result = new Matrix4();
            result._allAxis[0].x = m2._allAxis[0].x * m1._allAxis[0].x + m2._allAxis[0].y * m1._allAxis[1].x + m2._allAxis[0].z * m1._allAxis[2].x;
            result._allAxis[0].y = m2._allAxis[0].x * m1._allAxis[0].y + m2._allAxis[0].y * m1._allAxis[1].y + m2._allAxis[0].z * m1._allAxis[2].y;
            result._allAxis[0].z = m2._allAxis[0].x * m1._allAxis[0].z + m2._allAxis[0].y * m1._allAxis[1].z + m2._allAxis[0].z * m1._allAxis[2].z;

            result._allAxis[1].x = m2._allAxis[1].x * m1._allAxis[0].x + m2._allAxis[1].y * m1._allAxis[1].x + m2._allAxis[1].z * m1._allAxis[2].x;
            result._allAxis[1].y = m2._allAxis[1].x * m1._allAxis[0].y + m2._allAxis[1].y * m1._allAxis[1].y + m2._allAxis[1].z * m1._allAxis[2].y;
            result._allAxis[1].z = m2._allAxis[1].x * m1._allAxis[0].z + m2._allAxis[1].y * m1._allAxis[1].z + m2._allAxis[1].z * m1._allAxis[2].z;

            result._allAxis[2].x = m2._allAxis[2].x * m1._allAxis[0].x + m2._allAxis[2].y * m1._allAxis[1].x + m2._allAxis[2].z * m1._allAxis[2].x;
            result._allAxis[2].y = m2._allAxis[2].x * m1._allAxis[0].y + m2._allAxis[2].y * m1._allAxis[1].y + m2._allAxis[2].z * m1._allAxis[2].y;
            result._allAxis[2].z = m2._allAxis[2].x * m1._allAxis[0].z + m2._allAxis[2].y * m1._allAxis[1].z + m2._allAxis[2].z * m1._allAxis[2].z;

            result._allAxis[3].x = m2._allAxis[3].x * m1._allAxis[0].x + m2._allAxis[3].y * m1._allAxis[1].x + m2._allAxis[3].z * m1._allAxis[2].x + m1._allAxis[3].x;
            result._allAxis[3].y = m2._allAxis[3].x * m1._allAxis[0].y + m2._allAxis[3].y * m1._allAxis[1].y + m2._allAxis[3].z * m1._allAxis[2].y + m1._allAxis[3].y;
            result._allAxis[3].z = m2._allAxis[3].x * m1._allAxis[0].z + m2._allAxis[3].y * m1._allAxis[1].z + m2._allAxis[3].z * m1._allAxis[2].z + m1._allAxis[3].z;

            return result;
        }

        /// <summary>
        /// 矩阵乘向量
        /// </summary>
        public static fp3 operator *(Matrix4 m, fp3 vector)
        {
            return new fp3(
                vector.x * m._allAxis[0].x + vector.y * m._allAxis[1].x + vector.z * m._allAxis[2].x + m._allAxis[3].x,
                vector.x * m._allAxis[0].y + vector.y * m._allAxis[1].y + vector.z * m._allAxis[2].y + m._allAxis[3].y,
                vector.x * m._allAxis[0].z + vector.y * m._allAxis[1].z + vector.z * m._allAxis[2].z + m._allAxis[3].z);
        }

        /// <summary>
        /// 转为字符串
        /// </summary>
        public override string ToString()
        {
            return _allAxis[0].x + "," + _allAxis[1].x + "," + _allAxis[2].x + "," + _allAxis[3].x + "\n" +
                   _allAxis[0].y + "," + _allAxis[1].y + "," + _allAxis[2].y + "," + _allAxis[3].y + "\n" +
                   _allAxis[0].z + "," + _allAxis[1].z + "," + _allAxis[2].z + "," + _allAxis[3].z;
        }

        /// <summary>
        /// 对角赋值
        /// </summary>
        public void SetDiagonal(fp a, fp b, fp c)
        {
            _allAxis[0].x = a;
            _allAxis[1].y = b;
            _allAxis[2].z = c;
        }

        /// <summary>
        /// 变换给定点
        /// </summary>
        public fp3 Transform(fp3 vector)
        {
            return this * vector;
        }

        /// <summary>
        /// 获取行列式
        /// </summary>
        public fp GetDeterminant()
        {
            return (-_allAxis[0].z * (_allAxis[1].y * _allAxis[2].x)
                    + (_allAxis[0].y * (_allAxis[1].z * _allAxis[2].x))
                    + (_allAxis[0].z * (_allAxis[1].x * _allAxis[2].y))
                    - (_allAxis[0].x * (_allAxis[1].z * _allAxis[2].y))
                    - (_allAxis[0].y * (_allAxis[1].x * _allAxis[2].z))
                    + (_allAxis[0].x * (_allAxis[1].y * _allAxis[2].z)));
        }

        /// <summary>
        /// 赋值为给定向量的逆矩阵
        /// </summary>
        public void SetInverse(Matrix4 m)
        {
            // Make sure the determinant is non-zero.
            fp det = m.GetDeterminant();

            fp value = (fp)0.00001f;
            
            if (fpmath.abs(det) <= value) return;

            det = 1 / det;

            _allAxis[0].x = (-((_allAxis[1].z * _allAxis[2].y) + (_allAxis[1].y * _allAxis[2].z)) * det);
            _allAxis[0].y = (((_allAxis[0].z * _allAxis[2].y) - (_allAxis[0].y * _allAxis[2].z)) * det);
            _allAxis[0].z = (-((_allAxis[0].z * _allAxis[1].y) + (_allAxis[0].y * _allAxis[1].z)) * det);

            _allAxis[1].x = (((_allAxis[1].z * _allAxis[2].x) - (_allAxis[1].x * _allAxis[2].z)) * det);
            _allAxis[1].y = (-((_allAxis[0].z * _allAxis[2].x) + (_allAxis[0].x * _allAxis[2].z)) * det);
            _allAxis[1].z = (((_allAxis[0].z * _allAxis[1].x) - (_allAxis[0].x * _allAxis[1].z)) * det);

            _allAxis[2].x = (-((_allAxis[1].y * _allAxis[2].x) + (_allAxis[1].x * _allAxis[2].y)) * det);
            _allAxis[2].y = (((_allAxis[0].y * _allAxis[2].x) - (_allAxis[0].x * _allAxis[2].y)) * det);
            _allAxis[2].z = (-((_allAxis[0].y * _allAxis[1].x) + (_allAxis[0].x * _allAxis[1].y)) * det);

            _allAxis[3].x = (
                (_allAxis[1].z * (_allAxis[2].y * _allAxis[3].x))
                - (_allAxis[1].y * (_allAxis[2].z * _allAxis[3].x))
                - (_allAxis[1].z * (_allAxis[2].x * _allAxis[3].y))
                + (_allAxis[1].x * (_allAxis[2].z * _allAxis[3].y))
                + (_allAxis[1].y * (_allAxis[2].x * _allAxis[3].z))
                - (_allAxis[1].x * (_allAxis[2].y * _allAxis[3].z)) * det);

            _allAxis[3].y = (
                -(_allAxis[0].z * (_allAxis[2].y * _allAxis[3].x))
                + (_allAxis[0].y * (_allAxis[2].z * _allAxis[3].x))
                + (_allAxis[0].z * (_allAxis[2].x * _allAxis[3].y))
                - (_allAxis[0].x * (_allAxis[2].z * _allAxis[3].y))
                - (_allAxis[0].y * (_allAxis[2].x * _allAxis[3].z))
                + (_allAxis[0].x * (_allAxis[2].y * _allAxis[3].z)) * det);

            _allAxis[3].z = (
                (_allAxis[0].z * (_allAxis[1].y * _allAxis[3].x))
                - (_allAxis[0].y * (_allAxis[1].z * _allAxis[3].x))
                - (_allAxis[0].z * (_allAxis[1].x * _allAxis[3].y))
                + (_allAxis[0].x * (_allAxis[1].z * _allAxis[3].y))
                + (_allAxis[0].y * (_allAxis[1].x * _allAxis[3].z))
                - (_allAxis[0].x * (_allAxis[1].y * _allAxis[3].z)) * det);
        }

        /// <summary>
        /// 返回矩阵的逆矩阵
        /// </summary>
        public Matrix4 Inverse()
        {
            Matrix4 result = NewIdentity();
            result.SetInverse(this);
            return result;
        }

        /// <summary>
        /// 变为逆矩阵
        /// </summary>
        public void Invert()
        {
            SetInverse(this);
        }

        /// <summary>
        /// 变换给定向量
        /// </summary>
        public fp3 TransformDirection(fp3 vector)
        {
            return new fp3((vector.x * _allAxis[0].x) + (vector.y * _allAxis[1].x) + (vector.z * _allAxis[2].x),
                (vector.x * _allAxis[0].y) + (vector.y * _allAxis[1].y) + (vector.z * _allAxis[2].y),
                (vector.x * _allAxis[0].z) + (vector.y * _allAxis[1].z) + (vector.z * _allAxis[2].z));
        }

        /// <summary>
        /// 逆变换给定向量
        /// </summary>
        public fp3 TransformInverseDirection(fp3 vector)
        {
            return new fp3((vector.x * _allAxis[0].x) + (vector.y * _allAxis[0].y) + (vector.z * _allAxis[0].z),
                (vector.x * _allAxis[1].x) + (vector.y * _allAxis[1].y) + (vector.z * _allAxis[1].z),
                (vector.x * _allAxis[2].x) + (vector.y * _allAxis[2].y) + (vector.z * _allAxis[2].z));
        }

        /// <summary>
        /// 逆变换点
        /// </summary>
        public fp3 TransformInverse(fp3 vector)
        {
            fp3 tmp = vector;
            tmp.x -= _allAxis[3].x;
            tmp.y -= _allAxis[3].y;
            tmp.z -= _allAxis[3].z;

            return new fp3((tmp.x * _allAxis[0].x) + (tmp.y * _allAxis[0].y) + (tmp.z * _allAxis[0].z),
                (tmp.x * _allAxis[1].x) + (tmp.y * _allAxis[1].y) + (tmp.z * _allAxis[1].z),
                (tmp.x * _allAxis[2].x) + (tmp.y * _allAxis[2].y) + (tmp.z * _allAxis[2].z));
        }

        /// <summary>
        /// 变换给定向量
        /// </summary>
        // public Matrix3 TransformMatrix3(Matrix3 mt){
        //     return new Matrix3(
        //         (mt._allAxis[0].x * _allAxis[0].x) + (mt._allAxis[3].x * _allAxis[1].x) + (mt._allAxis[2].y * _allAxis[2].x),
        //         (mt._allAxis[1].x * _allAxis[0].x) + (mt._allAxis[0].y * _allAxis[1].x) + (mt._allAxis[3].y * _allAxis[2].x),
        //         (mt._allAxis[2].x * _allAxis[0].x) + (mt._allAxis[1].y * _allAxis[1].x) + (mt._allAxis[0].z * _allAxis[2].x),
        //         (mt._allAxis[0].x * _allAxis[0].y) + (mt._allAxis[3].x * _allAxis[1].y) + (mt._allAxis[2].y * _allAxis[2].y),
        //         (mt._allAxis[1].x * _allAxis[0].y) + (mt._allAxis[0].y * _allAxis[1].y) + (mt._allAxis[3].y * _allAxis[2].y),
        //         (mt._allAxis[2].x * _allAxis[0].y) + (mt._allAxis[1].y * _allAxis[1].y) + (mt._allAxis[0].z * _allAxis[2].y), 
        //         (mt._allAxis[0].x * _allAxis[0].z) + (mt._allAxis[3].x * _allAxis[1].z) + (mt._allAxis[2].y * _allAxis[2].z),
        //         (mt._allAxis[1].x * _allAxis[0].z) + (mt._allAxis[0].y * _allAxis[1].z) + (mt._allAxis[3].y * _allAxis[2].z),
        //         (mt._allAxis[2].x * _allAxis[0].z) + (mt._allAxis[1].y * _allAxis[1].z) + (mt._allAxis[0].z * _allAxis[2].z));
        // }

        /// <summary>
        /// 获取一列
        /// </summary>
        public fp3 GetAxisVector(int i)
        {
            return _allAxis[i];
        }
        
        /// <summary>
        /// 根据四元数旋转和位置进行赋值
        /// </summary>
        /// <param name="q"></param>
        /// <param name="pos"></param>
        public void SetOrientationAndPos(fpquaternion q, fp3 pos)
        {
            fp one = 1;
            fp two = 2;

            _allAxis[0].x = one - ((two * (q.y * q.y)) + (two * (q.z * q.z)));
            _allAxis[1].x = (two * (q.x * q.y)) - (two * (q.z * q.w));
            _allAxis[2].x = (two * (q.x * q.z)) + (two * (q.y * q.w));
            _allAxis[3].x = pos.x;

            _allAxis[0].y = (two * (q.x * q.y)) + (two * (q.z * q.w));
            _allAxis[1].y = one - ((two * (q.x * q.x)) + (two * (q.z * q.z)));
            _allAxis[2].y = (two * (q.y * q.z)) - (two * (q.x * q.w));
            _allAxis[3].y = pos.y;

            _allAxis[0].z = (two * (q.x * q.z)) - (two * (q.y * q.w));
            _allAxis[1].z = (two * (q.y * q.z)) + (two * (q.x * q.w));
            _allAxis[2].z = one - ((two * (q.x * q.x)) + (two * (q.y * q.y)));
            _allAxis[3].z = pos.z;
        }

        /// <summary>
        /// 根据四元数旋转和位置、缩放进行赋值
        /// </summary>
        /// <param name="q"></param>
        /// <param name="pos"></param>
        /// <param name="scale"></param>
        public void SetOrientationScaleAndPos(fpquaternion q, fp3 pos, fp3 scale)
        {
            fp one = 1;
            fp two = 2;

            _allAxis[0].x = one - ((two * (q.y * q.y)) + (two * (q.z * q.z)));
            _allAxis[0].x *= scale.x;
            _allAxis[1].x = (two * (q.x * q.y)) - (two * (q.z * q.w));
            _allAxis[1].x *= scale.x;
            _allAxis[2].x = (two * (q.x * q.z)) + (two * (q.y * q.w));
            _allAxis[2].x *= scale.x;
            _allAxis[3].x = pos.x;

            _allAxis[0].y = (two * (q.x * q.y)) + (two * (q.z * q.w));
            _allAxis[0].y *= scale.y;
            _allAxis[1].y = one - ((two * (q.x * q.x)) + (two * (q.z * q.z)));
            _allAxis[1].y *= scale.y;
            _allAxis[2].y = (two * (q.y * q.z)) - (two * (q.x * q.w));
            _allAxis[2].y *= scale.y;
            _allAxis[3].y = pos.y;

            _allAxis[0].z = (two * (q.x * q.z)) - (two * (q.y * q.w));
            _allAxis[0].z *= scale.z;
            _allAxis[1].z = (two * (q.y * q.z)) + (two * (q.x * q.w));
            _allAxis[1].z *= scale.z;
            _allAxis[2].z = one - ((two * (q.x * q.x)) + (two * (q.y * q.y)));
            _allAxis[2].z *= scale.z;
            _allAxis[3].z = pos.z;
        }

        public void Clear()
        {
            _allAxis[0].x = 1;
            _allAxis[1].x = 0;
            _allAxis[2].x = 0;
            _allAxis[3].x = 0;
            _allAxis[0].y = 0;
            _allAxis[1].y = 1;
            _allAxis[2].y = 0;
            _allAxis[3].y = 0;
            _allAxis[0].z = 0;
            _allAxis[1].z = 0;
            _allAxis[2].z = 1;
            _allAxis[3].z = 0;
        }
    }
}