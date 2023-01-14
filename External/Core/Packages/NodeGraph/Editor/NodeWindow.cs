using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{

    public class NodePropWindowBase
    {
        Vector2 scrollPos;
        bool ShowAllProps = false; // if true, display all properties in the inspector; if false, display just the selected component
        bool PendingScrollTo = false;
        GUIStyle style_Highlight = null;
        GUIStyle style_ScrollBg = null;
        Node oldActiveNode = null;

        class EditorState
        {
            public UnityEditor.Editor propEditor;
            public bool expandedProps;
            public bool dead;
        }
        Dictionary<Node, EditorState> map = new Dictionary<Node, EditorState>();
        List<Node> dying = new List<Node>();

        public bool DrawProps(List<Node> nodes, Node activeNode, bool forceSelectionChanged, float boundsW)
        {
            foreach (var pair in map)
                pair.Value.dead = true;
            GUI.skin.label.padding = new RectOffset();
            // note that we can't do the scroll-to op if event type is not Repaint. So we may have to queue that one for later
            bool somethingChanged = false;
            if (oldActiveNode != activeNode)
            {
                oldActiveNode = activeNode;
                forceSelectionChanged = true;
            }

            bool oldShow = ShowAllProps;
            ShowAllProps = EditorGUILayout.ToggleLeft("Show all nodes", ShowAllProps);
            if (oldShow != ShowAllProps)
                forceSelectionChanged = true;
            EditorGUILayout.Separator();

            // I think this is a GUIClip bug, where it's asking for the topmost rect's width instead of the visible rect's width when calculating contextWidth.
            // Luckily we can override the auto-sizing
            bool oldHier = EditorGUIUtility.hierarchyMode;
            float oldLabelW = EditorGUIUtility.labelWidth;
            float oldFieldW = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.hierarchyMode = true; // label field resizes to fraction of scroll view width
            float newFieldW = 50.0f;
            float newLabelW = Mathf.Max((boundsW - newFieldW) * 0.45f - 40f, 120f);
            EditorGUIUtility.labelWidth = newLabelW;
            EditorGUIUtility.fieldWidth = newFieldW;

            if (style_ScrollBg == null)
            {
                style_ScrollBg = new GUIStyle(EditorStyles.textArea);
                style_ScrollBg.margin = new RectOffset();
                style_ScrollBg.padding = new RectOffset();
            }
            scrollPos = GUILayout.BeginScrollView(scrollPos, style_ScrollBg, GUILayout.Width(boundsW));
            if (nodes != null)
                foreach (var node in nodes)
                {
                    if (node == null)
                        continue;

                    EditorState state = null;
                    if (!map.TryGetValue(node, out state))
                    {
                        state = new EditorState();
                        map[node] = state;
                    }
                    state.dead = false;
                    if (!ShowAllProps && (node != activeNode))
                    {
                        state.expandedProps = false;
                        continue;
                    }
                    UnityEditor.Editor.CreateCachedEditor(node, null, ref state.propEditor);
                    if (state.propEditor == null)
                        continue;

                    bool highlight = false;
                    if (activeNode == node)
                    {
                        if (forceSelectionChanged)
                        {
                            forceSelectionChanged = false;
                            PendingScrollTo = true;
                            state.expandedProps = true;
                        }
                        if (ShowAllProps)
                            highlight = true;
                    }
                    bool doScroll = PendingScrollTo && (Event.current.type == EventType.Repaint) && (activeNode == node);
                    Rect r1 = new Rect();
                    Rect r2 = new Rect();
                    Color oldColor = GUI.backgroundColor;
                    if (highlight)
                    {
                        if (style_Highlight == null)
                        {
                            style_Highlight = new GUIStyle(EditorStyles.textArea);
                            style_Highlight.margin = new RectOffset();
                            style_Highlight.padding = new RectOffset();
                        }
                        GUI.backgroundColor = new Color(1, 0.5f, 0, 1);
                        GUILayout.BeginVertical(style_Highlight);
                        GUI.backgroundColor = oldColor;
                    }
                    using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
                    {
                        state.expandedProps = EditorGUILayout.InspectorTitlebar(state.expandedProps, node);
                        if (doScroll)
                        {
                            r1 = GUILayoutUtility.GetLastRect();
                            r2 = r1;
                        }
                        if (state.expandedProps)
                        {
                            ++EditorGUI.indentLevel;
                            state.propEditor.DrawDefaultInspector();
                            if (doScroll)
                                r2 = GUILayoutUtility.GetLastRect();
                            --EditorGUI.indentLevel;
                        }
                        if (changeCheckScope.changed)
                            somethingChanged = true;
                    }
                    if (highlight)
                    {
                        GUILayout.EndVertical();
                    }

                    if (doScroll)
                    {
                        PendingScrollTo = false;
                        Rect r3 = Rect.MinMaxRect(Mathf.Min(r1.xMin, r2.xMin), Mathf.Min(r1.yMin, r2.yMin), Mathf.Max(r1.xMax, r2.xMax), Mathf.Max(r1.yMax, r2.yMax) + 10.0f);
                        GUI.ScrollTo(r3);
                        GUI.ScrollTo(r1);
                    }
                }
            EditorGUILayout.Separator();
            GUILayout.EndScrollView();

            EditorGUIUtility.labelWidth = oldLabelW;
            EditorGUIUtility.fieldWidth = oldFieldW;
            EditorGUIUtility.hierarchyMode = oldHier;

            foreach (var pair in map)
            {
                if (pair.Value.dead)
                    dying.Add(pair.Key);
            }
            for (int index = 0, max = dying.Count; index < max; ++index)
                map.Remove(dying[index]);
            dying.Clear();

            return somethingChanged;
        }
    }

    public class NodeWindow : EditorWindow
    {
        public const int gridSpacing = 32;
        public const float width = 1650;
        public const float height = 1100;
        public const float minScale = 0.5f;
        public const float maxScale = 2f;

        public float scale = 1f;
        //[MenuItem("Window/Signals")]
        //static NodeWindow Init()
        //{
        //    return EditorWindow.GetWindow(typeof(NodeWindow)) as NodeWindow;
        //}
        public static Dictionary<Type, Type> customNodeRects = new Dictionary<Type, Type>();

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            customNodeRects = new Dictionary<Type, Type>();
            foreach (var domainAssembly in System.AppDomain.CurrentDomain.GetAssemblies())
                foreach (var assemblyType in domainAssembly.GetTypes())
                {
                    if (assemblyType.IsSubclassOf(typeof(NodeRect)) && !assemblyType.IsAbstract && System.Attribute.IsDefined(assemblyType, typeof(CustomNodeRectDrawer)))
                    {
                        CustomNodeRectDrawer attribute = assemblyType.GetCustomAttributes(typeof(CustomNodeRectDrawer), true)[0] as CustomNodeRectDrawer;
                        customNodeRects.Add(attribute.type, assemblyType);
                    }
                }
        }

        public static NodeWindow Init(NodeGraph graph)
        {
            var window = EditorWindow.GetWindow(typeof(NodeWindow), false, "Node Graph", false) as NodeWindow;
            window.activeGraph = graph;
            window.OnEnable();
            //window.RebuildGraph(graph);
            return window;
        }

        public NodeGraph activeGraph;

        List<Node> graphNodes;

        List<NodeRect> nodes = new List<NodeRect>();
        Dictionary<NodeSocket, NodeSocketRect> sockets = new Dictionary<NodeSocket, NodeSocketRect>();

        NodeGraph pendingGraphRebuildNode = null;
        NodePropWindowBase propWindowHelper = new NodePropWindowBase();
        SplitterPanel divider = null;


        public void ChangeSelectedNode(NodeRect newNode)
        {
            refreshProperties = true;
            if (inRender)
                pendingSelect = newNode;
            else
            {
                pendingSelect = null;
                selectedNode = newNode;
                Repaint();
            }
        }

        NodeRect selectedNode = null;
        NodeRect pendingSelect = null;
        bool inRender = false;
        bool refreshProperties = false;

        public void RebuildGraph(NodeGraph graph)
        {
            pendingGraphRebuildNode = graph;
        }

        public void RebuildGraphInternal(NodeGraph graph)
        {
            var t = graph.transform;
            graphNodes = new List<Node>();

            CollectNodes(graphNodes, graph, t);

            var objects = new UnityEngine.Object[graphNodes.Count];
            for (int i = 0; i < graphNodes.Count; i++)
                objects[i] = graphNodes[i];
            for (int i = 0; i < graphNodes.Count; i++)
                graphNodes[i].RebuildSockets();

            nodes.Clear();
            NodeRect newSelection = null;

            for (int i = 0; i < graphNodes.Count; i++)
            {
                var graphNode = graphNodes[i];
                NodeRect node;

                //Checking if there are any custom node drawers
                if (customNodeRects.ContainsKey(graphNode.GetType()))
                {
                    var ctor = customNodeRects[graphNode.GetType()].GetConstructor(new Type[] { typeof(NodeWindow), typeof(Node) });
                    node = ctor.Invoke(new object[] { this, graphNode }) as NodeRect;
                }
                else
                    node = new NodeRect(this, graphNode);

                node.Initialize();
                nodes.Add(node);

                if ((selectedNode != null) && (selectedNode.node != null) && (selectedNode.node == graphNode))
                    newSelection = node;

                if (graphNode == activeGraph)
                {
                    //var input = new NodeRect(this);
                    ////input.InitializeGraphInput(activeGraph);
                    //nodes.Add(input);

                    //var output = new NodeRect(this);
                    ////output.InitializeGraphOutput(activeGraph);
                    //nodes.Add(output);

                    //if ((selectedNode != null) && (selectedNode.node != null) && (selectedNode.node == activeGraph))
                    //{
                    //    if (selectedNode.Type == NodeRect.NodeRectType.GraphInputs)
                    //        newSelection = input;
                    //    else if (selectedNode.Type == NodeRect.NodeRectType.GraphOutputs)
                    //        newSelection = output;
                    //}
                }
            }
            ChangeSelectedNode(newSelection);

            sockets.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                node.UpdateLayout();
                for (int j = 0; j < node.sockets.Count; j++)
                    sockets[node.sockets[j].socket] = node.sockets[j];
            }
        }

        public void AddNodeRect(NodeRect node)
        {
            node.Initialize();
            nodes.Add(node);
        }

        private static void CollectNodes(List<Node> nodes, NodeGraph graph, Transform t)
        {
            if (!t.gameObject.activeSelf)
                return;
            // stop at node graphs
            var nodeGraph = t.GetComponent<NodeGraph>();
            if (nodeGraph != null && nodeGraph != graph)
            {
                if (nodeGraph.inputs.Count != 0 || nodeGraph.outputs.Count != 0)
                    nodes.Add(nodeGraph);
                return;
            }

            var thisNodes = t.GetComponents<Node>();
            for (int i = 0; i < thisNodes.Length; i++)
            {
                var node = thisNodes[i];
                if (node == graph && graph.inputs.Count == 0 && nodeGraph.outputs.Count == 0) continue; // don't add ourselves if empty
                nodes.Add(node);
            }
            for (int c = 0; c < t.childCount; c++)
                CollectNodes(nodes, graph, t.GetChild(c));

        }

        public static Sprite circle;
        private void OnEnable()
        {
            if (circle == null)
                circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (activeGraph != null)
                RebuildGraph(activeGraph);
            dragging = false;
            Repaint();
            //         if (m_TreeViewState == null)
            //             m_TreeViewState = new TreeViewState();
            // 
            //         m_NodeGraphTreeView = new NodeGraphTreeView(m_TreeViewState);
        }


        Vector2 scrollDelta;
        public const float boxWidth = 150;
        public const float signalHeight = 16;
        public const float headerHeight = 16;
        public const float classHeight = 16;
        public const float footerHeight = 4;

        private bool liveUpdate = true;
        private bool trackSelection = true;
        private bool updateSelection = false;

        private void UpdateFromSelection()
        {
            if (updateSelection)
            {
                updateSelection = false;
                if (Selection.activeGameObject != null)
                {
                    NodeGraph graph = Selection.activeGameObject.GetComponentInParent<NodeGraph>();
                    if (graph != null)
                    {
                        Init(graph);
                    }
                }
            }
        }

        void SidePanel()
        {
            GUILayout.BeginArea(divider.RectPaneA);

            if (GUILayout.Button(activeGraph.name))
                Selection.activeGameObject = activeGraph.gameObject;

            liveUpdate = GUILayout.Toggle(liveUpdate, "Live Update");
            bool newTrackSelection = GUILayout.Toggle(trackSelection, "Track Selection");
            if (!trackSelection && newTrackSelection)
            {
                trackSelection = true;
                updateSelection = true;
            }
            trackSelection = newTrackSelection;

            UpdateFromSelection();

            if (GUILayout.Button("Refresh"))
                RebuildGraphInternal(activeGraph);

            if (GUILayout.Button("\u25b2 Up") && activeGraph.transform.parent)
            {
                NodeGraph graph = activeGraph.transform.parent.GetComponentInParent<NodeGraph>();
                if (graph != null)
                {
                    Init(graph);
                }
            }

            /*      if (GUILayout.Button("Fix off-screen nodes"))
                {
                    float newX = 16;
                    float newY = 16;

                    foreach (var node in nodes)
                    {
                        //if(node.nodePos.x <= 0 || node.nodePos.y <= 0)
                        {
                            node.node.pos = new Vector2(newX, newY);
                            newX += 128;
                            if (newX > 900)
                            {
                                newX = 0;
                                newY += 64;
                            }
                        }
                    }
                }*/

            //         EditorGUILayout.BeginHorizontal();
            //         GUILayout.Button("◀ Prev");
            //         GUILayout.Button("▶ Next");
            //         EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Add Comment"))
            {
                activeGraph.gameObject.AddComponent<NodeComment>();
                RebuildGraphInternal(activeGraph);
            }


            if (Event.current.type == EventType.Repaint)
                componentMenuRect = GUILayoutUtility.GetLastRect();
            bool dropDownSelected = EditorGUILayout.DropdownButton(new GUIContent("Quick Add Node"), FocusType.Keyboard, (GUILayoutOption[])null);


            if (GUILayout.Button("Snap to grid"))
            {
                foreach (var node in nodes)
                {
                    int newX = (int)((node.nodePos.x / gridSpacing) + 0.5f) * gridSpacing;
                    int newY = (int)((node.nodePos.y / gridSpacing) + 0.5f) * gridSpacing;
                    node.rect.position = new Vector2((float)newX, (float)newY);
                }
            }


            if (dropDownSelected)
            {
                if (componentMenu == null)
                    CreateComponentMenu();

                componentMenu.DropDown(componentMenuRect);
            }

            propWindowHelper.DrawProps(graphNodes, ((selectedNode != null) ? selectedNode.node : null), refreshProperties, divider.RectPaneA.width);
            refreshProperties = false;

            GUILayout.EndArea();
        }

        GenericMenu componentMenu;
        Rect componentMenuRect;

        private void CreateComponentMenu()
        {
            var listOfNodes = (from domainAssembly in System.AppDomain.CurrentDomain.GetAssemblies()
                               from assemblyType in domainAssembly.GetTypes()
                               where assemblyType.IsSubclassOf(typeof(Node)) && !assemblyType.IsAbstract && System.Attribute.IsDefined(assemblyType, typeof(AddNodeMenuItem))
                               select assemblyType).ToArray();

            //node, path
            SortedDictionary<string, Type> nodeDictionary = new SortedDictionary<string, Type>();

            foreach (var nodeClass in listOfNodes)
            {
                var att = nodeClass.GetCustomAttributes(typeof(AddNodeMenuItem), true)[0] as AddNodeMenuItem;
                nodeDictionary.Add(QuickAddNodeMenu.GetNameFromAttribute(att) + nodeClass.Name, nodeClass);
            }

            componentMenu = new GenericMenu();
            for (int i = 0; i < (int)QuickAddNodeMenu.Folder.MAX; i++)
            {
                var folderName = QuickAddNodeMenu.FolderName((QuickAddNodeMenu.Folder)i);
                foreach (var pair in nodeDictionary)
                    if (pair.Key.StartsWith(folderName))
                        componentMenu.AddItem(new GUIContent(pair.Key), false, AddComponentMenuCallback, pair.Value);
            }
        }
        private Vector2 offset;

        private void AddComponentMenuCallback(object data)
        {
            var type = (System.Type)data;
            var node = (Node)activeGraph.gameObject.AddComponent(type);
            node.RebuildSockets();
        }

        private bool scrolling;
        private bool isScaling = false;

        /// <summary>
        /// returns true if can procceed further.
        /// </summary>
        bool HandleInput()
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                bool isNode = false;
                foreach (var node in nodes)
                {
                    isNode = node.rect.Contains(GUIUtility.GUIToScreenPoint(e.mousePosition));

                    if (isNode)
                    {

                        break;
                    }
                }
                if (!isNode)
                {
                    // (Event.current.mousePosition)
                    //NodeRect.DeselectNodes();
                }
            }
            if (e.type == EventType.MouseDown && e.button == 2 && !isScaling)
            {
                scrolling = true;
                return false;
            }
            else if ((e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) && e.button == 2 && !isScaling)
            {
                scrolling = false;
                Repaint();
                return false;
            }
            else if (e.type == EventType.MouseDrag && scrolling)
            {
                //scrollDelta = e.delta;
                //offset += scrollDelta;
                Repaint();
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.LeftControl)
            {
                isScaling = true;
                return false;
            }
            else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.LeftControl)
            {
                isScaling = false;
                return false;
            }
            else if (e.type == EventType.ScrollWheel && isScaling)
            {
                scale -= e.delta.y / 100;
                scale = Mathf.Min(maxScale, scale);
                scale = Mathf.Max(minScale, scale);
                Repaint();
            }
            return true;
        }

        NodeGraph FindGraph(Node node)
        {
            Transform parent = node.transform;
            while (parent != null)
            {
                var graph = parent.GetComponent<NodeGraph>();
                if (graph != null)
                    return graph;
                parent = parent.parent;
            }
            return null;
        }

        void OnGUI()
        {
            if (DirtyNodes.nodes.Count > 0)
            {
                while (DirtyNodes.nodes.Count > 0)
                {
                    Node node = DirtyNodes.nodes.Pop();
                    NodeGraph graph = FindGraph(node);
                    if (graph == activeGraph)
                    {
                        pendingGraphRebuildNode = graph;
                        break;
                    }
                }
                DirtyNodes.nodes.Clear();
            }

            if (pendingGraphRebuildNode != null)
            {
                RebuildGraphInternal(pendingGraphRebuildNode);
                pendingGraphRebuildNode = null;
            }

            if (HandleInput() == false) return;


            if (trackSelection && activeGraph == null)
            {
                updateSelection = true;
                UpdateFromSelection();
            }

            if (scrolling)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].nodePos += scrollDelta;
                }
            }


            if (activeGraph == null)
                return;

            if (liveUpdate && graphNodes != null)
            {
                Repaint();
                //var t = activeGraph.transform;
                //var currentGraphNodes = new List<Node>();

                //CollectNodes(currentGraphNodes, activeGraph, t);

                //if (!currentGraphNodes.SequenceEqual(graphNodes))
                //{
                //    RebuildGraphInternal(activeGraph);
                //}
            }

            RenderWindow();
        }

        void RenderWindow()
        {
            if (divider == null)
            {
                divider = new SplitterPanel();

                SplitterPanel.InitialConfig config;
                config.UpDownMode = false;
                config.SwitchPanels = false;
                config.CanSnapAClosed = true;
                config.Collapse = SplitterPanel.CollapseMode.Normal;
                config.PaneASize = 150.0f;
                config.InitialBounds = new Rect(0.0f, 0.0f, position.width, position.height);
                config.MinSizeA = 213.0f;//// something wrong with the minimum size of Vertical, so I'm just setting it this large to protected things
                config.MinSizeB = 64.0f;
                divider.InitState(config);
            }
            divider.Bounds = new Rect(0.0f, 0.0f, position.width, position.height); // refresh bounds (in case window has been resized)

            //this.minSize = new Vector2(divider.MinBoundsX, this.minSize.y);
            if (divider.OnGUI())
            {
                Repaint();
                return;
            }

            if (divider.DefaultCollapeButtons(16.0f, 2.0f, 17.0f))
            {
                Repaint();
                return;
            }

            if (!divider.DrawPaneB)
            {
                scrollDelta = Vector2.zero;
            }
            else
            {
                inRender = true;
                pendingSelect = selectedNode;
                GUILayout.BeginArea(divider.RectPaneB);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                //Event evt = Event.current;
                //if (evt.type == EventType.MouseDown && evt.button == 0)
                //    NodeRect.selectedNode = null;
                //GUI.matrix = Matrix4x4.Scale(new Vector3( scale,  scale, 1.0f));

                // background
                //var defaultColor = GUI.color;
                var gridColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
                //GUI.color = new Color(0.1f, 0.1f, 0.1f);
                //GUI.DrawTexture(new Rect(0, 0, width, height), EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                //GUI.color = defaultColor;

                GUILayout.Label("", GUILayout.Width(width), GUILayout.Height(height));
                int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
                int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

                Handles.BeginGUI();
                Handles.color = gridColor;
                Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

                for (int i = 0; i < widthDivs; i++)
                {
                    Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height * maxScale - scale, 0f) + newOffset);
                }

                for (int j = 0; j < heightDivs; j++)
                {
                    Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width * maxScale - scale, gridSpacing * j, 0f) + newOffset);
                }

                Handles.color = Color.white;
                Handles.EndGUI();
                //GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);

                //for (int x = 0; x <  width; x +=  gridSpacing)
                //{
                //    GUI.DrawTexture(new Rect(x, 0, 1.0f,  height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
                //}

                //for (int y = 0; y <  height; y +=  gridSpacing)
                //{
                //    GUI.DrawTexture(new Rect(0, y,  width, 1), Texture2D.whiteTexture, ScaleMode.StretchToFill);
                //}

                //GUI.color = Color.white;

                //if (scrolling)
                //    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Pan);

                DrawConnections();
                DropAreaGUI();

                var oldBgColor = GUI.backgroundColor;
                BeginWindows();
                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    try
                    {
                        bool highlighted = (node == selectedNode);
                        if (!node.RenderWindow(i, highlighted))
                            RebuildGraph(activeGraph);

                    }
                    catch
                    {
                        RebuildGraph(activeGraph);
                    }
                }
                EndWindows();
                GUI.backgroundColor = oldBgColor;

                if (dragging)
                    DrawCurve(dragStart, dragStop, Color.white);

                // GUI.matrix = mat;
                //GUI.matrix = Matrix4x4.Scale(new Vector3(1, 1, 1.0f));

                EditorGUILayout.EndScrollView();
                GUILayout.EndArea();

                inRender = false;
                if (pendingSelect != selectedNode)
                    ChangeSelectedNode(pendingSelect);
                pendingSelect = null;

            }

            if (divider.DrawPaneA)
                SidePanel();
        }

        void OnSelectionChange()
        {
            if (trackSelection)
            {
                updateSelection = true;
                Repaint();
            }
        }

        private void Update()
        {
            if (liveUpdate && EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawConnections()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (var j = 0; j < nodes[i].sockets.Count; j++)
                {
                    var socketRect = nodes[i].sockets[j];
                    var input = socketRect.socket as NodeInput;
                    if (input != null)
                    {
                        var output = input.GetConnectedOutput();
                        if (output != null)
                        {
                            NodeSocketRect rect2 = null;
                            if (sockets.TryGetValue(output, out rect2))
                            {
                                Color c, shadow;

                                c = new Color(0.7f, 0.7f, 1);
                                shadow = new Color(0, 0, 0, 0.1f);

                                DrawCurve(rect2.connectPoint, socketRect.connectPoint, c, shadow);
                            }
                        }
                    }
                }
            }
        }

        bool dragging;
        Vector2 dragStart;
        Vector2 dragStop;

        public Vector2 scrollPos { get; private set; }

        public void DropAreaGUI()
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragExited:
                    dragging = false;
                    break;
                case EventType.MouseDown:
                    if (evt.button != 0) return;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var dragged = nodes[i].HitTest(evt.mousePosition);
                        if (dragged != null)
                        {
                            // Clear out drag data
                            DragAndDrop.PrepareStartDrag();

                            // Set up what we want to drag
                            DragAndDrop.SetGenericData("socket", dragged.socket);
                            var input = dragged.socket as NodeInput;
                            if (input != null)
                            {
                                Undo.RecordObject(input.node, "Disconnect");
                                input.Connect(null);
                                EditorUtility.SetDirty(input.node);
                            }

                            dragStart = dragStop = dragged.connectPoint;
                            DragAndDrop.paths = null;
                            DragAndDrop.objectReferences = new UnityEngine.Object[0];
                            DragAndDrop.StartDrag("Dragging connector");

                            // Make sure no one uses the event after us
                            Event.current.Use();
                            GUIUtility.hotControl = 0;
                            dragging = true;
                            break;
                        }
                    }



                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        // only allow dropping input or output
                        //if (DragAndDrop.objectReferences.Length != 1) return;
                        var obj = DragAndDrop.GetGenericData("socket");
                        var draggedInput = obj as NodeInput;
                        var draggedOuput = obj as NodeOutput;

                        if (draggedInput == null && draggedOuput == null) return;

                        NodeSocketRect dropped = null;
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            dropped = nodes[i].HitTest(evt.mousePosition);
                            if (dropped != null)
                                break;
                        }

                        bool valid = false;
                        if (dropped != null)
                        {
                            if (draggedInput != null)
                            {
                                var dropOutput = dropped.socket as NodeOutput;
                                if (dropOutput != null)
                                {
                                    valid = draggedInput.CanConnect(dropOutput);
                                    if (valid && evt.type == EventType.DragPerform)
                                    {
                                        DragAndDrop.AcceptDrag();
                                        Undo.RecordObjects(new UnityEngine.Object[] { draggedInput.node, dropOutput.node }, "Connect");
                                        draggedInput.Connect(dropOutput);
                                        EditorUtility.SetDirty(dropOutput.node);
                                        EditorUtility.SetDirty(draggedInput.node);
                                    }
                                }
                            }
                            else if (draggedOuput != null)
                            {
                                var dropInput = dropped.socket as NodeInput;
                                if (dropInput != null)
                                {
                                    valid = dropInput.CanConnect(draggedOuput);
                                    if (valid && evt.type == EventType.DragPerform)
                                    {
                                        DragAndDrop.AcceptDrag();
                                        Undo.RecordObjects(new UnityEngine.Object[] { dropInput.node, draggedOuput.node }, "Connect");
                                        dropInput.Connect(draggedOuput);
                                        EditorUtility.SetDirty(dropInput.node);
                                        EditorUtility.SetDirty(draggedOuput.node);
                                    }
                                }
                            }
                        }
                        DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;

                        dragging = (evt.type == EventType.DragUpdated);
                        if (draggedInput != null)
                            dragStart = evt.mousePosition;
                        else
                            dragStop = evt.mousePosition;
                        Repaint();
                    }
                    break;
            }
        }

        public static void DrawTextureGUI(Rect rect, Sprite sprite)
        {
            Rect spriteRect = new Rect(sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height,
                                       sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height);
            Vector2 actualSize = rect.size;

            actualSize.y *= (sprite.rect.height / sprite.rect.width);
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y + (rect.height - actualSize.y) / 2, actualSize.x, actualSize.y), sprite.texture, spriteRect);
        }

        void DrawNodeCurve(Rect start, Rect end, Color color)
        {
            Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
            Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);

            DrawCurve(startPos, endPos, color);
        }

        private static void DrawCurve(Vector3 startPos, Vector3 endPos, Color color)
        {
            DrawCurve(startPos, endPos, color, new Color(0, 0, 0, 0.1f));
        }
        private static void DrawCurve(Vector3 startPos, Vector3 endPos, Color color, Color shadowColor)
        {
            Vector3 startTan = startPos + Vector3.right * 50;
            Vector3 endTan = endPos + Vector3.left * 50;
            for (int i = 0; i < 3; i++) // Draw a shadow
                Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowColor, null, (i + 1) * 5);
            Handles.DrawBezier(startPos, endPos, startTan, endTan, color, null, 2);
        }
    }
}
