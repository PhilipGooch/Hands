using NBG.Core;

namespace NBG.LogicGraph
{
    internal interface ILogicGraphContainerCallbacks : ILogicGraphContainer
    {
        void OnNodeAdded(SerializableGuid guid, INode node);
        void OnNodeRemoved(SerializableGuid guid, INode node);
    }
}
