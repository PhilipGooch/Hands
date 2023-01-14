using NBG.Core;
using System.Collections.Generic;

namespace NBG.LogicGraph
{
    public interface ILogicGraph
    {
        ILogicGraphContainer Container { get; }
        public IReadOnlyDictionary<SerializableGuid, INode> Nodes { get; }
        public IReadOnlyDictionary<SerializableGuid, ILogicGraphVariable> Variables { get; }

        INode TryGetNode(SerializableGuid guid);
        INode GetNode(SerializableGuid guid);
    }
}
