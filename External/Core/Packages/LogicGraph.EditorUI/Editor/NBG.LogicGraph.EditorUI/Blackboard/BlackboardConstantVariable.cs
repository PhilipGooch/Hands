using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class BlackboardConstantVariable : BlackboardVariable
    {
        private const string k_UXMLGUID = "0440ac97267342a419718cdc99ebe519";

        private VisualElement valueFieldParent;

        private TextField variableNameField;
        private EnumField constantTypeEnum;

        private Button deleteButton;

        public new class UxmlFactory : UxmlFactory<BlackboardConstantVariable, VisualElement.UxmlTraits> { }

        public BlackboardConstantVariable()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            ConstantTypeFieldSetup();
            ConstantNameFieldSetup();

            deleteButton = this.Q<Button>("deleteThisButton");
            deleteButton.clickable.clicked += DeleteVariable;

            valueFieldParent = this.Q<VisualElement>("valueFieldParent");
            variableNameField.QInputField<string>().RegisterCallback<FocusOutEvent>(OnNameFieldFocusOut);

            //needed because of GraphView bug which blocks light theme uss
            if (!EditorGUIUtility.isProSkin)
            {
                variableNameField.Q<Label>().FixLightSkinLabel();
            }

            FieldsCreator.FixLabelStyling(variableNameField, LabelStyle.defaultBlackboardStyle);
            FieldsCreator.FixLabelStyling(constantTypeEnum, LabelStyle.defaultBlackboardStyle);
        }

        private void OnNameFieldFocusOut(FocusOutEvent evt)
        {
            activeGraph.StateChanged();
        }

        internal void Update(LogicGraphPlayerEditor activeGraph, IVariableContainer variable)
        {
            this.activeGraph = activeGraph;
            this.variable = variable;

            variableNameField.SetValueWithoutNotify(variable.Name);
            constantTypeEnum.SetValueWithoutNotify(variable.Type.VariableTypeToVariableTypeUI());
            CreateNewValueField();
        }

        private void ConstantTypeFieldSetup()
        {
            constantTypeEnum = this.Q<EnumField>("constantTypeField");
            constantTypeEnum.Init(VariableTypeUI.Int);
            constantTypeEnum.value = VariableTypeUI.Int;
            constantTypeEnum.RegisterCallback<ChangeEvent<System.Enum>>((evt) =>
            {
                activeGraph.NodesController.ChangeVariableType(variable.ID, ((VariableTypeUI)evt.newValue).VariableTypeUIToVariableType());
                activeGraph.StateChanged();
            });
        }

        private void ConstantNameFieldSetup()
        {
            variableNameField = this.Q<TextField>("variableName");
            variableNameField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                activeGraph.NodesController.ChangeVariableName(variable.ID, evt.newValue);
                //no need to update graph, since its just a string change
                //activeGraph.StateChanged();
            });
        }

        private void CreateNewValueField()
        {
            valueFieldParent.Clear();

            FieldsCreator.CreateVariableValueFieldFromVariableContainer(valueFieldParent, variable, activeGraph.NodesController);
        }
    }
}