#region Using
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Base class for all Nodes.
    /// Derived classes must add the [<seealso cref="SerializableAttribute"/>] attribute.
    /// </summary>
    [Serializable]
    [AddNodeMenu("Node")]
    public class Node
    {
        #region Enabled (Add back when hierarchy elements can be disabled)
        /*[SerializeField] bool enabled = true;
        public bool Enabled => enabled;
        public bool EnabledInHierarchy => enabled && !DisabledInHierarchy();

        /// <summary>
        /// Is this node directly enabled?
        /// </summary>
        /// <param name="enable"></param>
        public void SetEnabled(bool enable)
        {
            enabled = enable;
        }

        /// <summary>
        /// If any parent is disabled, this node is disabled in the hierarchy.
        /// </summary>
        bool DisabledInHierarchy() => GetAncestors().Where(ancestor => !ancestor.enabled).Any();*/
        #endregion

        #region Constructors
        public Node()
        {
            #region Assign Automatic Name
            if (string.IsNullOrEmpty(name))
            {
                name = GetType().Name.SpaceBeforeUppercase();
            }
            #endregion

            #region Generate Unique ID
            if (id == 0)
            {
                GenerateNewID();
            }
            #endregion
        }
        #endregion

        #region Duplicate
        /// <summary>
        /// Duplicate a <see cref="Node"/>. 
        /// Internally, the <see cref="Node"/> and its hierarchy is saved as JSON, then loaded back from the JSON to create a deep copy.
        /// </summary>
        public Node Duplicate() => NodeDuplicator.Duplicate(this);
        #endregion

        #region Save & Load
        /// <summary>
        /// Create a deep copy JSON output from this <see cref="Node"/> and its hierarchy. Can be loaded in using <see cref="LoadFromJSON{T}(string)"/>.
        /// </summary>
        public string SaveAsJSON() => NodeDuplicator.SaveAsJSON(this);

        /// <summary>
        /// Reconstructs a <see cref="Node"/> and its hierarchy from JSON text created using <see cref="SaveAsJSON"/>. 
        /// </summary>
        public static T LoadFromJSON<T>(string json) where T : Node => NodeDuplicator.LoadFromJSON<T>(json); 
        #endregion

        #region ID
        [SerializeField] protected int id = 0;
        public int ID => id;

        int GenerateNewID() => id = Guid.NewGuid().ToString().GetHashCode();

        internal void RegenerateIDs()
        {
            GenerateNewID();
            TraverseHierarchy(this, node =>
            {
                node.parentID = node.parent == null? 0: node.parent.id;
                node.GenerateNewID();
            });
        }
        #endregion

        #region Name
        [SerializeField] string name;
        public string Name { get => name; set => name = value.Trim(); }
        #endregion

        #region Parent
        bool ValidParenting(Node node, Node newParent)
        {
            //If the parent of the node is locked, there is no point in validating the parents
            if (node.parentLockState == ParentLockState.Locked) return false;

            //No point in doing anything if parent is the same
            if (node.parent == newParent) return false;

            //Node cannot be parented to itself
            if (node == newParent) return false;

            //Check if both parents allow for hierarchy modification
            if (Validate(node.parent) && Validate(newParent))
            {
                //Final check. Only needs to be performed on the new parent
                //Cannot set the parent to a node that has this node as an ancestor
                if (newParent != null && newParent.HasAncestor(node))
                {
                    return false;
                }
                
                //All checks passed. Parenting is valid.
                return true;
            }
            //Validations failed. Invalid parenting
            else return false;

            #region Validate
            static bool Validate(Node parent)
            {
                //Null is a valid parent as far as we are concerned
                if (parent == null) return true;

                //If parent has its lockstate to anything but None, we are unable to move the node
                if (parent.hierarchyLockState != HierarchyLockState.None) return false;

                //If any ancestor has a locked hierarchy, the parenting is invalid
                if (parent.GetAncestors().Where(ancestor => ancestor.HierarchyLocked).Any()) return false;

                return true;
            } 
            #endregion
        }

        [HideInInspector, NonSerialized] protected Node parent;
        [SerializeField, HideInInspector] internal int parentID;//Used to reconstruct hierarchy. 
        
        /// <summary>
        /// Set the parent of a <see cref="Node"/> to <paramref name="newParent"/>.
        /// </summary>
        /// <param name="newParent"></param>
        /// <returns>True if <paramref name="newParent"/> passed the validation checks and the parenting was successful.</returns>
        public bool SetParent(Node newParent)
        {
            return SetParent(newParent, true, true);
        }

        internal bool SetParent(Node newParent, bool validate, bool notify)
        {
            bool valid = !validate || ValidParenting(this, newParent);

            if (valid)
            {
                if (parent != null)
                {
                    parent.children.Remove(this);
                    if (notify)
                    {
                        parent.OnChildrenChangedInternal();
                    }
                }

                parent = newParent;

                if (parent != null)
                {
                    parentID = parent.ID;
                    parent.children.Add(this);
                    if (notify)
                    {
                        parent.OnChildrenChangedInternal();
                    }
                }
            }

            //Parent was successfully set to newParent
            return parent == newParent;
        }

        public Node Parent => parent;
        #endregion

        #region Lock
        /// <summary>
        /// Are the children of this <see cref="Node"/> locked?
        /// </summary>
        public bool ChildrenLocked => hierarchyLockState == HierarchyLockState.ChildrenLocked;

        /// <summary>
        /// Is the hierarchy of this <see cref="Node"/> locked?
        /// </summary>
        public bool HierarchyLocked => hierarchyLockState == HierarchyLockState.HierarchyLocked;
        
        /// <summary>
        /// If the parent of the <see cref="Node"/> is in any way locked, this will return true. 
        /// </summary>
        public bool ParentLocked
        {
            get
            {
                //Parent is directly locked.
                if (parentLockState == ParentLockState.Locked) return true;

                //Parent is not locked and we do not have any parent to control this node.
                if (parent == null) return false;

                //Parent has locked its children.
                if (parent.ChildrenLocked) return true;

                //Check if any ancestor has locked the hierarchy.
                return IsInLockedHierarchy;
            }
        }

        public bool IsInLockedHierarchy => GetAncestors().Where(a => a.HierarchyLocked).Any();

        #region Lock/Unlock Parent
        /// <summary>
        /// Set <see cref="parentLockState"/> to <see cref="ParentLockState.Auto"/>. Parent may still be locked by ancestors.
        /// </summary>
        /// <returns><see cref="ParentLockState"/> after operation is finished.</returns>
        public ParentLockState UnlockParent() => parentLockState = ParentLockState.Auto;

        /// <summary>
        /// Set <see cref="parentLockState"/> to <see cref="ParentLockState.Locked"/>.
        /// </summary>
        /// <returns><see cref="ParentLockState"/> after operation is finished.</returns>
        public ParentLockState LockParent() => parentLockState = ParentLockState.Locked; 
        #endregion

        #region Lock/Unlock Hierarchy
        /// <summary>
        /// Set <see cref="hierarchyLockState"/> to <see cref="HierarchyLockState.None"/>.
        /// </summary>
        /// <returns><see cref="HierarchyLockState"/> after operation is finished.</returns>
        public HierarchyLockState UnlockHierarchy() => hierarchyLockState = HierarchyLockState.None;

        /// <summary>
        /// Set <see cref="hierarchyLockState"/> to <see cref="HierarchyLockState.ChildrenLocked"/>.
        /// </summary>
        /// <returns><see cref="HierarchyLockState"/> after operation is finished.</returns>
        public HierarchyLockState LockChildren() => hierarchyLockState = HierarchyLockState.ChildrenLocked;

        /// <summary>
        /// Set <see cref="hierarchyLockState"/> to <see cref="HierarchyLockState.HierarchyLocked"/>.
        /// </summary>
        /// <returns><see cref="HierarchyLockState"/> after operation is finished.</returns>
        public HierarchyLockState LockHierarchy() => hierarchyLockState = HierarchyLockState.HierarchyLocked; 
        #endregion

        [SerializeField, HideInInspector] HierarchyLockState hierarchyLockState = HierarchyLockState.None;
        [SerializeField, HideInInspector] ParentLockState parentLockState = ParentLockState.Auto;
        #endregion

        #region Children
        [HideInInspector, NonSerialized] protected List<Node> children = new();
        public int ChildCount => children.Count; 

        #region Get
        /// <summary>
        /// Get child at index.
        /// </summary>
        /// <param name="index">Index of child.</param>
        /// <returns>Child node at specified index.</returns>
        public Node this[int index] => children[index];

        /// <summary>
        /// Get child in <see cref="Node"/> hierarchy using an array of indices.
        /// </summary>
        public Node GetChild(int[] indices)
        {
            Node node = this;
            for (int i = 0; i < indices.Length; i++)
            {
                node = node[indices[i]];
            }
            return node;
        }
        #endregion

        #region Index
        /// <summary>
        /// Get the index of a child.
        /// </summary>
        /// <param name="child"></param>
        /// <returns>Index of the child if the parent of the child is this node, and -1 if it is not.</returns>
        public int IndexOf(Node child) => children.IndexOf(child);
        #endregion

        #region Insert
        public void InsertChild(int index, Node child)
        {
            if (child == this) return;

            //Child parent is the same, but child was moved
            //SetParent will not call HierarchyChanged, so we need to do it ourselves after the reorder has happened
            bool reorder = child.parent == this;

            //Execute logic if the parent was successfully set to this node
            if (child.SetParent(this))
            {
                children.Remove(child);
                children.Insert(index, child);
            }
            if (reorder) OnChildrenChangedInternal();
        }
        #endregion

        #region Changed
        public event Action ChildrenChanged;
        void OnChildrenChangedInternal() 
        {
            //Nodes that override this function will have their logic executed first.
            OnChildrenChanged();

            //Then comes scripts that have subscribed to this event.
            ChildrenChanged?.Invoke();

            //Lastly, call this function recursively on eventual parent.
            parent?.OnChildrenChangedInternal();
        }

        /// <summary>
        /// Called when a change happens to the hierarchy of the <see cref="Node"/>. 
        /// Invoked right before <see cref="ChildrenChanged"/> event.
        /// </summary>
        protected virtual void OnChildrenChanged() { }
        #endregion
        #endregion

        #region Find
        /// <summary>
        /// Find a node withing the hierarchy with a matching id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>First node with the specified id withing the hierarchy.</returns>
        public Node Find(int id)
        {
            if (ID == id) return this;
            else return FindRecursive(this, id);
        }

        Node FindRecursive(Node parent, int id)
        {
            foreach (var child in parent.children)
            {
                if (child.id == id)
                    return child;

                var search = FindRecursive(child, id);
                if (search != null)
                    return search;
            }
            return null;
        }
        #endregion

        #region Get Root Node
        Node GetRootNode()
        {
            Node node = this;
            while (node.parent != null)
            {
                node = node.parent;
            }
            return node;
        }
        #endregion

        #region Ancestor
        public bool HasAncestor(Node parent) => GetAncestors().Contains(parent);

        public List<Node> GetAncestors()
        {
            List<Node> ancestors = new();

            Node node = this;

            while(node.parent != null)
            {
                node = node.parent;
                ancestors.Add(node);
            }

            return ancestors;
        }

        #endregion

        #region Traverse Hierarchy
        /// <summary>
        /// Traverse a hierarchy of nodes and perform actions on each node.
        /// </summary>
        /// <param name="parent">Node being traversed.</param>
        /// <param name="hook">Action to be performed on each node in the hierarchy.</param>
        internal void TraverseHierarchy(Node parent, Action<Node> hook)
        {
            foreach (var child in parent.children)
            {
                hook.Invoke(child);

                TraverseHierarchy(child, hook);
            }
        } 
        #endregion

        #region Indexed Path
        /// <summary>
        /// This function will give you a "key" used to find a node in its hierarchy. Loop through the indices from the root to get to this node.
        /// If you know the node position will not move, you can cache this "key" to easily retrieve the node at a near-zero performance cost.
        /// </summary>
        /// <returns>Array of indices, which can be used to easily find the node in the hierarchy. i.e: [0,1,1,0]</returns>
        public int[] IndexedPath()
        {
            //This is a root node.
            if (parent == null) return new int[0];

            List<int> path = new() { parent.IndexOf(this) };

            Node current = parent;

            //If current against all odds is this node, then we have a circular reference of parents, which should be impossible
            while (current.parent != null && current != this)
            {
                path.Insert(0, current.parent.IndexOf(current));
                current = current.parent;
            }
            return path.ToArray();
        } 
        #endregion
    }
}