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

        NodeTreeView treeView;
        NodeAsset nodeAsset;

        public NodeAsset NodeAsset { get => nodeAsset; set => nodeAsset = value; }

        private void OnEnable()
        {
            Initialize(nodeAsset);
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
                titleContent = new("Node Hierarchy");
            }
        }

        [MenuItem("Node/Node Hierarchy")]
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
