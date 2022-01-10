#region Using
using System;
#endregion

namespace Nexerate.Nodes
{
    /// <summary>
    /// Attribute added to nodes where you want some components to always be attached.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequireNodeComponents : Attribute
    {
        Type[] components;
        public Type[] Components => components;

        public RequireNodeComponents(params Type[] components)
        {
            this.components = components;
        }
    }

    /// <summary>
    /// Attribute added to components where you never want more than one component of target type on the same <see cref="Node"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DisallowMultiple : Attribute
    {

    }
}
