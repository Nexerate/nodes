#region Using
using UnityEditor;
#endregion

namespace Nexerate.Nodes.Editor
{
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