using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Logic Graph Editor Window - root element, updates and manages other tool components
    /// </summary>
    internal class LogicGraphWindow : EditorWindow
    {
        private VisualElement root;
        private LogicGraphView graphView;
        internal LogicGraphView GraphView => graphView;
        private BlackboardView blackboardView;

        internal BlackboardView BlackboardView => blackboardView;
        private InspectorView inspectorView;

        private VariableDragView dragView;
        internal VariableDragView DragView => dragView;

        private SearchersInspector searchersInspector;
        private GraphToolbarView toolbar;
        private ToastNotificationsManager toastNotificationsManager;
        private SettingsView settingsView;

        private const string k_NodeGraphUXMLGUID = "669f9b25581b6bf4280c3d688cd37155";
        private const string WindowName = "Logic Graph";

        public bool initialized = false;

        private LogicGraphPlayerEditor activeGraph;
        internal LogicGraphPlayerEditor ActiveGraph
        {
            get
            {
                return activeGraph;
            }
            set
            {
                if (value == null)
                    return;

                if (activeGraph != null && value.logicGraphPlayer == ActiveGraph.logicGraphPlayer)
                    return;

                activeGraph = value;

                if (initialized)
                {
                    SetNewActiveGraph();
                }
            }
        }

        internal bool GraphValid => ActiveGraph != null && ActiveGraph.logicGraphPlayer != null;

        private LogicGraphEditorShortcuts logicGraphEditorShortcuts = new LogicGraphEditorShortcuts();

        [MenuItem("No Brakes Games/Logic Graph...")]
        public static LogicGraphWindow OpenNewNodeGraphWindow()
        {
            LogicGraphPlayer player = null;
            var selected = Selection.activeGameObject;

            if (selected != null)
                player = selected.GetComponent<LogicGraphPlayer>();

            if (player != null)
            {
                return Init(new LogicGraphPlayerEditor(player, selected));
            }
            else
            {
                var window = GetWindow(typeof(LogicGraphWindow)) as LogicGraphWindow;
                if (window != null && window.GraphValid)
                {
                    Init(window.ActiveGraph);
                }

                return window;
            }
        }

        [DidReloadScripts(1)]
        private static void OnScriptsReloaded()
        {
            if (HasOpenInstances<LogicGraphWindow>())
            {
                var window = GetWindow<LogicGraphWindow>(false, WindowName, EditorStateManager.WindowFocusState);
                window.initialized = !window.NeedsToReinitialize();
                window.OnSelectionChange();
            }
        }

        public void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                var logicGraphPlayer = Selection.activeGameObject.GetComponentInParent<LogicGraphPlayer>(true);
                if (logicGraphPlayer != null)
                {
                    ActiveGraph = new LogicGraphPlayerEditor(logicGraphPlayer, Selection.activeGameObject);
                }
            }
            else // check if logic graph was deleted
            {
                if (GraphValid)
                    ClearAll();
            }
        }

        internal static LogicGraphWindow Init(LogicGraphPlayerEditor newGraph)
        {
            var window = GetWindow<LogicGraphWindow>(false, "Logic Graph: " + newGraph.graphObj.name, true);
            window.ActiveGraph = newGraph;
            window.minSize = new Vector2(700, 400);

            return window;
        }

        private void ModeChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredEditMode)
            {
                OnSelectionChange();
            }
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_NodeGraphUXMLGUID));
            visualTree.CloneTree(root);

            blackboardView = root.Q<BlackboardView>();
            inspectorView = root.Q<InspectorView>();
            graphView = root.Q<LogicGraphView>();
            toolbar = root.Q<GraphToolbarView>();
            searchersInspector = root.Q<SearchersInspector>();
            settingsView = root.Q<SettingsView>();

            EditorApplication.playModeStateChanged -= ModeChanged;
            EditorApplication.playModeStateChanged += ModeChanged;
            EditorApplication.hierarchyChanged += HierarchyChanged;

            SetupDragView();
            toastNotificationsManager = new ToastNotificationsManager(root);

            toolbar.Initialize(settingsView, blackboardView, inspectorView);
            settingsView.Initialize(searchersInspector, graphView);
            searchersInspector.AddOnClick(graphView.CreateNodeAtMousePos);

            if (GraphValid)
                SetNewActiveGraph();

            SceneManager.sceneLoaded += OnSceneLoadedPlaymode;
            EditorSceneManager.activeSceneChangedInEditMode += SceneChangedEditor;
            EditorApplication.playModeStateChanged += OnPlaymodeChanged;

            initialized = true;

            AssemblyReloadEvents.beforeAssemblyReload += () => { EditorStateManager.WindowFocusState = hasFocus; };
            Undo.undoRedoPerformed += OnUndoPerformed;

            root.RegisterCallback<KeyDownEvent>(logicGraphEditorShortcuts.OnKeyDown, TrickleDown.TrickleDown);
        }

        private void OnPlaymodeChanged(PlayModeStateChange playmodeState)
        {
            if (playmodeState == PlayModeStateChange.EnteredPlayMode)
                OnScriptsReloaded();
        }

        void OnUndoPerformed()
        {
            if (GraphValid)
                ActiveGraph.StateChanged();
        }

        private bool NeedsToReinitialize()
        {
            return graphView == null || blackboardView == null || toolbar == null || root == null;
        }

        private void SceneChangedEditor(Scene arg0, Scene arg1)
        {
            ClearAll();
        }

        private void OnSceneLoadedPlaymode(Scene arg0, LoadSceneMode arg1)
        {
            ClearAll();
        }

        private void Update()
        {
            if (initialized && GraphValid)
            {
                if (ActiveGraph.stateChanged)
                    UpdateAll(true);
                else if (Application.isPlaying) // non full update is used for animations and to display ports value changes
                    UpdateAll(false);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedPlaymode;
            EditorSceneManager.activeSceneChangedInEditMode -= SceneChangedEditor;
            EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
            EditorApplication.hierarchyChanged -= HierarchyChanged;
        }

        private void SetNewActiveGraph()
        {
            titleContent.text = "Logic Graph: " + ActiveGraph.graphObj.name;

            logicGraphEditorShortcuts.Initialize(ActiveGraph, graphView);
            blackboardView.Initialize(ActiveGraph);
            graphView.Initialize(this, ActiveGraph, inspectorView);
            inspectorView.Initialize(ActiveGraph);
            searchersInspector.Initialize(ActiveGraph);
            settingsView.SetNewActiveGraph(ActiveGraph);

            ActiveGraph.onToastNotification += toastNotificationsManager.AddToastNotification;

            UpdateAll(true);

            StartViewportUpdate();

            Focus();
        }

        #region ViewportUpdate

        private float updateStartTime;
        private const float waitUntilForceUpdateViewport = 0.3f;

        private void StartViewportUpdate()
        {
            //Incredibly hacky but very reliable, hopefully someday this can be changed to something not as hacky
            //GemometryChangedEvent doesnt trigger a second time after initialization (if you open another graph without closing window)
            //Just calling graphView.TryUpdateViewportPosition doesnt work because the layout most likely wont be finalized and will return Nan
            if (!graphView.TryUpdateViewportPosition(true))
            {
                EditorApplication.update -= OnEditorUpdate;
                EditorApplication.update += OnEditorUpdate;
                updateStartTime = Time.realtimeSinceStartup;
            }
        }

        private void OnEditorUpdate()
        {
            if (Time.realtimeSinceStartup - updateStartTime >= waitUntilForceUpdateViewport)
            {
                graphView.TryUpdateViewportPosition(false);
                EditorApplication.update -= OnEditorUpdate;
            }
        }

        #endregion

        internal void UpdateAll(bool fullUpdate)
        {
            blackboardView.Update(fullUpdate);
            graphView.Update(fullUpdate);
            inspectorView.Update(fullUpdate);
            toastNotificationsManager.Update();
            searchersInspector.Update();

            ActiveGraph.Reset();
        }

        private void HierarchyChanged()
        {
            if (GraphValid)
            {
                searchersInspector.Update();
                settingsView.Update();
            }
            else
                ClearAll();
        }

        private void ClearAll()
        {
            if (graphView != null)
                graphView.ClearAll();

            if (searchersInspector != null)
                searchersInspector.ClearAll();

            if (settingsView != null)
                settingsView.ClearAll();

            //just to be sure
            activeGraph = null;
        }

        private void SetupDragView()
        {
            dragView = new VariableDragView();
            dragView.visible = false;
            root.parent.Add(dragView);
            blackboardView.AddDragView(dragView);
            graphView.AddDragView(dragView);
            searchersInspector.AddDragView(dragView);
        }
    }
}