#region Using
using UnityEditor;
using System;
#endregion

namespace Nexerate.Nodes.Editor
{
    [InitializeOnLoad]
    public class NodeAssetSelectionManager
    {
        #region Events
        public static event Action<NodeAsset> NodeAssetSelected;
        public static event Action NodeAssetUnselected; 
        #endregion

        static NodeAssetSelectionManager()
        {
            Selection.selectionChanged -= OnSelection;
            Selection.selectionChanged += OnSelection;
        }

        static void OnSelection()
        {
            var selection = Selection.activeObject as NodeAsset;

            if (selection is NodeAsset asset)
            {
                if (EditorWindow.HasOpenInstances<NodeHierarchyWindow>())
                {
                    NodeHierarchyWindow window = EditorWindow.GetWindow<NodeHierarchyWindow>();
                    window.Initialize(selection);
                }
                NodeAssetSelected?.Invoke(selection);
            }
            else
            {
                NodeAssetUnselected?.Invoke();
            }
        }
    }
}