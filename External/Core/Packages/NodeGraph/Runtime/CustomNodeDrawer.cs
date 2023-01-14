using System;

namespace NBG.NodeGraph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CustomNodeRectDrawer : Attribute
    {
        public Type type;
        public CustomNodeRectDrawer(Type type)
        {
            this.type = type;
        }
    }
}
