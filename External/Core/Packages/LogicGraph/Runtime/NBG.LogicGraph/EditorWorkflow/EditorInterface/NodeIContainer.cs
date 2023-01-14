using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using NBG.LogicGraph.Nodes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NBG.LogicGraph
{
    abstract partial class Node : INodeContainer, IContainerUI
    {
        SerializableGuid INodeContainer.ID => GetID();

        SerializableGuid GetID()
        {
            foreach (var item in Owner.Nodes)
            {
                if (item.Value == this)
                    return item.Key;
            }

            return default;
        }

        ContainerType INodeContainer.ContainerType
        {
            get
            {
                if (this is GroupNode)
                    return ContainerType.Group;
                else if (this is CommentNode)
                    return ContainerType.Comment;
                else
                    return ContainerType.Generic;
            }
        }

        NodeConceptualType INodeContainer.NodeType
        {
            get
            {
                var attr = this.GetType().GetCustomAttribute<NodeConceptualTypeAttribute>(inherit: true);
                if (attr != null)
                    return attr.Type;

                if (this is BindingNode) //TODO: abstract
                {
                    var bn = (BindingNode)this;
                    var binding = bn.Binding;
                    Debug.Assert(binding != null);
                    if (binding.ConceptualType != NodeConceptualType.Undefined)
                        return binding.ConceptualType;
                    else if (this.HasFlowInput || this.FlowOutputs.Count > 0)
                        return NodeConceptualType.Function;
                    else
                        return NodeConceptualType.Getter;
                }

                return NodeConceptualType.Undefined;
            }
        }

        string INodeContainer.DisplayName => this.Name;

        string INodeContainer.DisplayContext
        {
            get
            {
                var context = this as INodeObjectContext;
                if (context == null)
                {
                    return string.Empty;
                }
                else
                {
                    var obj = context.ObjectContext;
                    if (obj == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return $"Target is {obj.name} ({obj.GetType().Name})";
                    }
                }
            }
        }

        bool INodeContainer.HasDynamicFlowOutputs => (this is INodeCustomFlow);

        bool INodeContainer.HasDynamicIO => (this is INodeCustomIO);
        bool INodeContainer.CanModifyDynamicIOList => ((this is INodeCustomIO) && ((INodeCustomIO)this).CanAddAndRemove);

        List<EditorInterface.Component> _components;
        internal bool ComponentsNeedRefresh;
        void RefreshComponents()
        {
            _components ??= new List<EditorInterface.Component>();

            if (HasFlowInput)
            {
                var co = _components.SingleOrDefault(x => x._userData == null);
                if (co == null)
                    AddFlowInputComponent();
            }

            // Flow outputs
            var deleted = _components.Where(x => x._userData is FlowOutput).Where(x => !_flowOutputs.Contains(x._userData));
            _components.RemoveAll(x => deleted.Contains(x));
            for (int i = 0; i < _flowOutputs.Count; ++i)
            {
                var co = _components.SingleOrDefault(x => x._userData == _flowOutputs[i]);
                if (co == null)
                    AddFlowOutputComponent(i);
            }

            // Stack inputs
            deleted = _components.Where(x => x._userData is StackInput).Where(x => !_stackInputs.Contains(x._userData));
            _components.RemoveAll(x => deleted.Contains(x));
            for (int i = 0; i < _stackInputs.Count; ++i)
            {
                var co = _components.SingleOrDefault(x => x._userData == _stackInputs[i]);
                if (co == null)
                    AddStackInputComponent(i);
            }

            // Stack outputs
            deleted = _components.Where(x => x._userData is StackOutput).Where(x => !_stackOutputs.Contains(x._userData));
            _components.RemoveAll(x => deleted.Contains(x));
            for (int i = 0; i < _stackOutputs.Count; ++i)
            {
                var co = _components.SingleOrDefault(x => x._userData == _stackOutputs[i]);
                if (co == null)
                    AddStackOutputComponent(i);
            }

            // Properties
            deleted = _components.Where(x => x._userData is NodeProperty).Where(x => !_properties.Contains(x._userData));
            _components.RemoveAll(x => deleted.Contains(x));
            for (int i = 0; i < _properties.Count; ++i)
            {
                var co = _components.SingleOrDefault(x => x._userData == _properties[i]);
                if (co == null)
                    AddPropertyComponent(i);
            }
        }

        void LinkComponents()
        {
            foreach (var co in _components)
            {
                LinkComponent(co);
            }
        }

        EditorInterface.Component AddFlowInputComponent()
        {
            var co = new EditorInterface.Component();
            co.Owner = this;
            co._userData = null;
            co.Direction = ComponentDirection.Input;
            co.Link = ComponentLink.Flow;
            co.Name = "in";
            co.Hidden = false;
            co.DataType = ComponentDataType.Node;
            co.Target = null;
            co.SupportsMultipleConnections = true;
            co.Value = null;
            co.DebugLastValue = null;
            _components.Add(co);
            return co;
        }

        EditorInterface.Component AddFlowOutputComponent(int i)
        {
            var fo = _flowOutputs[i];
            var co = new EditorInterface.Component();
            co.Owner = this;
            co._userData = fo;
            co._getUserDataSlotIndex = () => { return _flowOutputs.IndexOf(fo); };
            co.Direction = ComponentDirection.Output;
            co.Link = ComponentLink.Flow;
            co.Name = fo.Name;
            co.Hidden = false;
            co.DataType = ComponentDataType.Node;
            co.Target = null; // Link
            co.SupportsMultipleConnections = false;
            co.Value = null;
            co.DebugLastValue = null;
            _components.Add(co);
            return co;
        }

        EditorInterface.Component AddStackInputComponent(int i)
        {
            var si = _stackInputs[i];
            var co = new EditorInterface.Component();
            co.Owner = this;
            co._userData = si;
            co._getUserDataSlotIndex = () => { return _stackInputs.IndexOf(si); };
            co.Direction = ComponentDirection.Input;
            co.Link = ComponentLink.Data;
            co.Name = si.Name;
            co.Hidden = false;
            co.DataType = EditorInterfaceUtils.VariableTypeToComponentDataType(si.Type);
            co.BackingType = si.BackingType;
            co.VariantProvider = si.variants;
            co.Target = null; // Link
            co.SupportsMultipleConnections = false;
            co.Value = si.variable;
            co.DebugLastValue = si.debugLastValue;
            _components.Add(co);
            return co;
        }

        EditorInterface.Component AddStackOutputComponent(int i)
        {
            var so = _stackOutputs[i];
            var co = new EditorInterface.Component();
            co.Owner = this;
            co._userData = so;
            co._getUserDataSlotIndex = () => { return _stackOutputs.IndexOf(so); };
            co.Direction = ComponentDirection.Output;
            co.Link = ComponentLink.Data;
            co.Name = so.Name;
            co.Hidden = false;
            co.DataType = EditorInterfaceUtils.VariableTypeToComponentDataType(so.Type);
            co.Target = null;
            co._userDataRefIndex = i;
            co.SupportsMultipleConnections = true;
            co.Value = null;
            co.DebugLastValue = null;
            _components.Add(co);
            return co;
        }

        EditorInterface.Component AddPropertyComponent(int i)
        {
            var p = _properties[i];
            var co = new EditorInterface.Component();
            co.Owner = this;
            co._userData = p;
            co._getUserDataSlotIndex = () => { return _properties.IndexOf(p); };
            co.Direction = ComponentDirection.Embed;
            co.Link = ComponentLink.Data;
            co.Name = p.Name;
            co.Hidden = p.Flags.HasFlag(NodePropertyFlags.Hidden);
            co.DataType = EditorInterfaceUtils.VariableTypeToComponentDataType(p.Type);
            co.Target = null; // Link
            co.SupportsMultipleConnections = false;
            co.Value = p.variable;
            co.DebugLastValue = null;
            _components.Add(co);
            return co;
        }

        internal void RefreshComponent(IComponent component)
        {
            var co = (EditorInterface.Component)component;

            if (co._userData is FlowOutput)
            {
                var fo = (FlowOutput)co._userData;
                co.Name = fo.Name;
            }
            else if (co._userData is StackInput)
            {
                var si = (StackInput)co._userData;
                co.Name = si.Name;
                if (co.DataType != EditorInterfaceUtils.VariableTypeToComponentDataType(si.Type))
                {
                    co.DataType = EditorInterfaceUtils.VariableTypeToComponentDataType(si.Type);
                    co.Target = null; // Link
                    co.Value = si.variable;
                    co.DebugLastValue = si.debugLastValue;
                }
            }
            else if (co._userData is StackOutput)
            {
                var so = (StackOutput)co._userData;
                co.Name = so.Name;
                co.DataType = EditorInterfaceUtils.VariableTypeToComponentDataType(so.Type);
            }
            else if (co._userData is NodeProperty)
            {
                var p = (NodeProperty)co._userData;
                co.Name = p.Name;
                if (co.DataType != EditorInterfaceUtils.VariableTypeToComponentDataType(p.Type))
                {
                    co.DataType = EditorInterfaceUtils.VariableTypeToComponentDataType(p.Type);
                    co.Target = null; // Reference?
                    co.Value = p.variable;
                }
            }
        }

        internal void LinkComponent(IComponent component)
        {
            var co = (EditorInterface.Component)component;

            if (co._userData is FlowOutput)
            {
                var fo = (FlowOutput)co._userData;
                if (fo.refNodeGuid != SerializableGuid.empty)
                {
                    var targetGraph = co.Owner.Owner;
                    var targetNode = (Node)targetGraph.GetNode(fo.refNodeGuid);
                    if (targetNode.GetType() == typeof(ErrorNode))
                    {
                        co.Target = null;
                    }
                    else
                    {
                        co.Target = targetNode._components.Where(x => x.IsFlowInput).Single();
                    }
                }
            }
            else if (co._userData is StackInput)
            {
                var si = (StackInput)co._userData;
                if (si.refNodeGuid != SerializableGuid.empty)
                {
                    var targetContainer = co.Owner;
                    var targetGraph = targetContainer.Owner;
                    var targetNode = (Node)targetGraph.GetNode(si.refNodeGuid);
                    if (targetNode.GetType() == typeof(ErrorNode))
                    {
                        co.Target = null;
                    }
                    else
                    {
                        co.Target = targetNode._components.Where(x => x.Link == ComponentLink.Data && x.Direction == ComponentDirection.Output && si.refIndex == x._getUserDataSlotIndex()).Single();
                        if (co.Target.DataType != co.DataType)
                        {
                            Debug.LogError("Data type not compatible. Node link broken.");
                            co.Target = null;
                        }
                    }
                }
            }
        }

        IReadOnlyList<IComponent> GetComponents()
        {
            if (_components == null || ComponentsNeedRefresh) // Graph has not initialized UI components yet OR this is a new node instance OR something was modified
            {
                foreach (var node in Owner.Nodes)
                    ((Node)node.Value).RefreshComponents();
                foreach (var node in Owner.Nodes)
                    ((Node)node.Value).LinkComponents();
            }
            return _components;
        }
        IReadOnlyList<IComponent> INodeContainer.Components => GetComponents();

        IEnumerable<IComponent> INodeContainer.EditableComponents
        {
            get
            {
                var io = this as INodeCustomIO;
                if (io == null)
                    yield break;

                var all = GetComponents();
                foreach (var co_ in all)
                {
                    var component = (EditorInterface.Component)co_;
                    if (component.Link == ComponentLink.Data)
                    {
                        if (component.Direction == ComponentDirection.Input && io.CustomIOType == INodeCustomIO.Type.Inputs)
                            yield return component;
                        else if (component.Direction == ComponentDirection.Output && io.CustomIOType == INodeCustomIO.Type.Outputs)
                            yield return component;
                    }
                }
            }
        }

        Rect IContainerUI.Rect
        {
            get
            {
                var id = Owner.Nodes.Where(pair => pair.Value == this).Select(pair => pair.Key).Single();
                var player = ((LogicGraphPlayer)Owner.Container);
                var nodeui = player._nodeUIDatas.Where(x => x.nodeId == id).Single();
                return nodeui.data.rect;
            }
            set
            {
                var id = Owner.Nodes.Where(pair => pair.Value == this).Select(pair => pair.Key).Single();
                var player = ((LogicGraphPlayer)Owner.Container);
                var nodeui = player._nodeUIDatas.Where(x => x.nodeId == id).Single();
                ref var data = ref nodeui.data;
                data.rect = value;
            }
        }

        Color IContainerUI.Color
        {
            get
            {
                var id = Owner.Nodes.Where(pair => pair.Value == this).Select(pair => pair.Key).Single();
                var player = ((LogicGraphPlayer)Owner.Container);
                var nodeui = player._nodeUIDatas.Where(x => x.nodeId == id).Single();
                return nodeui.data.color;
            }
            set
            {
                var id = Owner.Nodes.Where(pair => pair.Value == this).Select(pair => pair.Key).Single();
                var player = ((LogicGraphPlayer)Owner.Container);
                var nodeui = player._nodeUIDatas.Where(x => x.nodeId == id).Single();
                ref var data = ref nodeui.data;
                data.color = value;
            }
        }

        string INodeContainer.SourceFile
        {
            get
            {
#if UNITY_EDITOR
                var bindingNode = this as BindingNode;
                if (bindingNode != null)
                {
                    return bindingNode.Binding.SourceFile;
                }
                else
#endif
                {
                    return string.Empty;
                }
            }
        }

        int INodeContainer.SourceLine
        {
            get
            {
#if UNITY_EDITOR
                var bindingNode = this as BindingNode;
                if (bindingNode != null)
                {
                    return bindingNode.Binding.SourceLine;
                }
                else
#endif
                {
                    return 0;
                }
            }
        }
    }
}
