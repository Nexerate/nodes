namespace Nexerate.Nodes
{
    public enum ParentLockState
    {
        /// <summary>
        /// Depending on ancestor <see cref="HierarchyLockState"/>, you might be able to change the parent of this <see cref="Node"/>. 
        /// </summary>
        Auto,
        /// <summary>
        /// You cannot change the parent of this <see cref="Node"/>.
        /// </summary>
        Locked/*,
        /// <summary>
        /// A parent is required for the <see cref="Node"/>. In other words, the <see cref="Node"/> must always be part of a hierarchy, and cannot be a root.
        /// </summary>
        Required*/
    }
}