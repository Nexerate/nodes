#region Using
using UnityEditor;
using UnityEngine;
#endregion

namespace Nexerate.Nodes.Editor
{
    [InitializeOnLoad]
    internal sealed class IconCache
    {
        public static Texture2D Node { get; private set; }
        public static Texture2D Lock { get; private set; }
        public static Texture2D Linked { get; private set; }
        public static Texture2D UnLinked { get; private set; }

        static IconCache()
        {
            CacheIcons();
        }

        static void CacheIcons()
        {
            Node = (Texture2D)EditorGUIUtility.IconContent("Occlusion@2x").image;
            Lock = (Texture2D)EditorGUIUtility.IconContent("InspectorLock").image;
            Linked = (Texture2D)EditorGUIUtility.IconContent("Linked").image;
            UnLinked = (Texture2D)EditorGUIUtility.IconContent("UnLinked").image;
        }
    }
}