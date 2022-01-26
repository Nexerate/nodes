#region Using
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#endregion

namespace Nexerate.Nodes.Editor
{
    /// <summary>
    /// <see cref="NodeComponentDrawer"/> is the default drawer for node components where a drawer is not specified.
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeComponent), true)]
    public class NodeComponentDrawer : PropertyDrawer
    {
        #region Fields
        private SerializedProperty property;
        private SerializedObject serializedObject;
        #endregion

        #region Properties
        protected SerializedProperty Property => property;
        protected SerializedObject SerializedObject => serializedObject;
        #endregion

        #region Shorthands
        /// <summary>
        /// Shorthand for writing <i>property.FindPropertyRelative(name);</i>
        /// </summary>
        protected SerializedProperty Relative(SerializedProperty property, string name) => property.FindPropertyRelative(name);

        /// <summary>
        /// Shorthand for writing <i>new PropertyField(property.FindPropertyRelative(name));</i>
        /// </summary>
        protected PropertyField RelativeField(SerializedProperty property, string name, string label = null, string tooltip = null) => new(Relative(property, name), label) { tooltip = tooltip ?? "" };

        /// <summary>
        /// Shorthand for writing <i>new PropertyField(property);</i>
        /// </summary>
        protected PropertyField Field(SerializedProperty property, string label = null, string tooltip = null) => new(property, label) { tooltip = tooltip ?? "" };
        #endregion

        public override VisualElement CreatePropertyGUI(SerializedProperty serializedProperty)
        {
            property = serializedProperty;
            serializedObject = property.serializedObject;

            Initialize();

            VisualElement container = new();

            Draw(container);

            return container;
        }

        public virtual void Draw(VisualElement container)
        {
            var fields = property.managedReferenceValue.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach(FieldInfo field in fields)
            {
                if (field.GetCustomAttribute<HideInInspector>() != null) continue;

                var serialize = field.GetCustomAttribute<SerializeField>();
                var serializeReference = field.GetCustomAttribute<SerializeReference>();
                if (serialize != null || serializeReference != null || field.IsPublic)
                {
                    //var field = RelativeField(property, field.name);
                    container.Add(RelativeField(property, field.Name));
                }
            }
        }

        internal virtual void Initialize() { }
    }
    public abstract class NodeComponentDrawer<T> : NodeComponentDrawer where T : NodeComponent
    {
        #region Fields
        private T component;
        #endregion

        #region Properties
        protected T Component => component;
        #endregion

        #region Initialize
        internal override void Initialize()
        {
            component = (T)Property.managedReferenceValue;
        }
        #endregion
    }
}
