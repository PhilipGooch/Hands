using System;
#if UNITY_EDITOR
using System.Runtime.CompilerServices;
#endif

namespace NBG.LogicGraph
{
    [Flags]
    public enum NodeAPIFlags
    {
        Default = 0,
        ForceFlowNode = (1 << 0),
    }

    public enum NodeAPIScope
    {
        Generic = 0,
        Sim = 1,
        View = 2,
    }

    /// <summary>
    /// Exposes a member to Logic Graphs.
    /// Bindings will be generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Property)]
    public class NodeAPIAttribute : Attribute
    {
        public string Description { get; }
        public NodeAPIScope Scope { get; }
        public NodeAPIFlags Flags { get; }

#if UNITY_EDITOR
        public string SourceFile { get; }
        public int SourceLine { get; }
#endif

        public NodeAPIAttribute(string description, NodeAPIScope scope = NodeAPIScope.Generic, NodeAPIFlags flags = NodeAPIFlags.Default
#if UNITY_EDITOR
            , [CallerFilePath] string sourceFile = ""
            , [CallerLineNumber] int sourceLine = 0
#endif
            )
        {
            this.Description = description;
            this.Scope = scope;
            this.Flags = flags;
#if UNITY_EDITOR
            this.SourceFile = sourceFile;
            this.SourceLine = sourceLine;
#endif
        }
    }
}
