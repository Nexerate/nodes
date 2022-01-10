#region Using
using System;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Add this attribute to a class deriving from <see cref="NodeComponent"/> to decide how it should appear in the <strong>Add Component</strong> menu<br/>
    /// found on a <see cref="Node"/> derived from <see cref="ComponentNode"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class AddNodeComponentMenuAttribute : Attribute
    {
        public string menuName;

        public AddNodeComponentMenuAttribute(string menuName)
        {
            this.menuName = menuName;
        }
    }
}