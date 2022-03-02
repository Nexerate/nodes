#region Using
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using System;
#endregion

namespace Nexerate.Nodes.Editor
{
    [CustomEditor(typeof(NodeAsset), true)]
    public class NodeAssetEditor : UnityEditor.Editor
    {
        NodeAsset asset;
        VisualElement nodeDrawer;

        internal static Action<bool> RefreshEditor;

        #region Theme Colors
        protected static bool DarkTheme => EditorGUIUtility.isProSkin;
        protected static Color DarkGray => DarkTheme ? new(36 / 255f, 36 / 255f, 36 / 255f) : new(161 / 255f, 161 / 255f, 161 / 255f);
        protected static Color LightGray => DarkTheme ? new(65 / 255f, 65 / 255f, 65 / 255f) : new(200 / 255f, 200 / 255f, 200 / 255f);
        protected static Color Gray => DarkTheme ? new(69 / 255f, 69 / 255f, 69 / 255f) : new(187 / 255f, 187 / 255f, 187 / 255f);
        #endregion

        #region Enable/Disable
        void OnEnable()
        {
            RefreshEditor -= Refresh;
            RefreshEditor += Refresh;

            asset = (NodeAsset)target;

            Initialize();
        }
        void OnDisable()
        {
            RefreshEditor -= Refresh;
        } 

        /// <summary>
        /// Called in OnEnable after the base class has initialized itself. 
        /// </summary>
        protected virtual void Initialize()
        {

        }

        #endregion

        public sealed override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            VisualElement assetEditor = new();
            nodeDrawer = new();

            DrawAssetEditor(assetEditor);
            Refresh();

            root.Add(assetEditor);
            root.Add(nodeDrawer);

            return root;
        }

        /// <summary>
        /// Override this function to draw a custom editor above the currently selected node in the <see cref="NodeAsset"/>. 
        /// </summary>
        protected virtual void DrawAssetEditor(VisualElement container)
        {

        }

        #region Draw Node Header
        void DrawNodeHeader(VisualElement container, Node target)
        {
            VisualElement header = new()
            {
                style =
                {
                    height = 25,
                    backgroundColor = LightGray,
                    marginLeft = -15,
                    marginRight = -6,
                    borderBottomWidth = 1,
                    borderBottomColor = DarkGray
                }
            };
            Label text = new(target.Name)
            {
                style =
                {
                    fontSize = 15,
                    height = Length.Percent(100),
                    width = Length.Percent(100),
                    unityTextAlign = TextAnchor.MiddleCenter,
                }
            };
            header.Add(text);

            container.Add(header);
        }
        #endregion

        public void Refresh(bool clearOnly = false)
        {
            serializedObject.Update();
            nodeDrawer.Clear();

            if (clearOnly) return;

            int nodeID = asset.Root.ID;

            if (EditorWindow.HasOpenInstances<NodeHierarchyWindow>())
            {
                var window = EditorWindow.GetWindow<NodeHierarchyWindow>();
                nodeID = window.TreeViewState.lastClickedID;
            }

            var node = asset.Find(nodeID) ?? asset.Root;

            int index = asset.Nodes.IndexOf(node);
            if (index == -1) return;

            DrawNodeHeader(nodeDrawer, node);

            SerializedProperty property = serializedObject.FindProperty("nodes").GetArrayElementAtIndex(index);

            PropertyField field = new();
            field.BindProperty(property);

            nodeDrawer.Add(field);
        }
    }

    [InitializeOnLoad]
    internal class PlayModeState
    {
        static PlayModeState()
        {
            EditorApplication.playModeStateChanged += ModeChanged;
        }

        /// <summary>
        /// If a <see cref="NodeHierarchyWindow"/> has a hierarchy open before we entered playmode, it will now be reopened.
        /// </summary>
        /// <param name="playModeState"></param>
        static void ModeChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredEditMode)
            {
                var asset = Selection.activeObject as NodeAsset;

                if (asset != null)
                {
                    if (EditorWindow.HasOpenInstances<NodeHierarchyWindow>())
                    {
                        var window = EditorWindow.GetWindow<NodeHierarchyWindow>();
                        window.Initialize(asset);
                    }
                }
            }
        }
    }
}