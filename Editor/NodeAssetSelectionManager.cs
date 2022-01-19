#region Using
using UnityEditor;
#endregion

namespace Nexerate.Nodes.Editor
{
    [InitializeOnLoad]
    internal class NodeAssetSelectionManager
    {
        static NodeAssetSelectionManager()
        {
            Selection.selectionChanged -= OnSelection;
            Selection.selectionChanged += OnSelection;
        }

        static void OnSelection()
        {
            var asset = Selection.activeObject;

            if (asset != null)
            {
                if (typeof(NodeAsset).IsAssignableFrom(asset.GetType()))
                {
                    if (EditorWindow.HasOpenInstances<NodeHierarchyWindow>())
                    {
                        NodeHierarchyWindow window = EditorWindow.GetWindow<NodeHierarchyWindow>();
                        window.Initialize(asset as NodeAsset);
                    }
                }
            }
            else
            {
                if (EditorWindow.HasOpenInstances<NodeHierarchyWindow>())
                {
                    NodeHierarchyWindow window = EditorWindow.GetWindow<NodeHierarchyWindow>();
                    window.NodeAsset = null;
                }
            }
        }
    }
}