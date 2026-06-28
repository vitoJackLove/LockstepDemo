using System.Collections;
using System.Collections.Generic;
using Ase.Serializing;
using Unity.Profiling;
using UnityEngine;

namespace TheKiwiCoder
{
    [AddComponentMenu("TheKiwiCoder/BehaviourTreeInstance")]
    public partial class BehaviourTreeInstance : MonoBehaviour
    {
        // The main behaviour tree asset
        [Tooltip("BehaviourTree asset to instantiate during Awake")]
        public BehaviourTree behaviourTree;

        [Tooltip("Run behaviour tree validation at startup (Can be disabled for release)")]
        public bool validate = true;

        [Tooltip("Override / set blackboard key values for this behaviour tree instance")]
        public List<BlackboardKeyValuePair> blackboardOverrides = new List<BlackboardKeyValuePair>();

        private bool _isPause;
        
        public BehaviourTree RuntimeTree
        {
            get
            {
                if (runtimeTree != null)
                {
                    return runtimeTree;
                }
                else
                {
                    return behaviourTree;
                }
            }
        }

        // Runtime tree instance
        BehaviourTree runtimeTree;

        // Storage container object to hold game object subsystems
        Context context;

        // Profile markers
        static readonly ProfilerMarker profileUpdate = new ProfilerMarker("BehaviourTreeInstance.Update");

        void ApplyBlackboardOverrides()
        {
            foreach (var pair in blackboardOverrides)
            {
                // Find the key from the new behaviour tree instance
                var targetKey = runtimeTree.blackboard.Find(pair.key.name);
                var sourceKey = pair.value;
                if (targetKey != null && sourceKey != null)
                {
                    targetKey.CopyFrom(sourceKey);
                }
            }
        }

        void InternalUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
            if (runtimeTree)
            {
                profileUpdate.Begin();
                context.tickResults.Clear();
                runtimeTree.Tick(tick,worldUpdateType);
                profileUpdate.End();
            }
        }

        public void ManualTick(uint tick,WorldUpdateType worldUpdateType)
        {
            if (_isPause)
            {
                return;
            }
            
            InternalUpdate(tick,worldUpdateType);
        }

        public void StartBehaviour(BaseEntity baseEntity, GameObject go)
        {
            bool isValid = ValidateTree(behaviourTree);
            
            if (isValid)
            {
                InstantiateTree(baseEntity, go);
            }
            else
            {
                runtimeTree = null;
            }
        }

        private void InstantiateTree(BaseEntity entity,GameObject go)
        {
            context = CreateBehaviourTreeContext(entity,go);
            runtimeTree = behaviourTree.Clone();
            runtimeTree.Bind(context);
            ApplyBlackboardOverrides();
        }

        Context CreateBehaviourTreeContext(BaseEntity entity, GameObject go)
        {
            return Context.CreateFromGameObject(entity,go);
        }

        bool ValidateTree(BehaviourTree tree)
        {
            bool isValid = true;
            
            if (validate)
            {
                string cyclePath;
                
                isValid = !IsRecursive(tree, out cyclePath);

                if (!isValid)
                {
                    Debug.LogError($"Failed to create recursive behaviour tree. Found cycle at: {cyclePath}");
                }
            }

            return isValid;
        }

        bool IsRecursive(BehaviourTree tree, out string cycle)
        {
            // Check if any of the subtree nodes and their decendents form a circular reference, which will cause a stack overflow.
            List<string> treeStack = new List<string>();
            HashSet<BehaviourTree> referencedTrees = new HashSet<BehaviourTree>();

            bool cycleFound = false;
            string cyclePath = "";

            System.Action<Node> traverse = null;
            traverse = (node) =>
            {
                if (!cycleFound)
                {
                    if (node is SubTree subtree && subtree.treeAsset != null)
                    {
                        treeStack.Add(subtree.treeAsset.name);
                        if (referencedTrees.Contains(subtree.treeAsset))
                        {
                            int index = 0;
                            foreach (var tree in treeStack)
                            {
                                index++;
                                if (index == treeStack.Count)
                                {
                                    cyclePath += $"{tree}";
                                }
                                else
                                {
                                    cyclePath += $"{tree} -> ";
                                }
                            }

                            cycleFound = true;
                        }
                        else
                        {
                            referencedTrees.Add(subtree.treeAsset);
                            BehaviourTree.Traverse(subtree.treeAsset.rootNode, traverse);
                            referencedTrees.Remove(subtree.treeAsset);
                        }

                        treeStack.RemoveAt(treeStack.Count - 1);
                    }
                }
            };
            treeStack.Add(tree.name);

            referencedTrees.Add(tree);
            BehaviourTree.Traverse(tree.rootNode, traverse);
            referencedTrees.Remove(tree);

            treeStack.RemoveAt(treeStack.Count - 1);
            cycle = cyclePath;
            return cycleFound;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!runtimeTree)
            {
                return;
            }

            BehaviourTree.Traverse(runtimeTree.rootNode, (n) =>
            {
                if (n.drawGizmos)
                {
                    n.OnDrawGizmos();
                }
            });
        }

        public BlackboardKey<T> FindBlackboardKey<T>(string keyName)
        {
            if (runtimeTree)
            {
                return runtimeTree.blackboard.Find<T>(keyName);
            }

            return null;
        }

        public void SetBlackboardValue<T>(string keyName, T value)
        {
            if (runtimeTree)
            {
                runtimeTree.blackboard.SetValue(keyName, value);
            }
        }

        public T GetBlackboardValue<T>(string keyName)
        {
            if (runtimeTree)
            {
                return runtimeTree.blackboard.GetValue<T>(keyName);
            }

            return default(T);
        }

        public void DoPause()
        {
            _isPause = true;
        }

        public void DoRecover()
        {
            _isPause = false;
        }
    }
}