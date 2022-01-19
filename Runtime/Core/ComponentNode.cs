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
    [AddComponentMenu("Component Node")]
    public class ComponentNode : Node
    {
        #region Components
        [SerializeReference, HideInInspector] protected List<NodeComponent> components = new();
        [SerializeReference] List<Type> requiredComponents = new();
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
            var required = GetType().GetCustomAttributes<RequireNodeComponents>(true);
            if (required != null && required.Count() > 0)
            {
                //Do this for each [RequireComponents] attribute
                foreach (var attribute in required)
                {
                    //Do this for each component in the attribute
                    foreach (var component in attribute.Components)
                    {
                        if (!requiredComponents.Contains(component) && ComponentCompatibleWithThisNode(component))
                        {
                            requiredComponents.Add(component);
                            components.Add(NodeComponent.CreateComponent(component, this));
                        }
                    }
                }
            }
        } 
        public bool RequiresComponent(Type componentType) => requiredComponents.Contains(componentType);
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
        public void AddComponent<T>() where T : NodeComponent => AddComponent(typeof(T));

        /// <summary>
        /// Add a component of specified type to the node.
        /// </summary>
        /// <param name="component">Type of component to add.</param>
        public void AddComponent(Type component)
        {
            if (ComponentCompatibleWithThisNode(component))
            {
                var disallowMultiple = component.GetCustomAttribute<DisallowMultiple>() != null;

                if (!(HasComponent(component) && disallowMultiple))
                {
                    NodeComponent instance = NodeComponent.CreateComponent(component, this);
                    components.Add(instance);
                    //instance.Initialize();
                }
            }
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
            if (!requiredComponents.Contains(component.GetType()))
            {
                return components.Remove(component);
            }
            return false;
        }
        public void RemoveComponentAt(int index)
        {
            if (!requiredComponents.Contains(components[index].GetType()))
            {
                components.RemoveAt(index);
            }
        }
        #endregion
    }
}