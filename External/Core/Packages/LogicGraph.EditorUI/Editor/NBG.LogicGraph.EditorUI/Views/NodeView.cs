using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using NBG.Core.Editor;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Manages creation of all logic node views (not comment or group)
    /// </summary>
    internal class NodeView : UnityEditor.Experimental.GraphView.Node, ISerializedNode
    {
        public enum PortType
        {
            Data,
            Flow,
        }

        private VisualElement breakpoint;
        private VisualElement nodeBorder;
        private VisualElement apiScopeIndicator;
        private ErrorBarView errorBar;
        private TargetObjectPingNodeButtonView gameObjectSelectionButton;

        public INodeContainer Container { set; get; }
        public Action<INodeContainer> onSelected { get; set; }
        public Action<INodeContainer> onDeselected { get; set; }

        //would be cool to unify these, maybe using an Interface?
        public Dictionary<ulong, PortView> ports = new Dictionary<ulong, PortView>();
        public Dictionary<ulong, VisualElement> embededFields = new Dictionary<ulong, VisualElement>();

        private LogicGraphPlayerEditor activeGraph;
        private Searcher searcher;

        private Label titleLabel;
        private TextElement flavorText;

        private AddPortsView addPortsView;
        private PortsDividerView portDivider;

        private (bool exists, IComponent component) lastOutputFlowPort;

        private List<IComponent> inputPortsToCreate = new List<IComponent>();
        private List<IComponent> outputPortsToCreate = new List<IComponent>();
        private List<IComponent> embededFieldsToCreate = new List<IComponent>();

        private float animationStartTime;
        private const float animationDuration = 1f;

        public NodeView()
        {
            nodeBorder = this.Q<VisualElement>("node-border");
            breakpoint = new VisualElement();
            breakpoint.AddToClassList("node-breakpoint-view");
            titleContainer.Add(breakpoint);
            portDivider = new PortsDividerView();

            SetupAPIScopeIndicator();
            SetupTitleContainer();

            RegisterCallback<GeometryChangedEvent>(FirstTimeLayoutSetupComepelete);
        }

        public void Initialize(LogicGraphPlayerEditor activeGraph, Action<INodeContainer> onSelected, Action<INodeContainer> onDeselected)
        {
            this.activeGraph = activeGraph;
            this.onSelected = onSelected;
            this.onDeselected = onDeselected;
        }

        internal void Update(INodeContainer node, bool fullUpdate, Searcher searcher)
        {
            //update node base
            {
                this.searcher = searcher;
                this.Container = node;

                AssignText();

                if (fullUpdate)
                {
                    SetPosition(((IContainerUI)node).Rect);
                }
            }

            lastOutputFlowPort = (false, default);

            List<ulong> portsToDelete = ports.Keys.ToList();
            List<ulong> embededFieldsToDelete = embededFields.Keys.ToList();
            CollectPortsAndFields(node, portsToDelete, embededFieldsToDelete);

            UpdatePorts(inputPortsToCreate, inputContainer);
            UpdatePorts(outputPortsToCreate, outputContainer);
            UpdateEmbededFields(embededFieldsToCreate);

            RemovePorts(portsToDelete);
            RemoveEmbededFields(embededFieldsToDelete);

            SetBreakpointState(false);
            UpdateAPIScopeIndicator(node.Scope);

            if (Application.isPlaying)
                UpdateNodeExecutionIndication();

            UpdateErrorBar();

            if (fullUpdate)
            {
                UpdateGameObjectSelectButton();
                UpdateNodeColor();
            }

            if (Container.HasDynamicFlowOutputs)
            {
                UpdateAddRemovePortButtons();
            }

            RefreshExpandedState();
        }

        void SetupAPIScopeIndicator()
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (!LogicGraphPlayer.EnableScopeIcons)
                return;
#pragma warning restore CS0162 // Unreachable code detected

            apiScopeIndicator = new VisualElement();
            apiScopeIndicator.AddToClassList("node-api-indicator-view");
            this.Add(apiScopeIndicator);
        }

        void UpdateAPIScopeIndicator(NodeAPIScope nodeAPIScope)
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (!LogicGraphPlayer.EnableScopeIcons)
                return;
