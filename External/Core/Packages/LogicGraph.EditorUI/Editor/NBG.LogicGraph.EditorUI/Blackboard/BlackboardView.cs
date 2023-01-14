using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class BlackboardView : VisualElement
    {
        private const string k_UXMLGUID = "f361c4f388bab9746ab8d90f777118c2";
        public new class UxmlFactory : UxmlFactory<BlackboardView, VisualElement.UxmlTraits> { }

        private List<BlackboardVariable> variableViews = new List<BlackboardVariable>();
        internal List<BlackboardVariable> VariableViews => variableViews;

        private Button addVariableButton;
        private ScrollView variablesParent;
        private VariableDragView dragView;
        private VisualElement rootContainer;

        private LogicGraphPlayerEditor activeGraph;
        internal bool GraphValid => activeGraph != null && activeGraph.logicGraphPlayer != null;

        public BlackboardView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            rootContainer = this.Q<VisualElement>("rootContainer");

            addVariableButton = this.Q<Button>("addVariableButton");
            addVariableButton.clickable.clicked += AddNewConstantVariable;

            variablesParent = this.Q<ScrollView>("variablesContainer");

            //needed because of GraphView bug which blocks light theme uss
            if (!EditorGUIUtility.isProSkin)
            {
                this.Q<Label>("header").FixLightSkinLabel();
               this.Q<Label>("addVariableLabel").FixLightSkinLabel();
            }

            this.SetVisibility(EditorPrefsManager.BlackboardVisible);
            this.FixTabBackgroundColor();
        }

        internal void AddDragView(VariableDragView dragView)
        {
            this.dragView = dragView;
        }

        internal void AddNewConstantVariable(VariableType type)
        {
            if (GraphValid)
            {
                activeGraph.NodesController.AddVariable("", type);
                activeGraph.StateChanged();
            }
        }

        private void AddNewConstantVariable()
        {
            if (GraphValid)
            {
                activeGraph.NodesController.AddVariable("", VariableType.Int);
                activeGraph.StateChanged();
            }
        }

        private void AddNewConstantVariableView(IVariableContainer variable)
        {
            var constantVariable = new BlackboardConstantVariable();
            constantVariable.SetupDrag(dragView);
            constantVariable.Update(activeGraph, variable);

            variablesParent.Add(constantVariable);
            variableViews.Add(constantVariable);
        }

        internal void Initialize(LogicGraphPlayerEditor activeGraph)
        {
            this.activeGraph = activeGraph;
        }

        internal void Update(bool fullUpdate)
        {
            if (!fullUpdate)
                return;

            RebuildBlackboard();
        }

        //need to do pooling
        private void RebuildBlackboard()
        {
            variablesParent.Clear();
            variableViews.Clear();

            foreach (var variable in activeGraph.NodesController.GetVariables())
            {
                AddNewConstantVariableView(variable);
            }
            
            if(variableViews.Count > 0)
            {
                //taken from BlackboardConstantVariable min width
                style.minWidth = 220; 
            }
            else
            {
                style.minWidth = 130;
            }
        }
    }
}