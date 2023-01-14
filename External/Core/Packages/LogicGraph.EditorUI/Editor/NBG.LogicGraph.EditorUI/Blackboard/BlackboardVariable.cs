using NBG.LogicGraph.EditorInterface;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class BlackboardVariable : VisualElement
    {
        internal IVariableContainer variable { get; set; }
        internal LogicGraphPlayerEditor activeGraph { get; set; }

        internal void SetupDrag(VariableDragView dragView)
        {
            this.AddManipulator(new ElementDragger<BlackboardVariable>(dragView));
        }

        internal void DeleteVariable()
        {
            if (activeGraph != null)
            {
                activeGraph.NodesController.RemoveVariable(variable.ID);
                activeGraph.StateChanged();
            }
        }
    }
}