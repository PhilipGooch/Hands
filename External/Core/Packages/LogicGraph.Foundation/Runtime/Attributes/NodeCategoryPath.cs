using System;

namespace NBG.LogicGraph
{
    /// <summary>
    /// Categorizes the node in the UI
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Property)]
    public class NodeCategoryPathAttribute : Attribute
    {
        public string Path { get; }

        public NodeCategoryPathAttribute(string path)
        {
            this.Path = path;
        }
    }
}
