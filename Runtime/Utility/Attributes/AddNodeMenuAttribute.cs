#region Using
using System;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Add this attribute to a class deriving from <see cref="Node"/> to decide how it should appear in the <strong>Add Node</strong> menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AddNodeMenuAttribute : Attribute
    {
        string menuName;
        public string MenuName => menuName;

        public AddNodeMenuAttribute(string menuName)
        {
            this.menuName = menuName;
        }
    }
}