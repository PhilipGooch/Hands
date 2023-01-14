using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Comment node view, contains no logic, allows to have notes in the graph view
    /// </summary>
    internal class CommentView : GraphElement, ISerializedNode
    {
        private const string k_UXMLGUID = "c612fa6b9de89c044b7084ad5ca2c07d";
        public new class UxmlFactory : UxmlFactory<CommentView, GraphElement.UxmlTraits> { }

        private ILogicGraphEditor activeGraph;
        public INodeContainer Container { set; get; }
        public Action<INodeContainer> onSelected { get; set; }
        public Action<INodeContainer> onDeselected { get; set; }

        private TextField headerField;
        private Label headerLabel;
        private VisualElement headerFieldInput;

        private TextField contentField;
        private Label contentLabel;
        private VisualElement contentFieldInput;

        private bool initialized;

        public CommentView()
        {
            initialized = false;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            usageHints = UsageHints.DynamicTransform;
            capabilities = Capabilities.Selectable | Capabilities.Groupable | Capabilities.Deletable | Capabilities.Movable | Capabilities.Ascendable | Capabilities.Resizable;

            style.width = 300;

            headerField = this.Q<TextField>("header");
            headerLabel = this.Q<Label>("headerLabel");
            headerFieldInput = headerField.QInputField<string>();
            headerFieldInput.AddToClassList("comment-view-text-field");

            contentField = this.Q<TextField>("content");
            contentLabel = this.Q<Label>("contentLabel");
            contentFieldInput = contentField.QInputField<string>();
            contentFieldInput.AddToClassList("comment-view-text-field");

            headerField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                if (activeGraph != null)
                    activeGraph.NodesController.SetCommentName(Container, e.newValue);
            });

            contentField.RegisterCallback<ChangeEvent<string>>(e =>
             {
                 if (activeGraph != null)
                     activeGraph.NodesController.SetCommentContent(Container, e.newValue);
             });

            SetupTextFieldCallbacks(headerFieldInput, headerLabel, OnHeaderBlur, OnHeaderMouseDown);
            SetupTextFieldCallbacks(contentFieldInput, contentLabel, OnContentBlur, OnContentMouseDown);

            style.backgroundColor = Parameters.commentBackgroundColor;

            SetStylingToComment(headerFieldInput, 20);
            SetStylingToComment(headerLabel, 20);

            SetStylingToComment(contentFieldInput, 12);
            SetStylingToComment(contentLabel, 12);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void SetupTextFieldCallbacks(VisualElement fieldInput, VisualElement label, EventCallback<BlurEvent> onHeaderBlur, EventCallback<MouseDownEvent> onHeaderMouseDown)
        {
            fieldInput.RegisterCallback(onHeaderBlur);
            label.RegisterCallback(onHeaderMouseDown);
            label.RegisterCallback<MouseEnterEvent>((_) =>
            {
                label.SetBorderColor(Parameters.commentTextHoverColor);
            });
            label.RegisterCallback<MouseLeaveEvent>((_) =>
            {
                label.SetBorderColor(Parameters.commentBackgroundColor);
            });
        }

        //stop GraphView right click from executing
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 1)
                evt.StopPropagation();
        }

        private void OnHeaderBlur(BlurEvent evt)
        {
            SetHeaderFieldEditMode(false);
            headerLabel.text = headerField.text;
        }

        private void OnContentBlur(BlurEvent evt)
        {
            SetContentFieldEditMode(false);
            contentLabel.text = contentField.text;
        }

        private void SetContentFieldEditMode(bool editMode)
        {
            contentFieldInput.visible = editMode;
            contentField.visible = editMode;
            contentLabel.visible = !editMode;
        }

        private void SetHeaderFieldEditMode(bool editMode)
        {
            headerFieldInput.visible = editMode;
            headerField.visible = editMode;
            headerLabel.visible = !editMode;
        }

        private void OnHeaderMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0 && evt.clickCount == 2)
            {
                headerField.SetValueWithoutNotify(headerLabel.text);

                SetHeaderFieldEditMode(true);
                headerFieldInput.Focus();

                evt.PreventDefault();
                evt.StopPropagation();
            }
        }

        private void OnContentMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0 && evt.clickCount == 2)
            {
                contentField.SetValueWithoutNotify(contentLabel.text);

                SetContentFieldEditMode(true);

                contentFieldInput.Focus();

                evt.PreventDefault();
                evt.StopPropagation();
            }
        }

        public void Initialize(LogicGraphPlayerEditor activeGraph, Action<INodeContainer> onSelected, Action<INodeContainer> onDeselected)
        {
            this.activeGraph = activeGraph;
            this.onSelected = onSelected;
            this.onDeselected = onDeselected;
        }

        public void Update(INodeContainer commentContainer, bool fullUpdate)
        {
            if (!fullUpdate)
                return;

            Container = commentContainer;

            headerField.SetValueWithoutNotify(activeGraph.NodesController.GetCommentName(commentContainer));
            contentField.SetValueWithoutNotify(activeGraph.NodesController.GetCommentContent(commentContainer));

            SetPosition(((IContainerUI)commentContainer).Rect);
            OnContentBlur(null);
            OnHeaderBlur(null);
        }

        //VERY IMPORTANT - overriding GetPosition like so, prevents comment from moving unintentionally when in a group
        public override Rect GetPosition()
        {
            return new Rect(base.resolvedStyle.left, base.resolvedStyle.top, base.resolvedStyle.width, base.resolvedStyle.height);
        }

        private void OnResized()
        {
            activeGraph.NodesController.UpdateRect(Container, GetPosition());
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var nodeHeight = resolvedStyle.height;
            var headerHeight = headerField.resolvedStyle.height;
            contentField.style.maxHeight = nodeHeight - headerHeight - 10;

            //to not dirty the scene when graph is first opened
            if (initialized)
                OnResized();

            initialized = true;
        }

        //some things cant bet set properly using uss
        private void SetStylingToComment(VisualElement element, int fontSize)
        {
            element.style.whiteSpace = WhiteSpace.Normal;
            element.style.backgroundColor = Parameters.commentBackgroundColor;
            element.style.color = Parameters.commentTextColor;
            element.style.fontSize = fontSize;

            element.SetBorderColor(Parameters.commentBackgroundColor);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            onSelected?.Invoke(Container);

            this.SetBorderColor(Parameters.nodeSelectionBorderColor);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            onDeselected?.Invoke(Container);
            this.SetBorderColor(Parameters.commentBackgroundColor);
        }
    }
}
