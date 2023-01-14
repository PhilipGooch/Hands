using NBG.Core;
using NBG.Core.Editor;
using NBG.LogicGraph.EditorInterface;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class InspectorView : VisualElement
    {
        private const string k_UXMLGUID = "ea2ec6c02c997a54790e51acf8391a4d";
        public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits> { }

        private Label nodeName;

        private LogicGraphPlayerEditor activeGraph;
        private INodeContainer Selected => selection.Count > 0 ? selection[0] : null;
        private List<INodeContainer> selection = new List<INodeContainer>();

        private VisualElement nodeDetailsParent;
        private EventNodeBuilder eventNodeBuilder;
        private VisualElement scriptInspector;
        private VisualElement scriptInspectorContentContainer;
        private VisualElement gameObjectIcon;
        private VisualElement messageContainer;

        public InspectorView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            nodeName = this.Q<Label>("nodeName");

            nodeDetailsParent = this.Q<VisualElement>("nodeDetailsContainer");
            eventNodeBuilder = nodeDetailsParent.Q<EventNodeBuilder>("eventNodeBuilder");
            scriptInspector = this.Q<VisualElement>("scriptInspector");
            scriptInspectorContentContainer = scriptInspector.Q<VisualElement>("contentContainer");
            gameObjectIcon = this.Q<VisualElement>("gameObjectIcon");
            messageContainer = this.Q("messageContainer");

            //needed because of GraphView bug which blocks light theme uss
            if (!EditorGUIUtility.isProSkin)
            {
                this.Q<Label>("header").FixLightSkinLabel();
                scriptInspectorContentContainer.RegisterCallback<GeometryChangedEvent>(OnScriptInspectorChanged);
            }

            this.SetVisibility(EditorPrefsManager.InspectorVisible);
            this.FixTabBackgroundColor();

            HideAll();
            UpdateLayout();
        }

        internal void Initialize(LogicGraphPlayerEditor activeGraph)
        {
            this.activeGraph = activeGraph;
        }

        public void Update(bool fullUpdate)
        {
            if (eventNodeBuilder != null && activeGraph != null && Selected != null)
                eventNodeBuilder.Update(Selected);
        }

        internal void NodeSelected(INodeContainer container)
        {
            selection.Add(container);

            if (selection.Count > 1)
            {
                HideAll();
                //multi selection is not supported! 
                messageContainer.SetVisibility(true);
                return;
            }

            DisplaySelection();
        }

        internal void NodeDeselected(INodeContainer container)
        {
            selection.Remove(container);
            if (selection.Count == 1)
            {
                DisplaySelection();
            }
            else if (selection.Count == 0)
            {
                HideAll();
                UpdateLayout();
            }
        }

        void DisplaySelection()
        {
            HideAll();

            nodeName.text = Selected.DisplayName;

            UpdateScriptInspector();
            UpdateEventBuilder();

            UpdateLayout();
        }

        private void HideAll()
        {
            scriptInspectorContentContainer.Clear();
            nodeName.text = "";
            scriptInspector.SetVisibility(false);
            nodeDetailsParent.SetVisibility(false);
            messageContainer.SetVisibility(false);
        }

        void UpdateLayout()
        {
            if (scriptInspector.IsVisible() || nodeDetailsParent.IsVisible())
                style.minWidth = 300;
            else
                style.minWidth = 100;
        }

        private void UpdateScriptInspector()
        {
            bool hasInspector = false;
            var context = Selected as INodeObjectContext;
            if (context != null && context.ObjectContext != null && !(context.ObjectContext is LogicGraphPlayer))
            {
                MonoBehaviourInspector(context);
                hasInspector = true;
            }

            if (HasSourceFileData(Selected))
            {
                NonMonoBehaviourInspector();
                hasInspector = true;
            }
            scriptInspector.SetVisibility(hasInspector);
        }

        private void OnScriptInspectorChanged(GeometryChangedEvent evt)
        {
            //needed because of GraphView bug which blocks light theme uss
            if (!UnityEditor.EditorGUIUtility.isProSkin)
            {
                foreach (var item in scriptInspectorContentContainer.Query<Label>().Where(x => !x.ClassListContains("unity-object-field-display__label")).ToList())
                {
                    item.style.color = Color.black;
                }
            }
        }

        private void UpdateEventBuilder()
        {
            if (Selected.HasDynamicIO)
            {
                eventNodeBuilder.Initialize(activeGraph);
                eventNodeBuilder.Update(Selected);
                nodeDetailsParent.Add(eventNodeBuilder);
                nodeDetailsParent.SetVisibility(true);
            }
            else
            {
                nodeDetailsParent.SetVisibility(false);
            }
        }

        void NonMonoBehaviourInspector()
        {
            Button button = new Button();
            button.clickable.clicked += () => OpenSourceFile(Selected);
            button.text = "Open Node Source";
            scriptInspectorContentContainer.Add(button);
            scriptInspector.SetVisibility(true);
        }

        void MonoBehaviourInspector(INodeObjectContext context)
        {
            VisualElementsEditorExtensions.FillDefaultInspector(scriptInspectorContentContainer, context.ObjectContext, false);

            gameObjectIcon.style.backgroundImage = VisualElementsEditorExtensions.GetIconBaseOnObjectType(context.ObjectContext);

            scriptInspector.SetVisibility(true);
        }

        bool HasSourceFileData(INodeContainer container)
        {
            return !string.IsNullOrEmpty(container.SourceFile);
        }

        private void OpenSourceFile(INodeContainer container)
        {
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(container.SourceFile, container.SourceLine, 0);
        }
    }
}
