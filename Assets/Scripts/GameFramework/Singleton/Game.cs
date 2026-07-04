using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rogue
{
    public static class Game
    {
        [StaticField]
        private static readonly Dictionary<Type, ISingleton> singletonTypes = new Dictionary<Type, ISingleton>();
        [StaticField]
        private static readonly Stack<ISingleton> singletons = new Stack<ISingleton>();
        [StaticField]
        private static readonly Queue<ISingleton> updates = new Queue<ISingleton>();
        [StaticField]
        private static readonly Queue<ISingleton> fixedUpdates = new Queue<ISingleton>();
        [StaticField]
        private static readonly Queue<ISingleton> lateUpdates = new Queue<ISingleton>();

        public static T AddSingleton<T>() where T : Singleton<T>, new()
        {
            T singleton = new T();
            AddSingleton(singleton);
            return singleton;
        }

        private static void AddSingleton(ISingleton singleton)
        {
            Type singletonType = singleton.GetType();
            if (singletonTypes.ContainsKey(singletonType))
            {
                throw new Exception($"already exist singleton: {singletonType.Name}");
            }

            singletonTypes.Add(singletonType, singleton);
            singletons.Push(singleton);

            singleton.Register();

            if (singleton is ISingletonAwake awake)
            {
                awake.Awake();
            }

            if (singleton is ISingletonUpdate)
            {
                updates.Enqueue(singleton);
            }
            
            if (singleton is ISingletonFixedUpdate)
            {
                fixedUpdates.Enqueue(singleton);
            }

            if (singleton is ISingletonLateUpdate)
            {
                lateUpdates.Enqueue(singleton);
            }
        }
        
        public static void RemoveSingleton<T>() where T : Singleton<T>, new()
        {
            Type singletonType = typeof(T);

            singletonTypes.TryGetValue(singletonType, out var singleton);
            
            if (singleton == null)
            {
                Debug.LogWarning($"not exist singleton: {singletonType.Name}");
                
                return;
            }

            singletonTypes.Remove(singletonType);
            singleton.Destroy();
        }

        public static void Update()
        {
            int count = updates.Count;
            while (count-- > 0)
            {
                ISingleton singleton = updates.Dequeue();

                if (singleton.IsDisposed())
                {
                    continue;
                }

                if (!(singleton is ISingletonUpdate update))
                {
                    continue;
                }

                updates.Enqueue(singleton);
                try
                {
                    update.Update();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
        
        public static void FixedUpdate()
        {
            int count = fixedUpdates.Count;
            
            while (count-- > 0)
            {
                ISingleton singleton = fixedUpdates.Dequeue();
                
                if (singleton.IsDisposed())
                {
                    continue;
                }

                if (singleton is not ISingletonFixedUpdate fixedUpdate)
                {
                    continue;
                }
                
                fixedUpdates.Enqueue(singleton);
                
                try
                {
                    fixedUpdate.FixedUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public static void LateUpdate()
        {
            int count = lateUpdates.Count;
            while (count-- > 0)
            {
                ISingleton singleton = lateUpdates.Dequeue();

                if (singleton == null || singleton.IsDisposed())
                {
                    continue;
                }

                if (!(singleton is ISingletonLateUpdate lateUpdate))
                {
                    continue;
                }

                lateUpdates.Enqueue(singleton);

                try
                {
                    lateUpdate.LateUpdate();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public static void Close()
        {
            // 顺序反过来清理
            while (singletons.Count > 0)
            {
                ISingleton iSingleton = singletons.Pop();
                iSingleton.Destroy();
            }

            singletonTypes.Clear();
        }
    }
}