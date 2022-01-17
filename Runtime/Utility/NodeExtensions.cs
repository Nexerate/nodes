#region Using
#endregion

namespace Nexerate.Nodes
{
    public static class NodeExtensions
    {
        public static T Rename<T>(this T node, string name) where T : Node
        {
            node.Name = name;
            return node;
        }
    }
}