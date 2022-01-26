#region Using
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System;
#endregion

namespace Nexerate.Nodes.Editor
{
    [InitializeOnLoad]
    internal sealed class Cache
    {
        public static List<Type> NodeComponentCache { get; private set; }
        public static List<Type> NodeCache { get; private set; }
        static Cache()
        {
            CacheNodesAndComponents();
        }

        static void CacheNodesAndComponents()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            NodeComponentCache = new List<Type>();
            NodeCache = new List<Type>();

            for (int i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes();
                NodeComponentCache.AddRange(types.Where(t => typeof(NodeComponent).IsAssignableFrom(t)));
                NodeCache.AddRange(types.Where(t => typeof(Node).IsAssignableFrom(t)));
            }
        }
    }
}