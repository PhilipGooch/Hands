using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class EventNodeBuilder : VisualElement
    {
        private const string k_UXMLGUID = "e239693f7492b6742814281c467c6beb";
        public new class UxmlFactory : UxmlFactory<EventNodeBuilder, VisualElement.UxmlTraits> { }

        private VisualElement fieldsContainer;

        private Button removeParameter;
        private Button addParameter;

        private Dictionary<ulong, EventParameterFieldView> eventParameterFieldViews = new Dictionary<ulong, EventParameterFieldView>();

        private EventParameterFieldView selected;

        private LogicGraphPlayerEditor activeGraph;
        private INodeContainer container;

        public EventNodeBuilder()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            fieldsContainer = this.Q<VisualElement>("fieldsContainer");

            removeParameter = this.Q<Button>("removeParameter");
            addParameter = this.Q<Button>("addParameter");
            removeParameter.focusable = true;
            addParameter.focusable = true;

            removeParameter.clickable.clicked += RemoveParameter;
            addParameter.clickable.clicked += AddParameter;
        }

        internal void Initialize(LogicGraphPlayerEditor activeGraph)
        {
            this.activeGraph = activeGraph;
        }

        internal void Update(INodeContainer container)
        {
            this.container = container;

            var canModifyList = container.CanModifyDynamicIOList;
            removeParameter.SetEnabled(canModifyList);
            addParameter.SetEnabled(canModifyList);

            List<ulong> toRemove = eventParameterFieldViews.Keys.ToList();

            foreach (var item in container.EditableComponents)
            {
                if (!item.Hidden)
                {
                    toRemove.Remove(item.InstanceId);

                    if (eventParameterFieldViews.ContainsKey(item.InstanceId))
                        eventParameterFieldViews[item.InstanceId].Update(item);
                    else
                        AddParameterView(item);
                }
            }

            foreach (var item in toRemove)
            {
                fieldsContainer.Remove(eventParameterFieldViews[item]);
                eventParameterFieldViews.Remove(item);
            }
        }

        private void AddParameter()
        {
            activeGraph.NodesController.AddField(container, VariableType.Bool, "tempVarName");
            activeGraph.StateChanged();
        }

        private void AddParameterView(IComponent component)
        {
            EventParameterFieldView eventParameterFieldView = new EventParameterFieldView();
            eventParameterFieldView.Initialize(activeGraph, container, component);
            eventParameterFieldView.AddManipulator(new SelectionManipulator<EventParameterFieldView>(OnParamterFieldViewFocused));

            fieldsContainer.Add(eventParameterFieldView);
            eventParameterFieldViews.Add(component.InstanceId, eventParameterFieldView);
        }

        private void OnParamterFieldViewUnfocused(bool fullDeselect)
        {
            selected.Deselect();

            if (fullDeselect)
            {
                selected = null;
            }
        }

        private void OnParamterFieldViewFocused(EventParameterFieldView eventParameterFieldView)
        {
            if (selected != null)
                OnParamterFieldViewUnfocused(true);

            selected = eventParameterFieldView;
            selected.Select();
        }

        private void RemoveParameter()
        {
            if (selected != null)
            {
                activeGraph.NodesController.RemoveField(container, selected.component);
                selected = null;
                activeGraph.StateChanged();
            }
        }
    }
}