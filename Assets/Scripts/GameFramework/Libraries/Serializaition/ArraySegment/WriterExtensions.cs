using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ase.Serializing
{
    /// <summary>
    /// Extensions to Write methods. Used by Write<T>.
    /// </summary>
    public static class WriterExtensions
    {
        /// <summary>
        /// Types which are are set to auto pack by default.
        /// </summary>
        internal static HashSet<System.Type> DefaultPackedTypes = new HashSet<System.Type>();

        static WriterExtensions()
        {
            DefaultPackedTypes.Add(typeof(int));
            DefaultPackedTypes.Add(typeof(uint));
            DefaultPackedTypes.Add(typeof(long));
            DefaultPackedTypes.Add(typeof(ulong));
            DefaultPackedTypes.Add(typeof(Color));
            DefaultPackedTypes.Add(typeof(Vector2Int));
            DefaultPackedTypes.Add(typeof(Vector3Int));
            DefaultPackedTypes.Add(typeof(Quaternion));
        }

        /// <summary>
        /// Writes value to dst without error checking.
        /// </summary>
        internal static void WriteUInt32(byte[] dst, uint value, ref int position)
        {
            dst[position++] = (byte)value;
            dst[position++] = (byte)(value >> 8);
            dst[position++] = (byte)(value >> 16);
            dst[position++] = (byte)(value >> 24);
        }
        /// <summary>
        /// Writes value to dst without error checking.
        /// </summary>
        internal static void WriteUInt64(byte[] dst, ulong value, ref int position)
        {
            dst[position++] = (byte)value;
            dst[position++] = (byte)(value >> 8);
            dst[position++] = (byte)(value >> 16);
            dst[position++] = (byte)(value >> 24);
            dst[position++] = (byte)(value >> 32);
            dst[position++] = (byte)(value >> 40);
            dst[position++] = (byte)(value >> 48);
            dst[position++] = (byte)(value >> 56);
        }

        /// <summary>
        /// 判斷兩個序列是否相等
        /// </summary>
        /// <returns></returns>
        public static bool SequenceEqual(this PooledWriter writer1, PooledWriter writer2)
        {
            return writer1.GetArraySegment().SequenceEqual(writer2.GetArraySegment());
            //return writer1.GetBuffer().SequenceEqual(writer2.GetBuffer());
        }
        
        //public static void WriteGameObject(this Writer writer, GameObject value) => writer.WriteGameObject(value);
        //public static void WriteTransform(this Writer writer, Transform value) => writer.WriteTransform(value);
    }
}
