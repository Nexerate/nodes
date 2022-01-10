#region Using
using UnityEngine;
using System;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Base class for all Components that can be added to Nodes.
    /// Derived classes must add the [<seealso cref="SerializableAttribute"/>] attribute.
    /// </summary>
    /// <typeparam name="T">Type the component is designed for. Can be either interface or class.</typeparam>
    public class NodeComponent<T> : NodeComponent where T : Node
    {
        [SerializeReference, HideInInspector] protected T target;

        public NodeComponent(T target) : base(target)
        {
            this.target = target;
        }
    }

    [Serializable]
    public class NodeComponent 
    {
        public NodeComponent(Node target) { }

        public static NodeComponent CreateComponent(Type component, Node targetNode)
        {
            return (NodeComponent)Activator.CreateInstance(component, targetNode);
        }
    }
}
