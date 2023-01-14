using System;

namespace NBG.LogicGraph
{
    /// <summary>
    /// Hides the node in the UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Property)]
    public class NodeHideInUIAttribute : Attribute
    {
    }
}
