﻿#region Using
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
        [SerializeReference] protected BaseNodeType root;
        public sealed override Node Root => root;

        #region Constructor
        public NodeAsset()
        {
            root = BuildHierarchy();
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

    public abstract class NodeAsset : ScriptableObject
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
            var root = Root;
            root.ChildrenChanged -= OnGraphChangedInternal;
            root.ChildrenChanged += OnGraphChangedInternal;

            ReCompileNodeList();

            Initialize();
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
        void OnGraphChangedInternal()
        {
            ReCompileNodeList();
            OnGraphChanged();
        }

        /// <summary>
        /// Called when a change happens to the graph hierarchy.
        /// </summary>
        protected virtual void OnGraphChanged() { } 
        #endregion

        void ReCompileNodeList()
        {
            nodes.Clear();
            CompileNodeListFromHierarchy(Root);
        }

        public Node Find(int id)
        {
            return nodes.Where(node => node.ID == id).FirstOrDefault();
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
                CompileNodeListFromHierarchy(parent[i]);
            }
        }

        #region Get Root Node
        public abstract Node Root { get; } 
        #endregion

        #region Selected ID
        [SerializeField, HideInInspector] int selectedID;
        public int SelectedID { get => selectedID; set => selectedID = value; } 
        #endregion
    }
}
