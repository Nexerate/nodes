#region Using
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Base class for any <seealso cref="Node"/> with a <seealso cref="NodeComponent"/> List.
    /// Derived classes must add the [<seealso cref="SerializableAttribute"/>] attribute.
    /// </summary>
    [Serializable]
    [AddNodeMenu("Component Node")]
    public class ComponentNode : Node
    {
        #region Components
        [SerializeReference, HideInInspector] protected List<NodeComponent> components = new();
        #endregion

        #region Constructors
        public ComponentNode() : base()
        {
            AddRequiredComponents();
            /*public void InitializeComponents()
            {
                for (int i = 0; i < components.Count; i++)
                {
                    components[i].Initialize();
                }
            }*/
            //Initialize();
            //InitializeComponents();
        }
        #endregion

        #region Required Components
        void AddRequiredComponents()
        {
            var attributes = GetType().GetCustomAttributes<RequireNodeComponents>(true);
            if (attributes != null && attributes.Count() > 0)
            {
                //Do this for each [RequireNodeComponents] attribute
                foreach (var attribute in attributes)
                {
                    //Do this for each component in the attribute
                    foreach (var component in attribute.Components)
                    {
                        if (ComponentCompatibleWithThisNode(component))
                        {
                            var newComponent = NodeComponent.CreateComponent(component, this);
                            newComponent.IsRequiredComponent = true;
                            components.Add(newComponent);
                        }
                    }
                }
            }
        } 
        #endregion

        #region Get Component
        public NodeComponent GetComponent(int index) => components[index];
        public T GetComponent<T>() where T : NodeComponent => (T)GetComponent(typeof(T)); 
        public NodeComponent GetComponent(Type type) => components.Where(c => c.GetType().Equals(type)).FirstOrDefault(); 
        public bool TryGetComponent<T>(out T component) where T : NodeComponent
        {
            component = GetComponent<T>();
            return component != null;
        }
        #endregion

        #region Has Component
        /// <summary>
        /// Does the node have this component?
        /// </summary>
        /// <returns>True if the node has this component.</returns>
        public bool HasComponent(NodeComponent component) => components.Contains(component);

        /// <summary>
        /// Does the node have a component of type T?
        /// </summary>
        /// <returns>True if the node has one or more components of type T.</returns>
        public bool HasComponent<T>() where T : NodeComponent => HasComponent(typeof(T));

        /// <summary>
        /// Does the node have a component of the specified type?
        /// </summary>
        /// <returns>True if the node has one or more components of the specified type.</returns>
        public bool HasComponent(Type type) => components.Where(c => c != null && c.GetType().Equals(type)).Any(); 
        #endregion

        #region Add Component
        /// <summary>
        /// Add a component of type T to the node.
        /// </summary>
        /// <typeparam name="T">Type of component to add.</typeparam>
        public T AddComponent<T>() where T : NodeComponent => (T)AddComponent(typeof(T));

        /// <summary>
        /// Add a component of specified type to the node.
        /// </summary>
        /// <param name="component">Type of component to add.</param>
        public NodeComponent AddComponent(Type component)
        {
            if (ComponentCompatibleWithThisNode(component))
            {
                var disallowMultiple = component.GetCustomAttribute<DisallowMultiple>() != null;

                if (!(HasComponent(component) && disallowMultiple))
                {
                    NodeComponent instance = NodeComponent.CreateComponent(component, this);
                    components.Add(instance);
                    return instance;
                }
            }
            return null;
        }

        /// <summary>
        /// Add this component to the Node. Will only execute if this exact component has not already been added to the node.
        /// </summary>
        /// <param name="component">Component to add.</param>
        public void AddComponent(NodeComponent component)
        {
            //You can not add the same component twice.
            if (HasComponent(component)) return;

            Type type = component.GetType();
            if (ComponentCompatibleWithThisNode(type))
            {
                var disallowMultiple = type.GetCustomAttribute<DisallowMultiple>() != null;

                if (!(HasComponent(type) && disallowMultiple))
                {
                    components.Add(component);
                }
            }
        }
        #endregion

        #region Component Compatibility
        bool ComponentCompatibleWithThisNode(Type component) => ComponentCompatibleWithNode(component, GetType());

        public static bool ComponentCompatibleWithNode(Type component, Type node)
        {
            //Compatible components must derive from INodeComponent, be a class, and not be abstract
            if (typeof(NodeComponent).IsAssignableFrom(component) && component.IsClass && !component.IsAbstract)
            {
                while (component.BaseType != typeof(NodeComponent))
                {
                    component = component.BaseType;
                }
                var generic = component.GetGenericArguments()[0];

                //If the node is derived from the generic argument of the component, the component is compatible 
                return generic.IsAssignableFrom(node);
            }
            return false;
        } 
        #endregion

        #region Remove Component
        /// <summary>
        /// Remove speciefied component from node.
        /// </summary>
        /// <param name="component">Component to remove from node.</param>
        /// <returns>True if the component was sucessfully removed.</returns>
        public bool RemoveComponent(NodeComponent component)
        {
            if (!component.IsRequiredComponent)
            {
                return components.Remove(component);
            }
            return false;
        }

        /// <summary>
        /// Remove component at <paramref name="index"/>.
        /// </summary>
        public void RemoveComponentAt(int index)
        {
            if (components[index] == null || !components[index].IsRequiredComponent)
            {
                components.RemoveAt(index);
            }
        }

        /// <summary>
        /// Remove all components of type T. Required components will not be removed.
        /// </summary>
        public void RemoveComponentsOfType<T>()
        {
            int count = components.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (components[i].GetType().Equals(typeof(T)))
                {
                    RemoveComponentAt(i);
                }
            }
        }
        #endregion

        #region Move Component
        public void MoveComponentTo(int index, NodeComponent component)
        {
            if (components.Remove(component))
            {
                index = Mathf.Clamp(index, 0, components.Count - 1);
                components.Insert(index, component);
            }
            else
            {
                Debug.LogError("Cannot move a component that is not attached to this Node");
            }
        } 
        #endregion
    }
}