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
        [SerializeField] TreeViewState treeViewState;

        public TreeViewState TreeViewState => treeViewState;

        NodeTreeView treeView;
        NodeAsset nodeAsset;

        GUIContent defaultTitle = new("Node Hierarchy");

        public NodeAsset NodeAsset { get => nodeAsset; set => nodeAsset = value; }

        private void OnEnable()
        {
            Initialize(nodeAsset);

            NodeAssetSelectionManager.NodeAssetUnselected -= UnsetNodeAsset;
            NodeAssetSelectionManager.NodeAssetUnselected += UnsetNodeAsset;
        }

        void UnsetNodeAsset()
        {
            nodeAsset = null;
        }

        public void Initialize(NodeAsset asset)
        {
            nodeAsset = asset;

            titleContent = new($"{(asset == null ? "Node" : asset.name)} Hierarchy");

            if (asset != null)
            {
                if (treeViewState == null)
                {
                    treeViewState = new();
                }
                treeView = new(nodeAsset, treeViewState);
            }
        }

        private void OnGUI()
        {
            if (nodeAsset != null)
            {
                treeView.OnGUI(new(0, 0, position.width, position.height));
            }
            else
            {
                if(titleContent != defaultTitle)
                {
                    titleContent = defaultTitle;
                }
            }
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
            if (obj is NodeAsset node)
            {
                var window = GetWindow<NodeHierarchyWindow>();
                window.Initialize(node);
                window.Show();

                return true;//We handled the open
            }
            return false;//We did not handle the the open
        }
    }
}
