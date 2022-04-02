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

        readonly GUIContent defaultTitle = new("Node Hierarchy");

        #region Node Asset
        [SerializeField] NodeAsset asset;
        public NodeAsset NodeAsset { get => asset; set => asset = value; } 
        #endregion

        private void OnEnable()
        {
            Initialize(NodeAsset);

            /*NodeAssetSelectionManager.NodeAssetUnselected -= UnsetNodeAsset;
            NodeAssetSelectionManager.NodeAssetUnselected += UnsetNodeAsset;*/
        }

        void UnsetNodeAsset()
        {
            NodeAsset = null;
            Repaint();
        }

        public void Initialize(NodeAsset asset)
        {
            NodeAsset = asset;

            titleContent = new($"{(asset == null ? "Node" : asset.name)} Hierarchy");

            if (treeViewState == null)
            {
                treeViewState = new();
            }

            if (asset != null)
            {
                treeView = new(NodeAsset, treeViewState);
            }
        }

        private void OnGUI()
        {
            if (NodeAsset != null)
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