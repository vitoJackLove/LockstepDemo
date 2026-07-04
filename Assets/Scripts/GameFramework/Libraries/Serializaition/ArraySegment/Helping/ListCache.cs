using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ase.Serializing.Helping
{
    /// <summary>
    /// Creates a reusable cache of T which auto expands.
    /// </summary>
    public class ListCache<T>
    {
        #region Public.

        /// <summary>
        /// Collection cache is for.
        /// </summary>
        public List<T> Collection = new List<T>();

        /// <summary>
        /// Entries currently written.
        /// </summary>
        public int Written => Collection.Count;

        #endregion

        #region Private.

        /// <summary>
        /// Cache for type.
        /// </summary>
        private Stack<T> _cache = new Stack<T>();

        #endregion

        public ListCache()
        {
            Collection = new List<T>();
        }

        public ListCache(int capacity)
        {
            Collection = new List<T>(capacity);
        }

        /// <summary>
        /// Returns T from cache when possible, or creates a new object when not.
        /// </summary>
        /// <returns></returns>
        private T Retrieve()
        {
            if (_cache.Count > 0)
                return _cache.Pop();
            else
                return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Stores value into the cache.
        /// </summary>
        /// <param name="value"></param>
        private void Store(T value)
        {
            _cache.Push(value);
        }

        /// <summary>
        /// Adds a new value to Collection and returns it.
        /// </summary>
        /// <param name="value"></param>
        public T AddReference()
        {
            T next = Retrieve();
            Collection.Add(next);
            return next;
        }

        /// <summary>
        /// Inserts an bject into Collection and returns it.
        /// </summary>
        /// <param name="value"></param>
        public T InsertReference(int index)
        {
            //Would just be at the end anyway.
            if (index >= Collection.Count)
                return AddReference();

            T next = Retrieve();
            Collection.Insert(index, next);
            return next;
        }

        /// <summary>
        /// Adds value to Collection.
        /// </summary>
        /// <param name="value"></param>
        public void AddValue(T value)
        {
            Collection.Add(value);
        }

        /// <summary>
        /// Inserts value into Collection.
        /// </summary>
        /// <param name="value"></param>

        public void InsertValue(int index, T value)
        {
            //Would just be at the end anyway.
            if (index >= Collection.Count)
                AddValue(value);
            else
                Collection.Insert(index, value);
        }

        /// <summary>
        /// Adds values to Collection.
        /// </summary>
        /// <param name="values"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValues(ListCache<T> values)
        {
            int w = values.Written;
            List<T> c = values.Collection;
            for (int i = 0; i < w; i++)
                AddValue(c[i]);
        }

        /// <summary>
        /// Adds values to Collection.
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValues(T[] values)
        {
            for (int i = 0; i < values.Length; i++)
                AddValue(values[i]);
        }

        /// <summary>
        /// Adds values to Collection.
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValues(List<T> values)
        {
            for (int i = 0; i < values.Count; i++)
                AddValue(values[i]);
        }

        /// <summary>
        /// Adds values to Collection.
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValues(HashSet<T> values)
        {
            foreach (T item in values)
                AddValue(item);
        }

        /// <summary>
        /// Adds values to Collection.
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValues(ISet<T> values)
        {
            foreach (T item in values)
                AddValue(item);
        }

        /// <summary>
        /// Adds values to Collection.
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddValues(IReadOnlyCollection<T> values)
        {
            foreach (T item in values)
                AddValue(item);
        }


        /// <summary>
        /// Resets cache.
        /// </summary>
        public void Reset()
        {
            foreach (T item in Collection)
                Store(item);
            Collection.Clear();
        }
    }
}
