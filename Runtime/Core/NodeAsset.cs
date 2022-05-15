#region Using
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Minimum requirement for all nodes in the hierarchy is to be derived from <seealso cref="BaseNodeType"/>.
    /// <br></br>
    /// Root node will derive from <seealso cref="BaseNodeType"/>.
    /// </summary>
    public abstract class NodeAsset<BaseNodeType> : NodeAsset where BaseNodeType : Node, new()
    {
        [SerializeReference, HideInInspector] protected BaseNodeType root;
        
        //When covariance is supported, change Node to BaseNodeType
        public sealed override Node Root => root;

        #region Constructor
        public NodeAsset()
        {
            root = BuildHierarchy();

            //We have to recompile the entire node list because BuildHierarchy can return an entire hierarchy
            ReCompileNodeList();
        }

        /// <summary>
        /// Override this function to build a custom hierarchy when the <see cref="NodeAsset"/> is created for the first time.
        /// You can lock Nodes in position using <see cref="HierarchyLockState"/> and <see cref="ParentLockState"/>.
        /// Useful for templates. You might want a template where the user can edit some nodes, but have others locked in place.
        /// By default, the root is created here.
        /// </summary>
        protected virtual BaseNodeType BuildHierarchy() => new BaseNodeType().Rename("Root");
        
        #endregion
    }

    public abstract class NodeAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        #region Nodes
        [SerializeReference] List<Node> nodes = new();
        /// <summary>
        /// Cached list of nodes in the <see cref="NodeAsset"/>. Faster to search this list than traverse the hierarchy yourself.
        /// List is updated when the <see cref="Node"/> hierarchy changes.
        /// </summary>
        public List<Node> Nodes => nodes;
        #endregion

        #region Initialize
        protected void OnEnable()
        {
            Enable();
        }

        public void Enable()
        {
            RebuildHierarchy();
            ReCompileNodeList();
            Initialize();
        }

        /// <summary>
        /// Rebuild the unserialized <see cref="Node"/> hierarchy from the serialized <see cref="nodes"/> list.
        /// <br></br>
        /// <br></br>
        /// Serializing parents and children on all nodes caused an unacceptable amount of lag 
        /// that made Nexerate Nodes unusable with 10+ nodes. <br> </br>Serializing the nodes in a linear array became the solution.
        /// Now it runs smooth with hundreds of nodes.
        /// </summary>
        public void RebuildHierarchy()
        {
            int count = nodes.Count;
            for (int i = 0; i < count; i++)
            {
                nodes[i].SetParent(nodes.Where(node => node.ID == nodes[i].parentID).FirstOrDefault(), false, false);
            }
        }

        /// <summary>
        /// Called after the internal <see cref="NodeAsset"/> initialization. <see cref="NodeAsset"/> is initialized in <see cref="OnEnable"/>.
        /// </summary>
        protected virtual void Initialize() { } 
        #endregion

        #region Graph Changed
        /// <summary>
        /// Invoked when the root detects a hierarchy change. Will recompile the node list and call <see cref="OnGraphChanged"/>.
        /// </summary>
        protected internal void OnGraphChangedInternal()
        {
            ReCompileNodeList();
            OnGraphChanged();
        }

        /// <summary>
        /// Called when a change happens to the graph hierarchy.
        /// </summary>
        protected virtual void OnGraphChanged() { } 
        #endregion

        #region Find
        /// <summary>
        /// Find <see cref="Node"/> where <see cref="Node.ID"/> matches <paramref name="id"/>.
        /// </summary>
        public Node Find(int id)
        {
            return nodes.Where(node => node.ID == id).FirstOrDefault();
        }

        /// <summary>
        /// Find nodes where <see cref="Node.Name"/> matches <paramref name="name"/>.
        /// </summary>
        public IEnumerable<Node> Find(string name)
        {
            return nodes.Where(node => node.Name == name);
        }
        #endregion

        #region Compile Node List
        /// <summary>
        /// Recompile the <see cref="nodes"/> list from the <see cref="Root"/> hierarchy.
        /// </summary>
        protected void ReCompileNodeList()
        {
            nodes.Clear();
            CompileNodeListFromHierarchy(Root);
        }

        void CompileNodeListFromHierarchy(Node parent)
        {
            if (parent == null) return;

            //Root
            if (nodes.Count == 0)
            {
                nodes.Add(parent);
            }

            for (int i = 0; i < parent.ChildCount; i++)
            {
                nodes.Add(parent[i]);
                parent[i].parentID = parent.ID;
                CompileNodeListFromHierarchy(parent[i]);
            }
        }
        #endregion

        #region Serialization Logic
        public void OnBeforeSerialize()
        {
            Root.ChildrenChanged -= OnGraphChangedInternal;
            Root.ChildrenChanged += OnGraphChangedInternal;
        }

        public void OnAfterDeserialize() { }
        #endregion

        #region Get Root Node
        public abstract Node Root { get; } 
        #endregion
    }
}