#pragma warning restore CS0162 // Unreachable code detected

            switch (nodeAPIScope)
            {
                case NodeAPIScope.Generic:
                    apiScopeIndicator.SetVisibility(false);
                    break;
                case NodeAPIScope.Sim:
                    apiScopeIndicator.SetVisibility(true);
                    //Alternative:
                    //RotateTool On@2x
                    //Animation Icon
                    //d_NavMeshAgent Icon
                    //d_preAudioAutoPlayOff@2x
                    apiScopeIndicator.style.backgroundImage = VisualElementsEditorExtensions.GetUnityBuiltinIcon("d_preAudioAutoPlayOff");
                    apiScopeIndicator.style.unityBackgroundImageTintColor = Color.red;
                    break;
                case NodeAPIScope.View:
                    apiScopeIndicator.SetVisibility(true);
                    apiScopeIndicator.style.backgroundImage = VisualElementsEditorExtensions.GetUnityBuiltinIcon("d_VisibilityOn");
                    apiScopeIndicator.style.unityBackgroundImageTintColor = Color.blue;
                    break;
                default:
                    break;
            }
        }

        private void SetupTitleContainer()
        {
            titleLabel = titleContainer.Q<Label>("title-label");

            VisualElement titleGroup = new VisualElement();
            titleContainer.Add(titleGroup);
            titleGroup.Add(titleLabel);
            flavorText = new TextElement();
            titleGroup.Add(flavorText);

            SetupGameObjectSelectButton();
            flavorText.AddToClassList("node-flavor-text-view");
        }

        private void FirstTimeLayoutSetupComepelete(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(FirstTimeLayoutSetupComepelete);

            flavorText.style.marginLeft = titleLabel.resolvedStyle.marginLeft;

            titleLabel.style.paddingBottom = 0;
            titleLabel.style.marginBottom = 0;

            flavorText.style.color = Parameters.nodeFlavorTextColor;
            flavorText.style.fontSize = 10;
        }

        internal void DisconnectEdge(Edge toDisconnect)
        {
            foreach (var port in ports)
            {
                port.Value.Disconnect(toDisconnect);
            }
        }

        internal IComponent GetPortComponent(Port portView)
        {
            var key = (portView as PortView).ID;
            if (ports.ContainsKey(key))
                return ports[key].Component;

            Debug.LogError("PORT NOT FOUND");
            return null;
        }

        #region Utility elements

        private void UpdateNodeColor()
        {
            titleContainer.style.backgroundColor = Parameters.NodeColors[Container.NodeType];
            extensionContainer.style.backgroundColor = (Color)(new Color32(61, 61, 61, 205));
        }

        private void UpdateErrorBar()
        {
            if (errorBar == null)
            {
                errorBar = new ErrorBarView();
                errorBar.RegisterButtonAction(OnErrorBarButtonClicked);
                extensionContainer.Add(errorBar);
            }

            var errorMessage = activeGraph.NodesController.CheckNodeForErrors(Container);
            errorBar.tooltip = errorMessage;
            errorBar.SetVisibility(!string.IsNullOrEmpty(errorMessage), false);
        }

        private void OnErrorBarButtonClicked()
        {
            Debug.Log("Error bar button clicked");
        }

        #region Node source ping button
        private void UpdateGameObjectSelectButton()
        {
            var context = Container as INodeObjectContext;

            if (context != null)
            {
                if (context.ObjectContext != null)
                {
                    gameObjectSelectionButton.SetVisibility(true);
                    gameObjectSelectionButton.SetBackgroundPicture(VisualElementsEditorExtensions.GetIconBaseOnObjectType(context.ObjectContext));
                }
                else
                {
                    gameObjectSelectionButton.SetVisibility(false);
                }
            }
            else
            {
                gameObjectSelectionButton.SetVisibility(false);
            }
        }

        private void SetupGameObjectSelectButton()
        {
            gameObjectSelectionButton = new TargetObjectPingNodeButtonView(PingSelection);
            var colapseButton = titleContainer.Q<VisualElement>("collapse-button");
            colapseButton.SetPaddingSize(0);
            colapseButton.style.width = 18;

            titleContainer.Add(gameObjectSelectionButton);
            titleButtonContainer.PlaceInFront(gameObjectSelectionButton);
        }

        #endregion

        private void SetBreakpointState(bool state)
        {
            if (breakpoint.visible != state)
                breakpoint.visible = state;
        }

        //not efficient, but need unity 2021 to use uss transitions
        void UpdateNodeExecutionIndication()
        {
            var currentCount = Time.frameCount;

            if (currentCount == Container.DebugLastActivatedFrameIndex && Container.DebugLastActivatedFrameIndex != 0)
            {
                animationStartTime = Time.realtimeSinceStartup;
            }

            if (Time.realtimeSinceStartup - animationStartTime < animationDuration)
            {
                nodeBorder.SetBorderColor(Color.Lerp(Parameters.activatedNodeColor, Color.black, (Time.realtimeSinceStartup - animationStartTime) / animationDuration));
            }
            else
            {
                nodeBorder.SetBorderColor(Color.black);
            }
        }

        private void UpdateAddRemovePortButtons()
        {
            if (addPortsView == null)
            {
                addPortsView = new AddPortsView();
                extensionContainer.Add(addPortsView);
                addPortsView.RegisterButtonActions(AddPort, RemoveLastFlowPort);
            }

            if (lastOutputFlowPort.exists)
                addPortsView.SetRemovePortsButtonVisibility(true);
            else
                addPortsView.SetRemovePortsButtonVisibility(false);
        }

        #endregion

        private void CollectPortsAndFields(INodeContainer node, List<ulong> portsToDelete, List<ulong> embededFieldsToDelete)
        {
            inputPortsToCreate.Clear();
            outputPortsToCreate.Clear();
            embededFieldsToCreate.Clear();

            foreach (var component in node.Components)
            {
                if (component.Hidden)  // nothing?
                {
                    continue;
                }
                else if (component.Direction == ComponentDirection.Embed) //embeded
                {
                    embededFieldsToDelete.Remove(component.InstanceId);
                    embededFieldsToCreate.Add(component);
                }
                else //port
                {
                    portsToDelete.Remove(component.InstanceId);

                    if (component.Direction == ComponentDirection.Input)
                        inputPortsToCreate.Add(component);
                    else
                        outputPortsToCreate.Add(component);
                }
            }
        }

        private void RemovePorts(List<ulong> portsToDelete)
        {
            foreach (var id in portsToDelete)
            {
                if (ports.ContainsKey(id))
                {
                    if (ports[id].direction == Direction.Input)
                    {
                        inputContainer.RemoveIfContains(ports[id]);
                    }
                    else
                    {
                        outputContainer.RemoveIfContains(ports[id]);
                    }
                }

                ports.Remove(id);
            }
        }

        private void RemoveEmbededFields(List<ulong> embededFieldsToDelete)
        {
            foreach (var id in embededFieldsToDelete)
            {
                if (embededFields.ContainsKey(id))
                {
                    extensionContainer.RemoveIfContains(embededFields[id]);
                }

                embededFields.Remove(id);
            }
        }

        private void UpdatePorts(List<IComponent> portsToCreate, VisualElement dividerContainer)
        {
            for (int i = 0; i < portsToCreate.Count; i++)
            {
                var port = portsToCreate[i];
                var portID = port.InstanceId;

                bool isLastFlowPort = false;
                bool createDivider = false;

                if (port.Link == ComponentLink.Flow)
                {
                    if (i + 1 < portsToCreate.Count)
                    {
                        isLastFlowPort = createDivider = portsToCreate[i + 1].Link == ComponentLink.Data;
                    }
                    else
                    {
                        isLastFlowPort = true;
                        RemovePortDivider(dividerContainer);
                    }
                }

                if (!ports.ContainsKey(portID))
                {
                    CreatePort(port);
                }
                else
                {
                    ports[portID].Update(port);
                }

                if (createDivider)
                {
                    CreatePortDivider(ports[portID], dividerContainer);
                }

                if (port.Direction == ComponentDirection.Output && isLastFlowPort)
                {
                    lastOutputFlowPort = (true, port);
                }
            }
        }

        private void UpdateEmbededFields(List<IComponent> embdedFieldsToCreate)
        {
            foreach (var item in embdedFieldsToCreate)
            {
                if (!embededFields.ContainsKey(item.InstanceId))
                {
                    CreateEmbededField(item);
                }
                else
                {
                    //update embded field?
                }
            }
        }

        private void CreatePortDivider(Port placeAfter, VisualElement container)
        {
            container.Add(portDivider);
            portDivider.PlaceInFront(placeAfter);
        }

        private void RemovePortDivider(VisualElement container)
        {
            var existingDivider = container.Q<PortsDividerView>();
            if (existingDivider != null)
            {
                container.Remove(existingDivider);
            }
        }

        private void CreatePort(IComponent component)
        {
            var port = PortView.CreatePortView(Orientation.Horizontal, component, null/*searcher.ShowSearcher - This would show the searcher window on mouse up*/, activeGraph);
            port.RegisterOnInputFieldValueChanged(OnPortInputFieldValueChanged);

            ports.Add(component.InstanceId, port);

            if (component.Direction == ComponentDirection.Input)
                inputContainer.Add(port);
            else
                outputContainer.Add(port);

            RefreshExpandedState();
        }

        private void CreateEmbededField(IComponent component)
        {
            var element = FieldsCreator.CreateVariableValueFieldFromComponent(this, component, component.Name, new LabelStyle(LabelStyling.auto), activeGraph.NodesController, OnPortInputFieldValueChanged);

            embededFields.Add(component.InstanceId, element);
            extensionContainer.Add(element);

            RefreshExpandedState();
        }

        private void OnPortInputFieldValueChanged()
        {
            AssignText();
        }

        private void AssignText()
        {
            title = Container.DisplayName;
            flavorText.text = Container.DisplayContext;

            if (String.IsNullOrWhiteSpace(Container.DisplayContext))
            {
                titleContainer.style.height = 26;
            }
            else
            {
                //splits flavor text into 2 lines
                if (Container.DisplayContext.Length > 40)
                {
                    var split = Container.DisplayContext.Split(' ');
                    var middleLength = Container.DisplayContext.Length / 2;
                    bool movedLines = false;
                    string finalString = "";

                    foreach (var item in split)
                    {
                        finalString += item;
                        if (finalString.Length >= middleLength && !movedLines)
                        {
                            finalString += "\n";
                            movedLines = true;
                        }
                        else
                            finalString += " ";
                    }

                    flavorText.text = finalString;
                    titleContainer.style.height = 48;
                }
                else
                {
                    titleContainer.style.height = 36;
                }
            }
        }

        private void AddPort()
        {
            activeGraph.NodesController.AddFlowPort(Container, "flow port");
            activeGraph.StateChanged();
        }

        private void RemoveLastFlowPort()
        {
            activeGraph.NodesController.RemoveFlowPort(Container, lastOutputFlowPort.component);
            activeGraph.StateChanged();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            //hidden until this functionality is ready
            /*  evt.menu.AppendAction("Cut", (_) =>
              {
                  activeGraph.OperationsController.Cut();
              });

              evt.menu.AppendAction("Copy", (_) =>
              {
                  activeGraph.OperationsController.Copy();
              });

              evt.menu.AppendAction("Paste", (_) =>
              {
                  activeGraph.OperationsController.Paste();
              });
            */
            if (Container.HasDynamicFlowOutputs)
            {
                evt.menu.AppendAction("Add flow output port", (_) =>
                {
                    AddPort();
                });

                evt.menu.AppendAction("Remove flow output port", (_) =>
                {
                    RemoveLastFlowPort();
                }, lastOutputFlowPort.exists ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();

            PingSelection();

            onSelected?.Invoke(Container);
        }

        void PingSelection()
        {
            var context = Container as INodeObjectContext;
            if (context != null && context.ObjectContext != null)
            {
                EditorGUIUtility.PingObject(context.ObjectContext);
            }
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            onDeselected?.Invoke(Container);
        }
    }
}
