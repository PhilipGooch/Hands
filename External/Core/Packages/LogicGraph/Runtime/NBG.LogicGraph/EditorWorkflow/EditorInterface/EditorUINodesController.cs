using NBG.Core;
using NBG.LogicGraph.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace NBG.LogicGraph.EditorInterface
{
    class NodeObjectReference : INodeObjectReference
    {
        public string name;
        public object variant;

        public UnityEngine.Object Target { get; set; }

        public List<(string segment, UnityEngine.Object relativeObj)> GetPathListType(NodeEntry entry)
        {
            List<(string segment, UnityEngine.Object relativeObj)> path = new List<(string segment, UnityEngine.Object relativeObj)>();
            if (Target != null)
            {
                var comp = Target as UnityEngine.Component;
                if (comp != null)
                {
                    path.Add((Target.GetType().Name, comp.gameObject));
                    path.Add((entry.description, comp.gameObject));
                    return path;
                }
                else
                {
                    path.Add((Target.name, null));
                    return path;
                }
            }
            else
            {
                path.Add(("null", null));
                return path;
            }
        }

        public List<(string segment, UnityEngine.Object relativeObj)> GetPathHierarchyType(UnityEngine.GameObject relative)
        {
            List<(string segment, UnityEngine.Object relativeObj)> path = new List<(string segment, UnityEngine.Object relativeObj)>();
            if (Target != null)
            {
                var comp = Target as UnityEngine.Component;
                if (comp != null)
                {
                    path = GetRelativePath(comp.gameObject, relative);
                    //add target GameObject to the path, to finish it
                    path.Add((Target.GetType().Name, comp));
                    return path;
                }
                else
                {
                    path.Add((Target.name, null));
                    return path;
                }
            }
            else
            {
                path.Add(("null", null));
                return path;
            }
        }

        static List<(string segment, UnityEngine.Object relativeObj)> GetRelativePath(GameObject startGameobject, GameObject relative)
        {
            var path = new List<(string segment, UnityEngine.Object relativeObj)>();

            if (startGameobject == null)
                return path;

            path.Add((startGameobject.name, startGameobject));

            while (startGameobject.transform.parent != null && startGameobject.transform != relative.transform)
            {
                startGameobject = startGameobject.transform.parent.gameObject;
                path.Add((startGameobject.name, startGameobject));
            }


            //reverse since its back to front
            path.Reverse();

            return path;
        }

        public NodeObjectReference()
        {
            this.Target = null;
            this.name = null;
            this.variant = null;
        }

        public NodeObjectReference(UnityEngine.Object _target, string _name, object extra = null)
        {
            UnityEngine.Debug.Assert(_target != null);
            UnityEngine.Debug.Assert(!string.IsNullOrEmpty(_name));

            this.Target = _target;
            this.name = _name;
            this.variant = extra;
        }
    }

    internal class EditorUINodesController : INodesController
    {
        LogicGraphPlayer logicGraphPlayer;

        public event Action<ToastNotification> onToastNotification;

        public EditorUINodesController(LogicGraphPlayer logicGraphPlayer)
        {
            this.logicGraphPlayer = logicGraphPlayer;
            //attach onToastNotification to backend
        }

        public string CheckNodeForErrors(INodeContainer node)
        {
            var validation = node as INodeValidation;
            if (validation != null)
                return validation.CheckForErrors();
            else
                return null;
        }

        public bool CanConnectPorts(IComponent output, IComponent input)
        {
            Debug.Assert(output.Owner.Owner == input.Owner.Owner);
            if (output.Owner.Owner != input.Owner.Owner)
                return false;
            if (output.Direction == input.Direction)
                return false; // Can't link same directions
            if (output.Direction == ComponentDirection.Embed || input.Direction == ComponentDirection.Embed)
                return false; // Can't link embeds
            if (output.DataType != input.DataType)
                return false; // Can't link different data types
            return true;
        }

        public bool CanConnectPortsSemantic(IComponent output, IComponent input, out string errorMsg)
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (LogicGraphPlayer.EnableScopes)
            {
                // Determine link owner
                Component owner;
                if (((Component)output).IsLinkOwner == true)
                {
                    Debug.Assert(((Component)input).IsLinkOwner == false);
                    owner = (Component)output;
                }
                else
                {
                    Debug.Assert(((Component)output).IsLinkOwner == false);
                    Debug.Assert(((Component)input).IsLinkOwner == true);
                    owner = (Component)input;
                }

                return ValidateWillNodeBeInValidScopeIfConnected(output, input, owner, out errorMsg);
            }
            else
            {
                errorMsg = null;
                return true;
            }
#pragma warning restore CS0162 // Unreachable code detected
        }

        public void ConnectNodes(IComponent output, IComponent input)
        {
            if (!CanConnectPorts(output, input))
                return;

            RecordUndo("Connect LogicGraph ports");

            Debug.Assert(output.Owner.Owner == input.Owner.Owner);

            // Determine link owner and target
            Component owner;
            Component target;
            if (((Component)output).IsLinkOwner == true)
            {
                Debug.Assert(((Component)input).IsLinkOwner == false);
                owner = (Component)output;
                target = (Component)input;
            }
            else
            {
                Debug.Assert(((Component)output).IsLinkOwner == false);
                Debug.Assert(((Component)input).IsLinkOwner == true);
                owner = (Component)input;
                target = (Component)output;
            }

            if (owner.Direction == ComponentDirection.Output && owner.Link == ComponentLink.Flow)
            {
                var fo = (FlowOutput)owner._userData;
                fo.refNodeGuid = target.Owner.ID;

                owner.Target = target;
            }
            else if (owner.Direction == ComponentDirection.Input && owner.Link == ComponentLink.Data)
            {
                var si = (StackInput)owner._userData;
                si.refNodeGuid = target.Owner.ID;
                si.refIndex = target._userDataRefIndex;

                owner.Target = target;
            }
            else
            {
                throw new NotImplementedException($"CreateLink() not supported for {owner.Link} {owner.Direction}.");
            }

            Dirty();
        }

        public INodeContainer CreateNode(NodeEntry nodeEntry, INodeObjectReference _reference)
        {
            RecordUndo("Create LogicGraph node");

            var target = _reference?.Target;

            UserlandBinding binding = null;
            if (nodeEntry.bindingType != null)
            {
                var bindings = UserlandBindings.GetWithAncestors(nodeEntry.bindingType);
                binding = bindings.Single(x => x.Name == nodeEntry.bindingName);
            }

            var node = ((LogicGraph)logicGraphPlayer.Graph).CreateNode(nodeEntry.archetype, target, binding);

            if (node is INodeVariantHandler)
            {
                var reference = (NodeObjectReference)_reference;
                var handler = (INodeVariantHandler)node;
                var variant = (INodeVariant)reference.variant;
                handler.SetVariant(variant);
            }

            Dirty();
            return node;
        }

        public void DisconnectNodes(IComponent output, IComponent input)
        {
            RecordUndo("Disconnect LogicGraph ports");
            BreakLink(output);
            BreakLink(input);
            Dirty();
        }

        static bool BreakLink(IComponent _component)
        {
            var component = (Component)_component;
            if (!component.IsLinkOwner)
                return false;

            if (component.Direction == ComponentDirection.Output && component.Link == ComponentLink.Flow)
            {
                var fo = (FlowOutput)component._userData;
                fo.refNodeGuid = SerializableGuid.empty;

                component.Target = null;
            }
            else if (component.Direction == ComponentDirection.Input && component.Link == ComponentLink.Data)
            {
                var si = (StackInput)component._userData;
                si.refNodeGuid = SerializableGuid.empty;
                si.refIndex = 0;

                component.Target = null;
            }
            else
            {
                throw new NotImplementedException($"BreakLink() not supported for {component.Link} {component.Direction}.");
            }

            return true;
        }

        static bool BreakLinksTo(IComponent _target)
        {
            var target = (Component)_target;
            if (target.IsLinkOwner)
                return false;

            foreach (var node in target.Owner.Owner.Nodes.Values)
            {
                var container = (INodeContainer)node;
                foreach (var component in container.Components.Cast<Component>())
                {
                    if (component._userData is StackInput)
                    {
                        var si = (StackInput)component._userData;
                        if (si.refNodeGuid == target.Owner.ID && si.refIndex == target._getUserDataSlotIndex())
                        {
                            si.refNodeGuid = SerializableGuid.empty;
                            si.refIndex = -1;
                            component.Target = null;
                        }
                    }
                }
            }

            return true;
        }

        public IEnumerable<NodeEntry> NodeTypes => GetNodeTypes();

        public IEnumerable<NodeEntry> GetNodeTypes()
        {
            // Contextual bindings
            foreach (var nodeType in GetNodeTypesRecursive(logicGraphPlayer.transform, logicGraphPlayer.transform))
                yield return nodeType;

            // Static bindings
            foreach (var pair in UserlandBindings.Bindings)
            {
                var bindings = pair.Value;
                foreach (var binding in bindings)
                {
                    if (binding.IsStatic && !binding.HideInUI)
                    {
                        if (binding.Type == UserlandBindingType.UBT_CustomMethod)
                            continue; //TODO: support custom methods
                        yield return BindingToNodeEntry(binding, null);
                    }
                }
            }

            // Special nodes
            foreach (var type in SpecialNodeTypes.Types)
            {
                if (type.GetCustomAttributes(false).Any(x => x.GetType() == typeof(NodeHideInUIAttribute)))
                    continue;
                if (typeof(INodeObjectContext).IsAssignableFrom(type))
                    continue;
                var ne = new NodeEntry();
                ne.archetype = type;
                ne.bindingType = null;
                ne.bindingName = null;
                ne.description = type.Name;
                var conceptualType = type.GetCustomAttribute<NodeConceptualTypeAttribute>();
                ne.conceptualType = conceptualType != null ? conceptualType.Type : NodeConceptualType.Undefined;
                var categoryPath = type.GetCustomAttribute<NodeCategoryPathAttribute>();
                ne.categoryPath = categoryPath != null ? categoryPath.Path : "Built-in";
                ne.reference = null;
                yield return ne;
            }
        }

        static IEnumerable<NodeEntry> GetNodeTypesRecursive(Transform root, Transform transform)
        {
            var components = transform.GetComponents(typeof(UnityEngine.Component));
            foreach (var comp in components)
            {
                if (comp is LogicGraphPlayer && root == transform)
                    continue; // Skip own LogicGraph
                foreach (var nodeType in GetNodeTypesForComponent(comp))
                    yield return nodeType;
            }

            for (int i = 0; i < transform.childCount; ++i)
            {
                var child = transform.GetChild(i);
                var lgp = child.GetComponent<LogicGraphPlayer>();
                if (lgp != null)
                {
                    foreach (var nodeType in GetNodeTypesForComponent(lgp))
                        yield return nodeType;
                    foreach (var nodeType in GetNodeTypesForLogicGraph(lgp))
                        yield return nodeType;
                    continue; // End recursion if another LogicGraph is found
                }

                foreach (var nodeType in GetNodeTypesRecursive(root, child))
                    yield return nodeType;
            }
        }

        static IEnumerable<NodeEntry> GetNodeTypesForComponent(UnityEngine.Component comp)
        {
            if (comp == null)
                yield break;

            var bindings = UserlandBindings.GetWithAncestors(comp.GetType());
            if (bindings == null)
                yield break;

            foreach (var binding in bindings)
            {
                if (!binding.IsStatic && !binding.HideInUI)
                    yield return BindingToNodeEntry(binding, comp);
            }
        }

        static NodeEntry BindingToNodeEntry(UserlandBinding binding, UnityEngine.Component context)
        {
            var ne = new NodeEntry();
            ne.bindingType = binding.TargetType;
            ne.bindingName = binding.Name;
            ne.conceptualType = binding.ConceptualType;
            switch (binding.Type)
            {
                case UserlandBindingType.UBT_Method:
                    {
                        ne.archetype = typeof(FunctionNode);
                        var methodBinding = (UserlandMethodBinding)binding;
                        switch (methodBinding.MethodType)
                        {
                            case UserlandBindingMethodType.UBMT_Function:
                                ne.description = methodBinding.NodeAPI.Description;
                                break;
                            case UserlandBindingMethodType.UBMT_PropertyGet:
                                ne.description = "(Get) " + methodBinding.NodeAPI.Description;
                                break;
                            case UserlandBindingMethodType.UBMT_PropertySet:
                                ne.description = "(Set) " + methodBinding.NodeAPI.Description;
                                break;
                        }
                    }
                    break;
                case UserlandBindingType.UBT_Event:
                    ne.archetype = typeof(EventNode);
                    ne.description = ((UserlandEventBinding)binding).Description;
                    break;
                default:
                    throw new NotImplementedException();
            }
            ne.categoryPath = binding.CategoryPath;
            ne.reference = (context != null) ? new NodeObjectReference(context, "?-?", null) : null;
            return ne;
        }

        static IEnumerable<NodeEntry> GetNodeTypesForLogicGraph(LogicGraphPlayer lgp)
        {
            var nodes = lgp.Graph.Nodes.Values.Where(x => x is INodeVariant).Cast<INodeVariant>();
            foreach (var node in nodes)
            {
                var ne = new NodeEntry();
                ne.archetype = node.VariantHandler;
                ne.bindingType = null;
                ne.bindingName = null;
                ne.description = $"{node.VariantHandler.Name} {node.VariantName}";
                ne.categoryPath = null;
                ne.reference = new NodeObjectReference(lgp, "?-?", node);
                ne.conceptualType = NodeConceptualType.Undefined;
                yield return ne;
            }
        }

        public IEnumerable<INodeContainer> GetNodes()
        {
            return logicGraphPlayer.Graph.Nodes.Values.Cast<INodeContainer>();
        }

        public void UpdateVariableValue<T>(IVarHandleContainer container, T value)
        {
            RecordUndo("Input Field Value Changed");
            ((IVarHandleContainerTyped<T>)container).SetValue(0, value);
            Dirty();
        }

        public void UpdateRect(INodeContainer container, Rect rect)
        {
            RecordUndo("Modify LogicGraph node");
            ((IContainerUI)container).Rect = rect;
            Dirty();
        }

        public void RemoveNode(INodeContainer toRemove)
        {
            RecordUndo("Delete LogicGraph node");
            ((LogicGraph)logicGraphPlayer.Graph).RemoveNode(toRemove.ID);
            Dirty();
        }

        public void AddFlowPort(INodeContainer owner, string name, INodeContainer target = null)
        {
            RecordUndo("Modify LogicGraph node");
            var io = (INodeCustomFlow)owner;
            io.AddCustomFlow();
            ((Node)owner).ComponentsNeedRefresh = true;
            Dirty();
        }

        public void RemoveFlowPort(INodeContainer owner, IComponent component)
        {
            RecordUndo("Modify LogicGraph node");
            var io = (INodeCustomFlow)owner;
            var co = (Component)component;
            io.RemoveCustomFlow(co._getUserDataSlotIndex());
            ((Node)owner).ComponentsNeedRefresh = true;
            Dirty();
        }

        #region Comments
        public string GetCommentName(INodeContainer container)
        {
            var node = (CommentNode)container;
            return node.Header;
        }

        public void SetCommentName(INodeContainer container, string value)
        {
            RecordUndo("Modify LogicGraph node");
            var node = (CommentNode)container;
            node.Header = value;
            Dirty();
        }

        public string GetCommentContent(INodeContainer container)
        {
            var node = (CommentNode)container;
            return node.Body;
        }

        public void SetCommentContent(INodeContainer container, string value)
        {
            RecordUndo("Modify LogicGraph node");
            var node = (CommentNode)container;
            node.Body = value;
            Dirty();
        }
        #endregion

        #region Groups

        public void AddNodeToGroup(INodeContainer container, INodeContainer toAdd)
        {
            RecordUndo("Modify LogicGraph node group");
            var group = (GroupNode)container;
            group.AddChild(toAdd.ID);
            Dirty();
        }

        public void RemoveNodeFromGroup(INodeContainer container, INodeContainer toRemove)
        {
            RecordUndo("Modify LogicGraph node group");
            var group = (GroupNode)container;
            var idToRemove = toRemove.ID;
            Debug.Assert(idToRemove != SerializableGuid.empty, "RemoveNodeFromGroup got an empty guid");
            group.RemoveChild(idToRemove);
            Dirty();
        }

        public string GetGroupName(INodeContainer container)
        {
            var group = (GroupNode)container;
            return group.Header;
        }

        public void SetGroupName(INodeContainer container, string value)
        {
            RecordUndo("Modify LogicGraph node");
            var group = (GroupNode)container;
            group.Header = value;
            Dirty();
        }

        public IEnumerable<INodeContainer> GetGroupChildren(INodeContainer container)
        {
            Debug.Assert(container.Owner.Container == (ILogicGraphContainer)logicGraphPlayer);

            var group = (GroupNode)container;
            foreach (var guid in group.Children)
            {
                var childNode = container.Owner.GetNode(guid);
                var childContainer = (INodeContainer)childNode;
                yield return childContainer;
            }
        }
        #endregion

        public void UpdateColor(INodeContainer container, Color color)
        {
            RecordUndo("Modify LogicGraph node");
            ((IContainerUI)container).Color = color;
            Dirty();
        }

        void RecordUndo(string description)
        {
#if UNITY_EDITOR
            Undo.RecordObject(logicGraphPlayer, description);
#endif
        }

        void Dirty()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(logicGraphPlayer);

            var prefabStage = PrefabStageUtility.GetPrefabStage(logicGraphPlayer.gameObject);
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
            else if (PrefabUtility.IsPartOfPrefabInstance(logicGraphPlayer))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(logicGraphPlayer);
            }
            else if (PrefabUtility.IsPartOfPrefabAsset(logicGraphPlayer))
            {
                PrefabUtility.SavePrefabAsset(logicGraphPlayer.gameObject);
            }
