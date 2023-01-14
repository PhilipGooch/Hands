using NBG.LogicGraph.EditorInterface;
using System;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// All currently selected graph data with controllers and UI specific actions/data
    /// </summary>
    internal class LogicGraphPlayerEditor : ILogicGraphEditor
    {
        internal LogicGraphPlayer logicGraphPlayer;
        internal GameObject graphObj;

        internal bool stateChanged;
        internal event Action<ToastNotification> onToastNotification;

        private INodesController nodesController;
        public INodesController NodesController { get => nodesController; }

        private IOperationsController operationsController;
        public IOperationsController OperationsController { get => operationsController; }

        public LogicGraphPlayerEditor(LogicGraphPlayer logicGraphPlayer, GameObject graphObj)
        {
            this.graphObj = graphObj;
            this.logicGraphPlayer = logicGraphPlayer;

            nodesController = new EditorUINodesController(logicGraphPlayer);
            operationsController = new EditorUIOperationsController(logicGraphPlayer);
        }

        //TODO? somehow attach to methods to automatically call
        internal void StateChanged()
        {
            stateChanged = true;
        }

        internal void Reset()
        {
            stateChanged = false;
        }

        internal void FireToast(ToastNotification toastNotification)
        {
            onToastNotification?.Invoke(toastNotification);
        }

        //TODO? duplicate method (NodesController)
        public void Dirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(logicGraphPlayer);
#endif
        }
    }
}
