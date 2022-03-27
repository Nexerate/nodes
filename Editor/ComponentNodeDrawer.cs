#region Using
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
#endregion

namespace Nexerate.Nodes.Editor
{
    [CustomPropertyDrawer(typeof(ComponentNode), true)]
    internal class ComponentNodeDrawer : PropertyDrawer
    {
        VisualElement root;
        VisualElement container;
        ComponentNode target;

        SerializedObject serializedObject;
        SerializedProperty property;

        #region Theme Colors
        static bool DarkTheme => EditorGUIUtility.isProSkin;
        static Color DarkGray => DarkTheme ? new(36 / 255f, 36 / 255f, 36 / 255f) : new(161 / 255f, 161 / 255f, 161 / 255f);
        static Color LightGray => DarkTheme ? new(65 / 255f, 65 / 255f, 65 / 255f) : new(200 / 255f, 200 / 255f, 200 / 255f);
        static Color Gray => DarkTheme ? new(69 / 255f, 69 / 255f, 69 / 255f) : new(187 / 255f, 187 / 255f, 187 / 255f); 
        #endregion

        public override VisualElement CreatePropertyGUI(SerializedProperty serializedProperty)
        {
            property = serializedProperty;
            serializedObject = serializedProperty.serializedObject;
            target = (ComponentNode)serializedProperty.managedReferenceValue;

            components = FilterCache(typeof(NodeComponent), ValidateComponent);

            root = new();
            container = new();
            root.Add(container);

            Draw();

            return root;
        }

        bool ValidateComponent(Type t)
        {
            //Target already has a component of this type that does not allow multiple components of same type to be added to the same node
            if (target.HasComponent(t) && t.GetCustomAttribute<DisallowMultiple>() != null) return false;
            
            //Type must not be abstract, and must be a class
            if (t.IsAbstract || !t.IsClass) return false;

            while(t.BaseType != typeof(NodeComponent) && t.BaseType != null)
            {
                t = t.BaseType;
            }

            var generics = t.GetGenericArguments();
            if (generics == null) return false;
            if (generics[0].IsAssignableFrom(target.GetType())) return true;
            return false;
        }

        void Draw()
        {
            DrawComponents(false);

            root.Add(AddComponentButton());
        }

        void DrawComponents(bool update)
        {
            if (update)
            {
                serializedObject.Update();
            }

            container.Clear();
            var components = property.FindPropertyRelative("components");

            for (int i = 0; i < components.arraySize; i++)
            {
                VisualElement componentContainer = new();
                int current = i;
                componentContainer.name = $"Component {current}";
                DrawComponent(componentContainer, components.GetArrayElementAtIndex(i), i);
                container.Add(componentContainer);
            }
        }

        void DrawComponent(VisualElement container, SerializedProperty component, int index)
        {

            VisualElement componentContainer = new();
            componentContainer.style.borderBottomWidth = 1;
            componentContainer.style.borderBottomColor = DarkGray;

            componentContainer.style.marginLeft = -15;
            componentContainer.style.marginRight = -6;
            componentContainer.style.paddingLeft = 15;

            Foldout foldout = ComponentFoldout(index);

            #region Field
            if (component.managedReferenceValue == null)
            {
                foldout.Add(new IMGUIContainer(() =>
                {
                    EditorGUILayout.HelpBox("Missing component!", MessageType.Warning);
                })
                {
                    style =
                    {
                        marginTop = 5,
                        marginBottom = 5
                    }
                });
            }
            else
            {
                PropertyField field = new(component);
                field.BindProperty(component);
                field.style.paddingBottom = 5;

                field.RegisterValueChangeCallback(e =>
                {
                    target.GetComponent(index).OnValidate();
                });
                foldout.Add(field);
            }
            #endregion

            componentContainer.Add(foldout);
            container.Add(componentContainer);
        }

