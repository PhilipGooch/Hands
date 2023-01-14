using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Manages port view and port input field creation
    /// </summary>
    internal class PortView : Port
    {
        internal ulong ID => Component.InstanceId;
        internal IComponent Component { get; private set; }

        private LogicGraphPlayerEditor activeGraph;

        private VisualElement inputField;
        private VisualElement labelFieldContainer;

        private const int kWidthToMove = 150;
        private const int kDoubleLineHeight = 40;
        private const int kSingleLineHeight = 24;

        private int totalWidth;

        private Action onInputFieldValueChanged;

        //needed to properly change port color whenever, thanks graph view.
        private VisualElement portConnector;

        private float animationStartTime;
        private const float animationDuration = 1f;

        protected PortView(Orientation orientation, IComponent component, LogicGraphPlayerEditor activeGraph, Type type, Direction direction)
            : base(orientation, direction, GetCapacity(component), type)
        {
            this.activeGraph = activeGraph;
            Component = component;

            portName = Component.Name;
            portColor = GetPortColor(Component);

            m_ConnectorText.style.marginLeft = 4;
            portConnector = this.Q<VisualElement>("connector");

            labelFieldContainer = new VisualElement();
            Add(labelFieldContainer);

            if (Component.Link == ComponentLink.Data && direction == Direction.Input)
            {
                inputField = FieldsCreator.CreateVariableValueFieldFromComponent(this, Component, "", new LabelStyle(LabelStyling.auto), activeGraph.NodesController, OnInputFieldValueChanged);
                inputField.style.marginBottom = 0;
                inputField.style.marginLeft = 4;
            }
            //Hopefully this constant update cycle wont create any issues
            onInputFieldValueChanged += activeGraph.StateChanged;

            SetInputFieldEnabledState(!connected);

            RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
        }

        internal void Update(IComponent component)
        {
            Component = component;
            var newColor = GetPortColor(Component);

            if (portColor != newColor)
            {
                portColor = newColor;
                portConnector.SetBorderColor(portColor);
            }

            portName = Component.Name;

            UpdateFieldValue();
            UpdateEdgesExecutionIndication();
        }

        void UpdateEdgesExecutionIndication()
        {
            foreach (Edge connection in connections)
            {
                var currentCount = Time.frameCount;

                if (Application.isPlaying)
                {
                    var inputNode = connection.input.node as NodeView;
                    var outputNode = connection.output.node as NodeView;

                    var activationTime = inputNode == null || outputNode == null ? 0 : Mathf.Min(inputNode.Container.DebugLastActivatedFrameIndex, outputNode.Container.DebugLastActivatedFrameIndex);
                    if (currentCount == activationTime && activationTime != 0)
                    {
                        animationStartTime = Time.realtimeSinceStartup;
                    }
                }

                if (Time.realtimeSinceStartup - animationStartTime < animationDuration)
                {
                    connection.edgeControl.inputColor = (Color.Lerp(Parameters.activatedNodeColor, portColor, (Time.realtimeSinceStartup - animationStartTime) / animationDuration));
                    connection.edgeControl.outputColor = (Color.Lerp(Parameters.activatedNodeColor, portColor, (Time.realtimeSinceStartup - animationStartTime) / animationDuration));
                }
                else
                {
                    connection.edgeControl.inputColor = portColor;
                    connection.edgeControl.outputColor = portColor;
                }
            }
        }

        void UpdateFieldValue()
        {
            switch (Component.DataType)
            {
                case ComponentDataType.Node:
                    break;
                case ComponentDataType.Bool:
                    UpdateFieldValue<bool>(Component);
                    break;
                case ComponentDataType.Int:
                    UpdateFieldValue<int>(Component);
                    break;
                case ComponentDataType.Float:
                    UpdateFieldValue<float>(Component);
                    break;
                case ComponentDataType.String:
                    UpdateFieldValue<string>(Component);
                    break;
                case ComponentDataType.UnityVector3:
                    UpdateFieldValue<Vector3>(Component);
                    break;
                case ComponentDataType.UnityObject:
                    UpdateFieldValue<UnityEngine.Object>(Component);
                    break;
                case ComponentDataType.CoreGuid:
                    break;
                case ComponentDataType.UnityQuaternion:
                    UpdateVector4FieldWithQuaternionValue(Component);
                    break;
                case ComponentDataType.UnityColor:
                    UpdateFieldValue<UnityEngine.Color>(Component);
                    break;
                default:
                    throw new NotImplementedException($"{Component.DataType}");
            }
        }

        void UpdateFieldValue<T>(IComponent component)
        {
            if (component.Link == ComponentLink.Data && direction == Direction.Input)
            {
                if (connected && component.DebugLastValue != null)
                    (inputField as BaseField<T>).SetValueWithoutNotify(((IVarHandleContainerTyped<T>)component.DebugLastValue).GetValue(0));
                else if (!connected && component.Value != null)
                    (inputField as BaseField<T>).SetValueWithoutNotify(((IVarHandleContainerTyped<T>)component.Value).GetValue(0));
            }
        }

        //Quaternion field does not exist in UI Toolkit yet...
        void UpdateVector4FieldWithQuaternionValue(IComponent component)
        {
            if (component.Link == ComponentLink.Data && direction == Direction.Input)
            {
                Quaternion quaternionValue = Quaternion.identity;

                if (connected && component.DebugLastValue != null)
                    quaternionValue = ((IVarHandleContainerTyped<Quaternion>)component.DebugLastValue).GetValue(0);
                else if (!connected && component.Value != null)
                    quaternionValue = ((IVarHandleContainerTyped<Quaternion>)component.Value).GetValue(0);

                (inputField as BaseField<Vector4>).SetValueWithoutNotify(new Vector4(quaternionValue.x, quaternionValue.y, quaternionValue.z, quaternionValue.w));
            }
        }

        internal void RegisterOnInputFieldValueChanged(Action onInputFieldValueChanged)
        {
            this.onInputFieldValueChanged += onInputFieldValueChanged;
        }

        private void OnInputFieldValueChanged()
        {
            //should input field be moved to second line
            CalculateTotalWidth();
            UpdateInputField();

            activeGraph.Dirty();
            onInputFieldValueChanged?.Invoke();
        }

        //calculate total width
        private void GeometryChangedCallback(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(GeometryChangedCallback);

            CalculateTotalWidth();
            UpdateInputField();
        }

        private void CalculateTotalWidth()
        {
            totalWidth = 0;
            foreach (var item in Children())
            {
                totalWidth += (int)item.resolvedStyle.width;
            }

            foreach (var item in labelFieldContainer.Children())
            {
                totalWidth += (int)item.resolvedStyle.width;
            }
        }

        internal static PortView CreatePortView(Orientation orientation, IComponent component, Action<Vector2> onPortDragDrop, LogicGraphPlayerEditor activeGraph)
        {
            var type = component.DataType.ComponentDataTypeToSystemType();
            var direction = ComponentDirectionToDirection(component.Direction);

            var portView = new PortView(orientation, component, activeGraph, type, direction);

            portView.m_EdgeConnector = new EdgeConnector<Edge>(new BaseEdgeConnectorListener(onPortDragDrop, activeGraph));
            portView.AddManipulator(portView.m_EdgeConnector);

            return portView;
        }

        public override void Connect(Edge edge)
        {
            base.Connect(edge);

            SetInputFieldEnabledState(!connected);
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);

            SetInputFieldEnabledState(!connected);
        }

        private void SetInputFieldEnabledState(bool enabled)
        {
            if (inputField != null)
            {
                UpdateInputField();

                inputField.SetEnabled(enabled);
            }
        }

        void UpdateInputField()
        {
            if (totalWidth >= kWidthToMove)
            {
                labelFieldContainer.Add(m_ConnectorText);
                labelFieldContainer.Add(inputField);
                style.height = kDoubleLineHeight;
            }
            else
            {
                Add(m_ConnectorText);
                Add(inputField);
                style.height = kSingleLineHeight;
            }
        }

        private Color GetPortColor(IComponent component)
        {
            var type = component.DataType.ComponentDataTypeToSystemType();

            if (Parameters.PortColors.ContainsKey(type))
                return Parameters.PortColors[type];

            return Parameters.unknownPortColor;
        }

        private static Capacity GetCapacity(IComponent component)
        {
            return (component.Link == ComponentLink.Data && component.Direction == ComponentDirection.Input) ||
                   (component.Link == ComponentLink.Flow && component.Direction == ComponentDirection.Output) ?
                   Capacity.Single : Capacity.Multi;
        }

        private static Direction ComponentDirectionToDirection(ComponentDirection direction)
        {
            switch (direction)
            {
                case ComponentDirection.Input:
                    return Direction.Input;
                case ComponentDirection.Output:
                    return Direction.Output;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
