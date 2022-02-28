#region Using
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#endregion

namespace Nexerate.Nodes
{
    [Serializable]
    internal class NodeDuplicator
    {
        [SerializeReference] List<Node> nodes = new();

        public static Node Duplicate(Node node)
        {
            string json = SaveAsJSON(node, false);
            return LoadFromJSON<Node>(json);
        }

        public static string SaveAsJSON(Node node, bool pretty = false)
        {
            NodeDuplicator duplicator = new();

            duplicator.CompileNodeListFromHierarchy(node);

            return JsonUtility.ToJson(duplicator, pretty);
        }

        public static T LoadFromJSON<T>(string json) where T : Node
        {
            NodeDuplicator duplicator = new();
            duplicator = JsonUtility.FromJson<NodeDuplicator>(json);

            //Reparent
            int count = duplicator.nodes.Count;
            for (int i = 0; i < count; i++)
            {
                duplicator.nodes[i].SetParent(duplicator.nodes.Where(node => node.ID == duplicator.nodes[i].parentID).FirstOrDefault(), false, false);
            }

            //Regenerate IDs
            duplicator.nodes[0].RegenerateIDs();

            return (T)duplicator.nodes[0];
        }

        void CompileNodeListFromHierarchy(Node parent)
        {
            //Root
            if (nodes.Count == 0)
            {
                nodes.Add(parent);
            }

            for (int i = 0; i < parent.ChildCount; i++)
            {
                nodes.Add(parent[i]);
                CompileNodeListFromHierarchy(parent[i]);
            }
        }
    }
}