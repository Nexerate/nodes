#region Using
using System.Collections.Generic;
using System.Linq; 
#endregion

namespace Nexerate.Nodes.Editor
{
    public static class NodeEditorUtility
    {
        static List<Node> Nodes = new();

        public static bool HasNodesInClipboard => Nodes != null && Nodes.Count > 0;

        public static void Copy(List<Node> nodes)
        {
            Nodes = nodes.Where(node => !node.ParentLocked).ToList();
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                //Remove nodes with ancestors in the selection
                //Only uppermost nodes should be duplicated
                var ancestors = nodes[i].GetAncestors();
                if (ancestors.Intersect(nodes).Any())
                {
                    nodes.RemoveAt(i);
                }
            }
        }

        public static void Paste(Node parent)
        {
            if (parent.ChildrenLocked || parent.HierarchyLocked || parent.IsInLockedHierarchy) return;

            for (int i = 0; i < Nodes.Count; i++)
            {
                var duplicate = Nodes[i].Duplicate();
                duplicate.SetParent(parent);
            }
        }
    }
}
