#region Using
using UnityEditor.IMGUI.Controls;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor;
#endregion

namespace Nexerate.Nodes.Editor
{
    public sealed class NodeHierarchyWindow : EditorWindow
    {

        const string DefaultHeader = "Node Hierarchy";

        #region TreeView State
        [SerializeField] TreeViewState treeViewState;
        public TreeViewState TreeViewState => treeViewState; 
        #endregion

        NodeTreeView treeView;

        #region Node Asset
        [SerializeField] NodeAsset asset;
        public NodeAsset NodeAsset 
        { 
            get => asset; 
            set
            {
                asset = value;
                titleContent.text = asset == null ? DefaultHeader : asset.name;
                Repaint();
            }
        } 
        #endregion

        private void OnEnable()
        {
            Initialize(NodeAsset);
        }

        public void Initialize(NodeAsset asset)
        {
            treeViewState ??= new();

            if ((NodeAsset = asset) == null) return;
            
            treeView = new(NodeAsset, treeViewState);
        }

        private void OnGUI()
        {
            if (NodeAsset == null) return;

            treeView.OnGUI(new(0, 0, position.width, position.height));
        }

        [MenuItem("Window/Nexerate Nodes/Node Hierarchy")]
        public static void ShowWindow()
        {
            var window = GetWindow<NodeHierarchyWindow>();
            window.Initialize(null);
            window.Show();
        }

        [OnOpenAsset]
        static bool AssetOpenHierarchyWindow(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is NodeAsset asset)
            {
                var window = GetWindow<NodeHierarchyWindow>();
                window.Initialize(asset);
                window.Show();

                return true;//We handled the open
            }
            return false;//We did not handle the the open
        }
    }
}