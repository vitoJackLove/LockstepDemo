using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TheKiwiCoder
{
    // The context is a storage object for sharing common data between nodes in the tree.
    // Useful for caching components, game objects, or other data that is used by multiple nodes.
    public class Context
    {
        public GameObject gameObject;
        public Transform transform;
        public BaseEntity Entity;
        public Dictionary<string, Node.State> tickResults;

        public static Context CreateFromGameObject(BaseEntity entity , GameObject gameObject)
        {
            Context context = new Context();
            context.gameObject = gameObject;
            context.Entity = entity;
            context.transform = gameObject.transform;
            context.tickResults = new Dictionary<string, Node.State>();
            return context;
        }

        public T GetComponent<T>() where T : Component
        {
            return gameObject.GetComponent<T>();
        }
    }
}