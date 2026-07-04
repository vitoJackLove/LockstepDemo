using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ase.Serializing
{
    /// <summary>
    /// Reader which is reused to save on garbage collection and performance.
    /// </summary>
    public sealed class PooledReader : Reader, IDisposable
    {
        internal PooledReader(byte[] bytes) : base(bytes) { }
        internal PooledReader(ArraySegment<byte> segment) : base(segment) { }
        public void Dispose() => ReaderPool.Recycle(this);
    }

    /// <summary>
    /// Collection of PooledReader. Stores and gets PooledReader.
    /// </summary>
    public static class ReaderPool
    {
        #region Private.
        /// <summary>
        /// Pool of readers.
        /// </summary>
        private static readonly Stack<PooledReader> _pool = new Stack<PooledReader>();
        #endregion

        /// <summary>
        /// Get the next reader in the pool
        /// <para>If pool is empty, creates a new Reader</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledReader GetReader(byte[] bytes)
        {
            return GetReader(new ArraySegment<byte>(bytes));
        }

        /// <summary>
        /// Get the next reader in the pool or creates a new one if none are available.
        /// </summary>
        public static PooledReader GetReader(ArraySegment<byte> segment)
        {
            PooledReader result;
            if (_pool.Count > 0)
            {
                result = _pool.Pop();
                result.Initialize(segment);
            }
            else
            {
                result = new PooledReader(segment);
            }

            return result;
        }

        /// <summary>
        /// Puts reader back into pool
        /// <para>When pool is full, the extra reader is left for the GC</para>
        /// </summary>
        public static void Recycle(PooledReader reader)
        {
            _pool.Push(reader);
        }
    }
}
