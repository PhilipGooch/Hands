using System;

namespace NBG.LogicGraph
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeSerializationAttribute : Attribute
    {
        public string SerializedName { get; }

        public NodeSerializationAttribute(string serializedName)
        {
            this.SerializedName = serializedName;
        }
    }
}
