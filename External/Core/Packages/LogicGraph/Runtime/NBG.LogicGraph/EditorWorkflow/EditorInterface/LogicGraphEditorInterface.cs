using NBG.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.LogicGraph.EditorInterface
{
    public enum Severity
    {
        Info,
        Warning,
        Error
    }

    internal enum ContainerType
    {
        Generic,
        Comment,
        Group,
    }

    internal enum ComponentDirection
    {
        Input,
        Output,
        Embed,
    }

    internal enum ComponentLink
    {
        Data,
        Flow,
    }

    internal enum ComponentDataType
    {
        Node, // Flow link
        Bool,
        Int,
        Float,
        String,
        UnityVector3,
        UnityObject,
        CoreGuid,
        UnityQuaternion,
        UnityColor,
    }

    internal interface IComponent
    {
        INodeContainer Owner { get; }

        ulong InstanceId { get; }

        ComponentDirection Direction { get; }
        ComponentLink Link { get; }

        /// <summary>
        /// User facing name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Is this component hidden (e.g. internal).
        /// </summary>
        bool Hidden { get; }

        bool IsFlowInput { get; }

        /// <summary>
        /// Type of data being carried.
        /// </summary>
        ComponentDataType DataType { get; }

        /// <summary>
        /// Component being referenced.
        /// </summary>
        IComponent Target { get; }

        bool SupportsMultipleConnections { get; }

        /// <summary>
        /// Value of DataType type
        /// </summary>
        IVarHandleContainer Value { get; }

        /// <summary>
        /// Debugging: last value of DataType type during execution
        /// </summary>
        IVarHandleContainer DebugLastValue { get; }

        /// <summary>
        /// Real type. (e.g. if DataType is UnityObject, this might contain the type of the MonoBehaviour)
        /// </summary>
        public Type BackingType { get; }

        /// <summary>
        /// Optional list of the possible variants.
        /// </summary>
        public IVariantProvider VariantProvider { get; }
    }

    internal interface INodeContainer
    {
        ILogicGraph Owner { get; }

        SerializableGuid ID { get; }

        ContainerType ContainerType { get; }

        NodeConceptualType NodeType { get; }

        NodeAPIScope Scope { get; }

        /// <summary>
        /// Friendly name of the container.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Context of the container (e.g. Unity component reference).
        /// null when container does not support references.
        /// </summary>
        string DisplayContext { get; }

        bool HasDynamicFlowOutputs { get; }
        bool HasDynamicIO { get; }
        bool CanModifyDynamicIOList { get; }

        IReadOnlyList<IComponent> Components { get; }
        IEnumerable<IComponent> EditableComponents { get; }

        /// <summary>
        /// Frame index of last activation.
        /// Based on Time.frameCount.
        /// </summary>
        int DebugLastActivatedFrameIndex { get; }

        /// <summary>
        /// Absolute local path of a relevant source file.
        /// </summary>
        string SourceFile { get; }

        /// <summary>
        /// Line number in `SourceFile`.
        /// </summary>
        int SourceLine { get; }
    }

    internal interface IContainerUI
    {
        Rect Rect { get; set; }
        Color Color { get; set; }
    }

    internal interface INodeObjectReference
    {
        List<(string segment, UnityEngine.Object relativeObj)> GetPathListType(NodeEntry entry);
        List<(string segment, UnityEngine.Object relativeObj)> GetPathHierarchyType(UnityEngine.GameObject relative);

        UnityEngine.Object Target { get; set; }
    }

    internal struct NodeEntry
    {
        public string description;
        public string categoryPath;

        public Type archetype;
        public Type bindingType;
        public string bindingName;

        public NodeConceptualType conceptualType;

        public INodeObjectReference reference;
    }

    internal struct ToastNotification
    {
        internal Severity severity;
        internal string header;
        internal string message;
        internal Action onClick;

        public ToastNotification(Severity severity, string header, string message, Action onClick)
        {
            this.severity = severity;
            this.header = header;
            this.message = message;
            this.onClick = onClick;
        }
    }

    internal interface IVariableContainer
    {
        SerializableGuid ID { get; }

        string Name { get; }

        VariableType Type { get; }

        IVarHandleContainer Variable { get; }
    }

    internal interface INodesController
    {
        IEnumerable<INodeContainer> GetNodes();

        IEnumerable<NodeEntry> NodeTypes { get; }

        INodeContainer CreateNode(NodeEntry nodeEntry, INodeObjectReference reference);

        string CheckNodeForErrors(INodeContainer node);

        void RemoveNode(INodeContainer toRemove);

        void ConnectNodes(IComponent output, IComponent input);

        void DisconnectNodes(IComponent output, IComponent input);

        bool CanConnectPorts(IComponent output, IComponent input);
        bool CanConnectPortsSemantic(IComponent output, IComponent input, out string errorMsg);

        public void UpdateVariableValue<T>(IVarHandleContainer container, T value);

        void AddFlowPort(INodeContainer node, string name, INodeContainer target = null);

        void RemoveFlowPort(INodeContainer node, IComponent port);

        void UpdateRect(INodeContainer toMove, Rect rect);


        //Comments
        string GetCommentName(INodeContainer container);
        void SetCommentName(INodeContainer container, string name);

        string GetCommentContent(INodeContainer container);
        void SetCommentContent(INodeContainer container, string content);

        //Groups
        void AddNodeToGroup(INodeContainer container, INodeContainer toAdd);

        void RemoveNodeFromGroup(INodeContainer container, INodeContainer toRemove);

        string GetGroupName(INodeContainer container);
        void SetGroupName(INodeContainer container, string name);

        IEnumerable<INodeContainer> GetGroupChildren(INodeContainer container);

        void UpdateColor(INodeContainer container, Color color);

        //Event Builder
        void AddField(INodeContainer owner, VariableType type, string name);

        void RemoveField(INodeContainer owner, IComponent component);

        void ChangeFieldType(INodeContainer owner, IComponent component, VariableType newType);

        void ChangeFieldName(INodeContainer owner, IComponent component, string newName);

        //Blackboard
        SerializableGuid AddVariable(string name, VariableType type);

        void RemoveVariable(SerializableGuid id);

        void ChangeVariableType(SerializableGuid variableId, VariableType newType);

        void ChangeVariableName(SerializableGuid variableId, string newName);

        INodeContainer CreateVariableNode(SerializableGuid variableId);

        IEnumerable<IVariableContainer> GetVariables();
    }

    internal interface IOperationsController
    {
        public void Cut(List<INodeContainer> nodesToCut);

        public void Copy(List<INodeContainer> nodesToCopy);

        public void Paste(Vector2 position);

        public void Duplicate(List<INodeContainer> nodesToDuplicate);

    }

    internal interface ILogicGraphEditor
    {
        INodesController NodesController { get; }

        IOperationsController OperationsController { get; }
    }

    public enum VariableTypeUI
    {
        Bool,
        Int,
        Float,
        String,
        UnityVector3,
        UnityObject,
        Quaternion,
        UnityColor,
    }

    internal static class EditorInterfaceUtils
    {
        public static ComponentDataType VariableTypeToComponentDataType(VariableType type)
        {
            switch (type)
            {
                case VariableType.Bool:
                    return ComponentDataType.Bool;
                case VariableType.Int:
                    return ComponentDataType.Int;
                case VariableType.Float:
                    return ComponentDataType.Float;
                case VariableType.String:
                    return ComponentDataType.String;
                case VariableType.UnityVector3:
                    return ComponentDataType.UnityVector3;
                case VariableType.UnityObject:
                    return ComponentDataType.UnityObject;
                case VariableType.Guid:
                    return ComponentDataType.CoreGuid;
                case VariableType.Quaternion:
                    return ComponentDataType.UnityQuaternion;
                case VariableType.Color:
                    return ComponentDataType.UnityColor;
                default:
                    throw new NotSupportedException();
            }
        }

        public static Type ComponentDataTypeToSystemType(this ComponentDataType dataType)
        {
            switch (dataType)
            {
                case ComponentDataType.Node:
                    return typeof(INode);
                case ComponentDataType.Bool:
                    return typeof(bool);
                case ComponentDataType.Int:
                    return typeof(int);
                case ComponentDataType.Float:
                    return typeof(float);
                case ComponentDataType.String:
                    return typeof(string);
                case ComponentDataType.UnityVector3:
                    return typeof(UnityEngine.Vector3);
                case ComponentDataType.UnityObject:
                    return typeof(UnityEngine.Object);
                case ComponentDataType.UnityQuaternion:
                    return typeof(UnityEngine.Quaternion);
                case ComponentDataType.UnityColor:
                    return typeof(UnityEngine.Color);
                default:
                    throw new NotSupportedException();
            }
        }

        internal static VariableTypeUI VariableTypeToVariableTypeUI(this VariableType variableType)
        {
            switch (variableType)
            {
                case VariableType.Bool:
                    return VariableTypeUI.Bool;
                case VariableType.Int:
                    return VariableTypeUI.Int;
                case VariableType.Float:
                    return VariableTypeUI.Float;
                case VariableType.String:
                    return VariableTypeUI.String;
                case VariableType.UnityVector3:
                    return VariableTypeUI.UnityVector3;
                case VariableType.UnityObject:
                    return VariableTypeUI.UnityObject;
                case VariableType.Quaternion:
                    return VariableTypeUI.Quaternion;
                case VariableType.Color:
                    return VariableTypeUI.UnityColor;
                default:
                    throw new NotSupportedException();
            }
        }

        internal static VariableType VariableTypeUIToVariableType(this VariableTypeUI variableType)
        {
            switch (variableType)
            {
                case VariableTypeUI.Bool:
                    return VariableType.Bool;
                case VariableTypeUI.Int:
                    return VariableType.Int;
                case VariableTypeUI.Float:
                    return VariableType.Float;
                case VariableTypeUI.String:
                    return VariableType.String;
                case VariableTypeUI.UnityVector3:
                    return VariableType.UnityVector3;
                case VariableTypeUI.UnityObject:
                    return VariableType.UnityObject;
                case VariableTypeUI.Quaternion:
                    return VariableType.Quaternion;
                case VariableTypeUI.UnityColor:
                    return VariableType.Color;
                default:
                    throw new NotSupportedException();
            }
        }

        public static VariableTypeUI ComponentDataTypeToVariableTypeUI(this ComponentDataType type)
        {
            switch (type)
            {
                case ComponentDataType.Bool:
                    return VariableTypeUI.Bool;
                case ComponentDataType.Int:
                    return VariableTypeUI.Int;
                case ComponentDataType.Float:
                    return VariableTypeUI.Float;
                case ComponentDataType.String:
                    return VariableTypeUI.String;
                case ComponentDataType.UnityVector3:
                    return VariableTypeUI.UnityVector3;
                case ComponentDataType.UnityObject:
                    return VariableTypeUI.UnityObject;
                case ComponentDataType.UnityQuaternion:
                    return VariableTypeUI.Quaternion;
                case ComponentDataType.UnityColor:
                    return VariableTypeUI.UnityColor;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
