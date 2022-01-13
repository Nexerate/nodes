#region Using
#endregion

namespace Nexerate.Nodes
{
    public enum HierarchyLockState
    {
        /// <summary>
        /// You can freely add and remove nodes from the hierarchy, except for nodes with <see cref="ParentLockState"/> set to <see cref="ParentLockState.Locked"/>.
        /// </summary>
        None,
        /// <summary>
        /// You can not add nor remove any child from <see cref="Node.children"/>. You can still add and remove children of children.
        /// </summary>
        ChildrenLocked,
        /// <summary>
        /// You cannot add nor remove nodes from the hierarchy.
        /// </summary>
        HierarchyLocked
    }
}