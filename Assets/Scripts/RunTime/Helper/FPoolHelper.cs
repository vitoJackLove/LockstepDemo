using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 对象池系统
/// </summary>
public static class FPoolHelper 
{
     /// <summary>
     /// 对象池系统
     /// </summary>
     private static Dictionary<Type, object> _objectPoolDic = new Dictionary<Type, object>();

     /// <summary>
     /// 获取
     /// </summary>
     /// <typeparam name="T"></typeparam>
     /// <returns></returns>
     public static T Get<T>() where T : class, IPool, new()
     {
          if (!_objectPoolDic.ContainsKey(typeof(T)))
          {
               ObjectPool<T> objectPool = new ObjectPool<T>(CreateObject<T>, GetObject<T>, ReleaseObject<T>,DestroyObject<T>);

               _objectPoolDic.Add(typeof(T), objectPool);

               return objectPool.Get();
          }

          return ((ObjectPool<T>)_objectPoolDic[typeof(T)]).Get();
     }

     //TODO 清空引用
     private static void DestroyObject<T>(T obj) where T : class, IPool, new()
     {
     }

     private static void GetObject<T>(T obj) where T : class, IPool, new() { }

     public static void Release<T>(object obj) where T : class, IPool, new()
     {
          if (!_objectPoolDic.ContainsKey(typeof(T)))
          {
               Debug.Log($"回收对象错误：没有{typeof(T)}类型");
               
               return;
          }
          ((ObjectPool<T>)_objectPoolDic[typeof(T)]).Release((T)obj);
     }

     private static T CreateObject<T>() where T : class, IPool, new()
     {
          return new T();
     }
     
     private static void ReleaseObject<T>(object obj) where T : class, IPool, new()
     {
          IPool pool = (IPool)obj;
          
          pool.Clear();
     }
     
     public static void Clear()
     {
          _objectPoolDic.Clear();
          _objectPoolDic = null;
     }
}
