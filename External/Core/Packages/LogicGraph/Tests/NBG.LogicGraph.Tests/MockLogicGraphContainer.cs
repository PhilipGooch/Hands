using UnityEngine;

namespace NBG.LogicGraph.Tests
{
    public class MockLogicGraphContainer : MonoBehaviour, ILogicGraphContainerCallbacks
    {
        public ILogicGraph Graph { get; set; }

        public void OnNodeAdded(Core.SerializableGuid guid, INode node)
        {
        }

        public void OnNodeRemoved(Core.SerializableGuid guid, INode node)
        {
        }
    }
}
