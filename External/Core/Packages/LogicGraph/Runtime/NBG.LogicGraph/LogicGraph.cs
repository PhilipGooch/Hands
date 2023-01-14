using NBG.Core;
using NBG.LogicGraph.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NBG.LogicGraph
{
    /// <summary>
    /// LogicGraph variable.
    /// </summary>
    public interface ILogicGraphVariable
    {
        string Name { get; }
        VariableType Type { get; }
    }

    class LogicGraphVariable : ILogicGraphVariable
    {
        public string Name { get; set; }
        public VariableType Type { get; set; }

        public IVarHandleContainer variable;
    }

    public static class LogicGraphBindings
    {
        public static void OnEvent(object sender, long eventId)
        {
            // 1. OnEvent is re-entrant
            // 2. Listener list might change during iteration
            // TODO: OPTIMIZE EVENTS!
            var tmp = new List<EventNode.Listener>(EventNode._listeners.Where(x => x.sender == sender && x.senderEventId == eventId));
            foreach (var handler in tmp)
            {
                var scope = UnityEngine.Time.inFixedTimeStep ? NodeAPIScope.Sim : NodeAPIScope.View;
                LogicGraph.Traverse(handler.target, scope);
            }
        }
    }

    internal class LogicGraph : ILogicGraph
    {
        public const int SerializationVersion = 0;

        readonly ILogicGraphContainer _container;
        public ILogicGraphContainer Container => _container;

        readonly Dictionary<SerializableGuid, INode> _nodes = new Dictionary<SerializableGuid, INode>();
        public IReadOnlyDictionary<SerializableGuid, INode> Nodes => _nodes;

        readonly Dictionary<SerializableGuid, ILogicGraphVariable> _variables = new Dictionary<SerializableGuid, ILogicGraphVariable>();
        public IReadOnlyDictionary<SerializableGuid, ILogicGraphVariable> Variables => _variables;

        public INode TryGetNode(SerializableGuid guid)
        {
            if (_nodes.TryGetValue(guid, out INode value))
                return value;
            else
                return null;
        }

        public INode GetNode(SerializableGuid guid)
        {
            var node = TryGetNode(guid);
            if (node == null)
                throw new Exception($"Could not find node '{guid}'.");
            return node;
        }

        public LogicGraph(ILogicGraphContainer container)
        {
            _container = container;
        }

        public static void Traverse(INode start, NodeAPIScope scope)
        {
            var ctx = ExecutionContextBindings.GetForCurrentThread();
            ctx.Scope = scope;

            var node = (Node)start;
            TraverseInternal(node, ctx);
        }

        public static void TraverseWithContext(INode start, ExecutionContext ctx)
        {
            var node = (Node)start;
            TraverseInternal(node, ctx);
        }

        static void TraverseInternal(Node start, ExecutionContext ctx)
        {
            try
            {
                var flows = start?.Execute(ctx);
                var frame = ctx.Peek();
                for (int i = 0; i < flows; ++i)
                {
                    var index = ctx.Stack.Pop<int>();
                    var fo = start.FlowOutputs[index];
                    if (fo.refNodeGuid != SerializableGuid.empty)
                    {
                        var nextNode = (Node)frame.Graph.GetNode(fo.refNodeGuid);
                        TraverseInternal(nextNode, ctx);
                    }
                }

                // If this is not a FlowControlNode, it should follow the only default FlowOutput
                if (flows == 0
                    && !(start is FlowControlNode)
                    && start.FlowOutputs.Count == 1)
                {
                    var fo = start.FlowOutputs[0];
                    if (fo.refNodeGuid != SerializableGuid.empty)
                    {
                        var nextNode = (Node)frame.Graph.GetNode(fo.refNodeGuid);
                        TraverseInternal(nextNode, ctx);
                    }
                }
                ctx.Pop();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Logic Graph traversal exception.");
                UnityEngine.Debug.LogException(e);
            }
        }

        public void Serialize(ISerializationContext ctx)
        {
            foreach (var pair in _nodes)
            {
                var guid = pair.Key;
                var node = (Node)pair.Value;
                if (SerializationUtils.IsSerializableNode(node.GetType()))
                {
                    var data = node.Serialize(ctx);
                    ctx.OnWriteNodeEntry(guid, data);
                }
                else if (node is ErrorNode)
                {
                    var errorNode = (ErrorNode)node;
                    ctx.OnWriteNodeEntry(guid, errorNode.Backup);
                }
                else
                {
                    UnityEngine.Debug.LogError($"Failed to serialize archetype '{node.GetType()}'");
                }
            }

            foreach (var pair in _variables)
            {
                var guid = pair.Key;
                var variable = (LogicGraphVariable)pair.Value;

                var entry = new SerializableVariableEntry();
                entry.Name = variable.Name;
                entry.Type = variable.Type;
                entry.Value = variable.variable.Get(0).Serialize(ctx);
                ctx.OnWriteVariableEntry(guid, entry);
            }
        }

        public void Deserialize(IDeserializationContext ctx)
        {
            for (int i = 0; i < ctx.VariableCount; ++i)
            {
                ctx.GetVariableEntry(i, out SerializableGuid guid, out SerializableVariableEntry entry);

                var variable = new LogicGraphVariable();
                variable.Name = entry.Name;
                variable.Type = entry.Type;
                variable.variable = VarHandleContainers.Create(entry.Type);
                variable.variable.Get(0).Deserialize(ctx, entry.Value);
                _variables.Add(guid, variable);
            }

            for (int i = 0; i < ctx.NodeCount; ++i)
            {
                ctx.GetNodeEntry(i, out SerializableGuid guid, out SerializableNodeEntry entry);

                if (guid == SerializableGuid.empty) // Prefab instance override handling.
                {
                    UnityEngine.Debug.Assert(string.IsNullOrWhiteSpace(entry.NodeType), "LogicGraph node with an empty guid should also have empty serialized data.");
                    continue;
                }

                UnityEngine.Object objectContext = null;
                Type nodeArchetype = null;
                Node node = null;

                try
                {
                    objectContext = ctx.GetUnityObject(entry.Target);
                    nodeArchetype = SerializationUtils.GetSerializedNodeType(entry.NodeType);
                    node = CreateNodeInternal(nodeArchetype, objectContext);

                    var deserializationError = node.Deserialize(ctx, entry);
                    if (deserializationError != null)
                        throw new Exception(deserializationError);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e, (UnityEngine.Object)_container);
                    if (node is INodeResettable)
                    {
                        UnityEngine.Debug.LogError($"Node '{entry.NodeType}' will be reset.", (UnityEngine.Object)_container);
                        node = CreateNodeInternal(nodeArchetype, objectContext);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Node '{entry.NodeType}' will be replaced by an internal error node.", (UnityEngine.Object)_container);
                        node = CreateNodeInternal(typeof(Nodes.ErrorNode), null);
                        var errorNode = (ErrorNode)node;
                        errorNode.Backup = entry;
                        errorNode.ErrorMsg = e.Message;
                    }
                }

                _nodes.Add(guid, node);
            }
        }

        /// <summary>
        /// Creates a new node and registers it with a new guid.
        /// </summary>
        public Node CreateNode(Type nodeArchetype, UnityEngine.Object objectContext, UserlandBinding binding)
        {
            var guid = SerializableGuid.Create(Guid.NewGuid());
            if (_nodes.TryGetValue(guid, out INode _))
                throw new Exception($"Adding node with a duplicate id: {guid}");

            if (binding != null && binding.IsStatic && objectContext != null)
                throw new Exception($"Adding a static binding with an object context {binding.Name}.");

            var node = CreateNodeInternal(nodeArchetype, objectContext);

            if (node is INodeOnAddToGraph)
            {
                var addToGraph = (INodeOnAddToGraph)node;
                addToGraph.OnAddToGraph();
            }

            if (node is INodeBinding)
            {
                if (binding == null)
                    throw new Exception($"Null binding specified for a binding node {nodeArchetype.Name}");

                var bindingNode = (INodeBinding)node;
                bindingNode.OnDeserializedBinding(binding);
            }
            else
            {
                if (binding != null)
                    throw new InvalidOperationException();
            }

            _nodes.Add(guid, node);
            ((ILogicGraphContainerCallbacks)_container)?.OnNodeAdded(guid, node);
            return node;
        }

        /// <summary>
        /// Creates a new node and registers it with a new guid.
        /// </summary>
        public T CreateNode<T>() where T : Node
        {
            return (T)CreateNode(typeof(T), null, null);
        }

        /// <summary>
        /// Creates a new node and registers it with a new guid.
        /// </summary>
        public T CreateNode<T>(UnityEngine.Object objectContext) where T : Node
        {
            return (T)CreateNode(typeof(T), objectContext, null);
        }

        /// <summary>
        /// Creates a new node and registers it with a new guid.
        /// </summary>
        public T CreateNode<T>(UnityEngine.Object objectContext, UserlandBinding binding) where T : Node
        {
            return (T)CreateNode(typeof(T), objectContext, binding);
        }

        Node CreateNodeInternal(Type nodeArchetype, UnityEngine.Object objectContext)
        {
            var node = (Node)Activator.CreateInstance(nodeArchetype);
            node.Owner = this;

            if (node is INodeOnInitialize)
            {
                var deserializedNode = node as INodeOnInitialize;
                deserializedNode.OnInitialize();
            }

            if (node is INodeObjectContext)
            {
                var ctx = (INodeObjectContext)node;
                ctx.ObjectContext = objectContext;
            }

            return node;
        }

        /// <summary>
        /// Removes a node.
        /// </summary>
        public void RemoveNode(SerializableGuid id)
        {
            if (_nodes.TryGetValue(id, out INode node))
            {
                if (node is INodeOnDisable)
                {
                    var disableNode = (INodeOnDisable)node;
                    disableNode.OnDisable();
                }

                RemoveNodeInternal(id);
                ((ILogicGraphContainerCallbacks)_container)?.OnNodeRemoved(id, node);
            }
            else
            {
                throw new Exception($"Could not find node to remove. Id: {id}");
            }
        }

        void RemoveNodeInternal(SerializableGuid id)
        {
            foreach (var pair in _nodes)
            {
                var groupNode = pair.Value as GroupNode;
                if (groupNode != null)
                {
                    if (groupNode.Children.Contains(id))
                    {
                        groupNode.RemoveChild(id);
                        break;
                    }
                }
            }

            _nodes.Remove(id);

            // Remove connections to the deleted node
            foreach (var _node in _nodes.Values)
            {
                var node = (Node)_node;
                node.RemoveLinksToNode(id);
            }
        }

        /// <summary>
        /// Allocates a new variable.
        /// </summary>
        public SerializableGuid AddVariable(string name, VariableType type)
        {
            var guid = SerializableGuid.Create(Guid.NewGuid());
            if (_variables.TryGetValue(guid, out ILogicGraphVariable _))
                throw new Exception($"Adding variable with a duplicate id: {guid}");

            var variable = new LogicGraphVariable();
            variable.Name = name;
            variable.Type = type;
            variable.variable = VarHandleContainers.Create(type);
            _variables.Add(guid, variable);
            return guid;
        }

        /// <summary>
        /// Removes a variable.
        /// </summary>
        public void RemoveVariable(SerializableGuid id)
        {
            if (_variables.TryGetValue(id, out ILogicGraphVariable node))
            {
                _variables.Remove(id);
            }
            else
            {
                throw new Exception($"Could not find variable to remove. Id: {id}");
            }
        }

        public void OnEnable()
        {
            foreach (var pair in _nodes)
            {
                var node = pair.Value as INodeOnEnable;
                node?.OnEnable();
            }
        }

        public void OnDisable()
        {
            foreach (var pair in _nodes)
            {
                var node = pair.Value as INodeOnDisable;
                node?.OnDisable();
            }
        }

        public void OnStart()
        {
            foreach (var pair in _nodes)
            {
                var node = pair.Value as INodeOnStart;
                node?.OnStart();
            }
        }

        public void OnFixedUpdate(float dt, bool processFixedUpdateEventNodes)
        {
            foreach (var pair in _nodes)
            {
                var node = pair.Value as INodeOnFixedUpdate;
                node?.OnFixedUpdate(dt);
            }

            if (processFixedUpdateEventNodes)
            {
                foreach (var pair in _nodes)
                {
                    if (pair.Value is NBG.LogicGraph.Nodes.FixedUpdateEventNode)
                        LogicGraph.Traverse(pair.Value, NodeAPIScope.Sim);
                }
            }
        }

        public void OnUpdate(float dt, bool processUpdateEventNodes)
        {
            foreach (var pair in _nodes)
            {
                var node = pair.Value as INodeOnUpdate;
                node?.OnUpdate(dt);
            }

            if (processUpdateEventNodes)
            {
                foreach (var pair in _nodes)
                {
                    if (pair.Value is NBG.LogicGraph.Nodes.UpdateEventNode)
                        LogicGraph.Traverse(pair.Value, NodeAPIScope.View);
                }
            }
        }

        internal SerializableGuid GetNodeId(INode node)
        {
            return _nodes.Where(pair => pair.Value == node).Select(pair => pair.Key).Single();
        }

        public Node GetCustomEventNode(string eventName)
        {
            return (Node)_nodes.Values.FirstOrDefault(x => (x is Nodes.CustomEventNode) && ((Nodes.CustomEventNode)x).EventName == eventName);
        }

        public Node GetCustomOutputNode(string outputName)
        {
            return (Node)_nodes.Values.FirstOrDefault(x => (x is Nodes.CustomOutputNode) && ((Nodes.CustomOutputNode)x).OutputName == outputName);
        }
    }
}