#endif
        }

        #region INodeCustomIO
        public void AddField(INodeContainer owner, VariableType type, string name)
        {
            RecordUndo("Modify LogicGraph node");
            var io = (INodeCustomIO)owner;
            io.AddCustomIO(name, type);
            ((Node)owner).ComponentsNeedRefresh = true;
            Dirty();
        }

        public void RemoveField(INodeContainer owner, IComponent component)
        {
            RecordUndo("Modify LogicGraph node");
            var io = (INodeCustomIO)owner;
            var co = (Component)component;
            io.RemoveCustomIO(co._getUserDataSlotIndex());
            ((Node)owner).ComponentsNeedRefresh = true;
            Dirty();
        }

        public void ChangeFieldType(INodeContainer owner, IComponent component, VariableType newType)
        {
            RecordUndo("Modify LogicGraph node");
            var io = (INodeCustomIO)owner;
            var co = (Component)component;
            var curName = io.GetCustomIOName(co._getUserDataSlotIndex());
            if (io.GetCustomIOType(co._getUserDataSlotIndex()) != newType)
            {
                io.UpdateCustomIO(co._getUserDataSlotIndex(), curName, newType);
                ((Node)owner).RefreshComponent(co);
                BreakLinksTo(co);
                ((Node)owner).ComponentsNeedRefresh = true;
            }
            Dirty();
        }

        public void ChangeFieldName(INodeContainer owner, IComponent component, string newName)
        {
            RecordUndo("Modify LogicGraph node");
            var io = (INodeCustomIO)owner;
            var co = (Component)component;
            var curType = io.GetCustomIOType(co._getUserDataSlotIndex());
            io.UpdateCustomIO(co._getUserDataSlotIndex(), newName, curType);
            ((Node)owner).RefreshComponent(co);
            Dirty();
        }
        #endregion

        #region LogicGraph variables

        public SerializableGuid AddVariable(string name, VariableType type)
        {
            RecordUndo("Add LogicGraph variable");
            var graph = (LogicGraph)logicGraphPlayer.Graph;
            var id = graph.AddVariable(name, type);

            Dirty();
            return id;
        }

        public void RemoveVariable(SerializableGuid id)
        {
            RecordUndo("Remove LogicGraph variable");
            var graph = (LogicGraph)logicGraphPlayer.Graph;
            graph.RemoveVariable(id);

            // Remove all references of this variable
            var nodes = GetNodes().ToList();
            foreach (var node in nodes)
            {
                if (node is VariableNode)
                {
                    var vn = (VariableNode)node;
                    if (vn.VariableId == id)
                    {
                        BreakLinksTo(node.Components[0]);
                        RemoveNode(node);
                    }
                }
            }

            Dirty();
        }

        public void ChangeVariableType(SerializableGuid variableId, VariableType newType)
        {
            RecordUndo("Modify LogicGraph variable");
            var graph = logicGraphPlayer.Graph;
            var variable = (LogicGraphVariable)graph.Variables[variableId];
            variable.Type = newType;
            variable.variable = VarHandleContainers.Create(newType);

            // Remove all links to VariableNodes of this variable
            var nodes = GetNodes().ToList();
            foreach (var node in nodes)
            {
                if (node is VariableNode)
                {
                    var vn = (VariableNode)node;
                    if (vn.VariableId == variableId)
                    {
                        vn.StackOutputs[0].Type = newType; //TODO: wrap
                        vn.RefreshComponent(node.Components[0]);
                        BreakLinksTo(node.Components[0]);
                        vn.ComponentsNeedRefresh = true;
                    }
                }
            }

            Dirty();
        }

        public void ChangeVariableName(SerializableGuid variableId, string newName)
        {
            RecordUndo("Modify LogicGraph node");
            var graph = logicGraphPlayer.Graph;
            var variable = (LogicGraphVariable)graph.Variables[variableId];
            variable.Name = newName;

            // Refresh all VariableNodes of this variable
            var nodes = GetNodes().ToList();
            foreach (var node in nodes)
            {
                if (node is VariableNode)
                {
                    var vn = (VariableNode)node;
                    if (vn.VariableId == variableId)
                    {
                        vn.RefreshComponent(node.Components[0]);
                    }
                }
            }

            Dirty();
        }

        public INodeContainer CreateVariableNode(SerializableGuid variableId)
        {
            RecordUndo("Create LogicGraph variable");
            var node = ((LogicGraph)logicGraphPlayer.Graph).CreateNode<VariableNode>();
            node.VariableId = variableId;

            Dirty();
            return node;
        }

        public IEnumerable<IVariableContainer> GetVariables()
        {
            foreach (var item in logicGraphPlayer.Graph.Variables)
            {
                yield return (new VariableContainer()
                {
                    ID = item.Key,
                    Variable = ((LogicGraphVariable)item.Value).variable,
                    Name = item.Value.Name,
                    Type = item.Value.Type,
                });
            }
        }

        class VariableContainer : IVariableContainer
        {
            public SerializableGuid ID { get; internal set; }

            public IVarHandleContainer Variable { get; internal set; }

            public string Name { get; internal set; }

            public VariableType Type { get; internal set; }
        }
        #endregion

        public bool ValidateWillNodeBeInValidScopeIfConnected(IComponent output, IComponent input, IComponent ignoreLink, out string errorMsg)
        {
            errorMsg = null;

            var visited = new List<INodeContainer>(64);
            var outCont = FindFirstNonGenericScopeNodeRecursive(output.Owner, visited, ignoreLink);
            if (outCont == null)
                return true;

            visited.Clear();
            var intCont = FindFirstNonGenericScopeNodeRecursive(input.Owner, visited, ignoreLink);
            if (intCont == null)
                return true;

            if (outCont.Scope == intCont.Scope)
                return true;

            errorMsg = $"'{outCont.DisplayName}' is in {outCont.Scope} scope, but '{intCont.DisplayName}' is in {intCont.Scope} scope";
            /*onToastNotification?.Invoke(new ToastNotification(
                Severity.Error,
                "Can't connect sub-graphs of different scopes",
                $"'{outCont.DisplayName}' is in {outCont.Scope} scope, but '{intCont.DisplayName}' is in {intCont.Scope} scope",
                null));*/
            return false;
        }

        static INodeContainer FindFirstNonGenericScopeNodeRecursive(INodeContainer container, IList<INodeContainer> visited, IComponent ignoreLink)
        {
            if (visited.Contains(container))
                return null;
            visited.Add(container);

            var currentNode = (Node)container;
            if (currentNode.Scope != NodeAPIScope.Generic)
                return container;

            foreach (var comp in container.Components)
            {
                if (comp.Direction == ComponentDirection.Embed)
                    continue;
                if (comp == ignoreLink)
                    continue;

                if (comp.Target != null)
                {
                    if (comp.Target != ignoreLink)
                    {
                        var targetContainer = comp.Target.Owner;
                        if (!visited.Contains(targetContainer))
                        {
                            // Recursive search
                            var found = FindFirstNonGenericScopeNodeRecursive(targetContainer, visited, ignoreLink);
                            if (found != null)
                                return found;
                        }
                    }
                }
                else
                {
                    // Check for incoming links
                    var graph = container.Owner;
                    foreach (var node in graph.Nodes.Values)
                    {
                        var otherCont = (INodeContainer)node;
                        if (visited.Contains(otherCont))
                            continue;

                        foreach (var otherComp in otherCont.Components)
                        {
                            if (otherComp.Target == comp && otherComp != ignoreLink)
                            {
                                // Recursive search
                                var found = FindFirstNonGenericScopeNodeRecursive(otherCont, visited, ignoreLink);
                                if (found != null)
                                    return found;
                                break;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
