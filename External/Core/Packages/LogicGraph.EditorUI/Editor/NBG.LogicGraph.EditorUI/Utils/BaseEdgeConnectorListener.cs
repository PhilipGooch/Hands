using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI
{
    internal class BaseEdgeConnectorListener : IEdgeConnectorListener
    {
        private Action<Vector2> onPortDragDrop;
        private LogicGraphPlayerEditor activeGraph;

        public BaseEdgeConnectorListener(Action<Vector2> onPortDragDrop, LogicGraphPlayerEditor activeGraph)
        {
            this.activeGraph = activeGraph;
            this.onPortDragDrop = onPortDragDrop;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            onPortDragDrop?.Invoke(position);
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            var input = (PortView)edge.input;
            var output = (PortView)edge.output;


            if (activeGraph.NodesController.CanConnectPortsSemantic(input.Component, output.Component, out var msg))
            {
                activeGraph.NodesController.ConnectNodes(input.Component, output.Component);
                activeGraph.StateChanged();
            }
            else
            {
                activeGraph.FireToast(new ToastNotification(Severity.Warning, "Node connection not viable!", msg, null));
            }
        }
    }
}
