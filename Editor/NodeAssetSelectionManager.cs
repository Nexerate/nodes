#region Using
using UnityEditor;
using System;
#endregion

namespace Nexerate.Nodes.Editor
{
    [InitializeOnLoad]
    internal class NodeAssetSelectionManager
    {
        public static event Action NodeAssetUnselected;
        static NodeAssetSelectionManager()
        {
            Selection.selectionChanged -= OnSelection;
            Selection.selectionChanged += OnSelection;
        }

        static void OnSelection()
        {
            var asset = Selection.activeObject as NodeAsset;

            if (asset != null)
            {
                if (typeof(NodeAsset).IsAssignableFrom(asset.GetType()))
                {
                    if (EditorWindow.HasOpenInstances<NodeHierarchyWindow>())
                    {
                        NodeHierarchyWindow window = EditorWindow.GetWindow<NodeHierarchyWindow>();
                        window.Initialize(asset);
                    }
                }
            }
            else
            {
                NodeAssetUnselected?.Invoke();
            }
        }
    }

    internal class NodeAssetCreationManager : AssetPostprocessor
    {
        static void OnPostProcessAsset()
        {

        }
    }
}