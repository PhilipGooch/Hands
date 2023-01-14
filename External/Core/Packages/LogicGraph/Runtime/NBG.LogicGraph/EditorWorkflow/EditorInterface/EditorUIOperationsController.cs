using System.Collections.Generic;
using UnityEngine;

namespace NBG.LogicGraph.EditorInterface
{
    internal class EditorUIOperationsController : IOperationsController
    {
        LogicGraphPlayer logicGraphPlayer;

        public EditorUIOperationsController(LogicGraphPlayer logicGraphPlayer)
        {
            this.logicGraphPlayer = logicGraphPlayer;
        }

        public void Cut(List<INodeContainer> nodesToCut)
        {
        }

        public void Copy(List<INodeContainer> nodesToCopy)
        {
        }

        public void Paste(Vector2 position)
        {
        }

        public void Duplicate(List<INodeContainer> nodesToDuplicate)
        {
        }
    }
}
