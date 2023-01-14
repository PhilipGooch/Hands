using System;

namespace NBG.LogicGraph
{
    /// <summary>
    /// Categorizes the node in the UI.
    /// </summary>
    public enum NodeConceptualType
    {
        Undefined = 0,
        EntryPoint,
        Function,
        Getter,
        FlowControl,
        TypeConverter,
    }

    /// <summary>
    /// Categorizes the node in the UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeConceptualTypeAttribute : Attribute
    {
        public NodeConceptualType Type { get; }

        public NodeConceptualTypeAttribute(NodeConceptualType type)
        {
            this.Type = type;
        }
    }
}