        Foldout ComponentFoldout(int index)
        {
            Foldout foldout = new();
            var component = target.GetComponent(index);

            bool missing = component == null;

            string header = missing ? "Missing" :component.GetType().Name;

            string key = $"{header}EditorFoldout";
            if (PlayerPrefs.HasKey(key))
            {
                foldout.value = PlayerPrefs.GetInt(key) == 1;
            }
            else foldout.value = true;

            void InitializeFoldout(GeometryChangedEvent e)
            {
                foldout.UnregisterCallback<GeometryChangedEvent>(InitializeFoldout);

                foldout.RegisterValueChangedCallback(e =>
                {
                    PlayerPrefs.SetInt(key, foldout.value ? 1 : 0);
                });

                var toggle = foldout.hierarchy[0];
                toggle.style.marginRight = 0;
                toggle.style.marginBottom = 0;

                foldout.Q("unity-content").style.marginLeft = 0;
                foldout.Q("unity-content").style.marginRight = 6;

                var background = toggle.hierarchy[0];
                background.style.flexDirection = FlexDirection.Row;
                background.name = "background";
                background.style.height = 20;
                background.style.paddingLeft = 2;
                background.style.paddingTop = 2;
                background.style.backgroundColor = LightGray;
                background.style.borderBottomWidth = 1;
                background.style.borderBottomColor = DarkGray;

                #region Icon (Unused)
                /*var icon = EditorGUIUtility.ObjectContent(null, target[index].GetType());
                if (icon.image == null) icon = EditorGUIUtility.ObjectContent(null, typeof(NodeComponent));
                if (icon.image != null)
                {
                    Image image = new();
                    image.style.width = image.style.height = 16;
                    image.image = icon.image;
                    background.Add(image);
                }*/
                #endregion

                Label label = new(header.SpaceBeforeUppercase().Remove("Component").Trim());
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                background.Add(label);

                var button = new Button();
                button.clicked += () =>
                {
                    GenericMenu menu = new();
                    //We can only remove the component if it is not required by the node
                    if (!missing && component.IsRequiredComponent)
                    {
                        menu.AddDisabledItem(new("Remove Component"));
                    }
                    else
                    {
                        menu.AddItem(new("Remove Component"), false, () =>
                        {
                            RemoveComponent(index);
                        });
                    }
                    menu.ShowAsContext();
                };
                button.style.marginLeft = StyleKeyword.Auto;
                button.style.backgroundColor = Color.clear;
                button.style.width = button.style.height = 16;
                button.style.marginTop = -2;
                button.style.backgroundImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("d__Menu@2x").image);
                button.style.borderLeftWidth = button.style.borderRightWidth = button.style.borderTopWidth = button.style.borderBottomWidth = 0;
                background.Add(button);
            }
            foldout.RegisterCallback<GeometryChangedEvent>(InitializeFoldout);

            return foldout;
        }

        List<Type> FilterCache(Type type, Func<Type, bool> filter)
        {
            return Cache.NodeComponentCache.Where(t => t.IsSubclassOf(type) && filter.Invoke(t)).ToList();
        }

        List<Type> components = new();

        Button AddComponentButton()
        {
            var button = new Button()
            {
                text = "Add Component",
                style =
                {
                    marginTop = 10, marginLeft = 2 + 75, marginRight = 5 + 75, height = 25
                }
            };
            //var button = new Button(text: "Add Component").Margin(left: 2, right: 5).Height(25);
            //^Interface^
            //Would need to add Interface as a dependency to get these handy snippets.
            //Would have made this entire class cleaner.

            button.style.display = components == null || components.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            button.clicked += () =>
            {
                GenericDropdown dropdown = new("Components", "Component", components) { clickedType = AddComponent };
                Rect rect = button.worldBound;
                dropdown.Show(rect);
            };
            button.Focus();
            return button;
        }

        #region Add Component
        void AddComponent(Type component)
        {
            if (component.GetCustomAttribute<DisallowMultipleComponent>() != null)
                components.Remove(component);

            Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, "Add Component");
            target.AddComponent(component);
            Undo.FlushUndoRecordObjects();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
            DrawComponents(true);
        }
        #endregion

        #region Remove Component
        void RemoveComponent(int index)
        {
            var component = target.GetComponent(index);

            //Only way for components to be null is if their type is removed from the project
            //Therefore, there is no problem that it won't be added back to the menu
            if (component != null)
            {
                //Add component type back to the 'Add Component' menu if it wasn't already there
                if (!components.Contains(component.GetType()))
                    components.Add(component.GetType());
            }

            Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, "Remove Component");
            target.RemoveComponentAt(index);
            Undo.FlushUndoRecordObjects();

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(serializedObject.targetObject);

            DrawComponents(true);
        } 
        #endregion
    }
}