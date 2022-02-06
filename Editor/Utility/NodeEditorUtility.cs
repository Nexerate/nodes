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

        /// <summary>
        /// Copies the nodes. Only nodes that can be copied will be copied. Nodes with locked parents or nodes in locked hierarchies
        /// will not be copied.
        /// </summary>
        public static void Copy(List<Node> nodes)
        {
            Nodes = nodes.Where(node => !node.ParentLocked && !node.IsInLockedHierarchy).ToList();
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                //Remove nodes with ancestors in the selection
                //Only uppermost nodes should be duplicated
                var ancestors = Nodes[i].GetAncestors();
                if (ancestors.Intersect(Nodes).Any())
                {
                    Nodes.RemoveAt(i);
                }
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                //Duplicate the nodes to lose reference to the original nodes.
                //This prevents a bug where changes to copied nodes done after the copy were being included in the paste.
                Nodes[i] = Nodes[i].Duplicate();
            }
        }

        public static void Paste(Node parent)
        {
            if (HasNodesInClipboard)
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
}
