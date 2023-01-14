using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    internal class EventParameterFieldView : VisualElement
    {
        private const string k_UXMLGUID = "2d8ac5169a9d3c04bb140eebbd6e22dc";
        public new class UxmlFactory : UxmlFactory<EventParameterFieldView, VisualElement.UxmlTraits> { }

        private TextField parameterName;
        private VisualElement parameterNameInputField;
        private EnumField parameterType;

        private Color original;

        private LogicGraphPlayerEditor activeGraph;
        private INodeContainer container;
        internal IComponent component;

        public EventParameterFieldView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);
            parameterName = this.Q<TextField>("variableNameField");
            parameterName.RegisterCallback<ChangeEvent<string>>(ParamterNameChanged);

            parameterNameInputField = parameterName.QInputField<string>();
            parameterNameInputField.style.minWidth = 60;

            parameterType = this.Q<EnumField>("variableTypeField");
            parameterType.Init(VariableTypeUI.Bool);
            parameterType.value = VariableTypeUI.Bool;
            parameterType.RegisterCallback<ChangeEvent<Enum>>(ConstantTypeChanged);

            var parameterTypeLabel = this.Q<Label>();
            parameterTypeLabel.style.minWidth = 50;

            original = style.backgroundColor.value;
        }

        public void Initialize(LogicGraphPlayerEditor activeGraph, INodeContainer container, IComponent component)
        {
            this.activeGraph = activeGraph;
            this.container = container;

            Update(component);
        }

        private void ConstantTypeChanged(ChangeEvent<Enum> evt)
        {
            var varType = (VariableTypeUI)evt.newValue;
            activeGraph.NodesController.ChangeFieldType(container, component, varType.VariableTypeUIToVariableType());
            activeGraph.StateChanged();
        }

        internal void Update(IComponent component)
        {
            this.component = component;
            parameterName.value = component.Name;
            parameterType.value = component.DataType.ComponentDataTypeToVariableTypeUI();
        }

        private void ParamterNameChanged(ChangeEvent<string> evt)
        {
            activeGraph.NodesController.ChangeFieldName(container, component, evt.newValue);
            activeGraph.StateChanged();
        }

        internal void Deselect()
        {
            style.backgroundColor = original;
        }

        internal void Select()
        {
            style.backgroundColor = Parameters.fieldSelectionColor;
        }
    }
}