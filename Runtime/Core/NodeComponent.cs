#region Using
using UnityEngine;
using System;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Base class for all components that can be added to nodes.
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
        [SerializeField] bool isRequiredComponent;

        //This will be made init once supported
        public bool IsRequiredComponent { get => isRequiredComponent; set => isRequiredComponent = value; }

        public NodeComponent(Node target) { }

        /// <summary>
        /// Called when a <see cref="NodeComponent"/> is changed in the Editor.
        /// </summary>
        public virtual void OnValidate() { }

        public static NodeComponent CreateComponent(Type component, Node targetNode)
        {
            return (NodeComponent)Activator.CreateInstance(component, targetNode);
        }
    }

    /*[Serializable]
    [AddNodeComponentMenu("Test")]
    public class TestComponent : NodeComponent<Node>
    {
        public float test = 1;
        public TestComponent(Node target) : base(target) { }
    }*/
}
