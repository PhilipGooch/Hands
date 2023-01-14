using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Hotkeys
    /// </summary>
    internal class LogicGraphEditorShortcuts
    {
        private LogicGraphPlayerEditor activeGraph;
        private LogicGraphView logicGraphView;

        internal void Initialize(LogicGraphPlayerEditor activeGraph, LogicGraphView logicGraphView)
        {
            this.activeGraph = activeGraph;
            this.logicGraphView = logicGraphView;
        }

        internal void OnKeyDown(KeyDownEvent ev)
        {
            switch (ev.keyCode)
            {
                case KeyCode.C:
                    if (ev.ctrlKey)
                    {
                        activeGraph.OperationsController.Copy(logicGraphView.GetSelectedNodes());
                    }
                    break;
                case KeyCode.X:
                    if (ev.ctrlKey)
                    {
                        activeGraph.OperationsController.Cut(logicGraphView.GetSelectedNodes());
                        activeGraph.StateChanged();
                    }
                    break;
                case KeyCode.V:
                    if (ev.ctrlKey)
                    {
                        activeGraph.OperationsController.Paste(logicGraphView.MousePosition);
                        activeGraph.StateChanged();
                    }
                    break;
                case KeyCode.D:
                    if (ev.ctrlKey)
                    {
                        activeGraph.OperationsController.Duplicate(logicGraphView.GetSelectedNodes());
                        activeGraph.StateChanged();
                    }
                    break;
                case KeyCode.A: //select all
                    if (ev.ctrlKey)
                    {
                        logicGraphView.SelectAllViews();
                        activeGraph.StateChanged();
                    }
                    break;
            }
        }
    }
}
