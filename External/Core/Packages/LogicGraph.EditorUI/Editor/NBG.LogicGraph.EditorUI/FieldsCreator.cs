using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Creates input fields
    /// </summary>
    internal static class FieldsCreator
    {
        internal static VisualElement CreateVariableValueFieldFromComponent(VisualElement parent, IComponent component, string name, LabelStyle labelStyle, INodesController nodesController, Action onChangeAdditional = null)
        {
            var inputField = new VisualElement();

            switch (component.DataType)
            {
                case ComponentDataType.Node:
                    break;
                case ComponentDataType.Bool:
                    inputField = CreateField<bool>(CreateBoolField, name, component.VariantProvider, component.BackingType, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case ComponentDataType.Int:
                    inputField = CreateField<int>(CreateIntegerField, name, component.VariantProvider, component.BackingType, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case ComponentDataType.Float:
                    inputField = CreateField<float>(CreateFloatField, name, component.VariantProvider, component.BackingType, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case ComponentDataType.String:
                    inputField = CreateField<string>(CreateStringField, name, component.VariantProvider, component.BackingType, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case ComponentDataType.UnityVector3:
                    inputField = CreateField<UnityEngine.Vector3>(CreateVector3Field, name, component.VariantProvider, component.BackingType, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case ComponentDataType.UnityObject:
                    inputField = CreateField<UnityEngine.Object>(CreateObjectField, name, component.VariantProvider, component.BackingType, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case ComponentDataType.CoreGuid:
                    break;
                case ComponentDataType.UnityQuaternion:
                    inputField = CreateQuaternionField(name, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case ComponentDataType.UnityColor:
                    inputField = CreateField<UnityEngine.Color>(CreateColorField, name, component.VariantProvider, component.BackingType, component.Value, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                default:
                    throw new NotImplementedException($"{component.DataType}");
            }

            return inputField;
        }

        internal static VisualElement CreateVariableValueFieldFromVariableContainer(VisualElement parent, IVariableContainer variable, INodesController nodesController, Action onChangeAdditional = null)
        {
            var inputField = new VisualElement();
            var labelStyle = LabelStyle.defaultBlackboardStyle;

            switch (variable.Type.VariableTypeToVariableTypeUI())
            {
                case VariableTypeUI.Int:
                    inputField = CreateField<int>(CreateIntegerField, "Integer", null, typeof(int), variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case VariableTypeUI.Bool:
                    inputField = CreateField<bool>(CreateBoolField, "Bool", null, typeof(bool), variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case VariableTypeUI.String:
                    inputField = CreateField<string>(CreateStringField, "String", null, typeof(string), variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case VariableTypeUI.UnityVector3:
                    inputField = CreateField<UnityEngine.Vector3>(CreateVector3Field, "Vector3", null, typeof(UnityEngine.Vector3), variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case VariableTypeUI.UnityObject:
                    inputField = CreateField<UnityEngine.Object>(CreateObjectField, "Object", null, typeof(UnityEngine.Object), variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case VariableTypeUI.Float:
                    inputField = CreateField<float>(CreateFloatField, "Float", null, typeof(float), variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case VariableTypeUI.Quaternion:
                    inputField = CreateQuaternionField("Quaternion", variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                case VariableTypeUI.UnityColor:
                    inputField = CreateField<Color>(CreateColorField, "Color", null, typeof(Color), variable.Variable, parent, nodesController, onChangeAdditional, labelStyle);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return inputField;
        }

        static VisualElement CreateField<T>(
            Func<string, T, Type, VisualElement, Action<T>, LabelStyle, VisualElement> creator,
            string name, IVariantProvider variantProvider, Type baseType, IVarHandleContainer container, VisualElement parent, INodesController nodesController, Action onChangeAdditional, LabelStyle labelStyle)
        {
#if UNITY_2021_1_OR_NEWER

            var variants = (IVariantProviderTyped<T>)(variantProvider);
            if (variants != null)
            {
                return CreateDropDownField(
                   ((IVarHandleContainerTyped<T>)container).GetValue(0),
                   variants,
                   parent,
                   (newValueStr) =>
                   {
                       var index = variants.GetVariantIndexByName(newValueStr);
                       T newValue = variants.GetVariantValue(index);
                       nodesController.UpdateVariableValue(container, newValue);
                       onChangeAdditional?.Invoke();
                   },
                   labelStyle);
            }
            else
#else
#warning LogicGraph node parameter variant feature will not function in Unity earlier than 2021.1
#endif
            {
                return creator(
                  name,
                   ((IVarHandleContainerTyped<T>)container).GetValue(0),
                   baseType,
                   parent,
                   (newValue) =>
                   {
                       nodesController.UpdateVariableValue(container, newValue);
                       onChangeAdditional?.Invoke();
                   },
                   labelStyle);
            }
        }

        static VisualElement CreateQuaternionField(
            string name, IVarHandleContainer container, VisualElement parent, INodesController nodesController, Action onChangeAdditional, LabelStyle labelStyle)
        {
            return CreateQuaternionField(
                name,
                ((IVarHandleContainerTyped<UnityEngine.Quaternion>)container).GetValue(0),
                parent,
                (newValue) =>
                {
                    var newQuat = new Quaternion(newValue.x, newValue.y, newValue.z, newValue.w);
                    nodesController.UpdateVariableValue(container, newQuat);
                    onChangeAdditional?.Invoke();
                },
                labelStyle);
        }

        private static Toggle CreateBoolField(string name, bool initialValue, Type _, VisualElement parent, Action<bool> onChange, LabelStyle labelStyle)
        {
            var field = new Toggle(name);
            field.SetValueWithoutNotify(initialValue);
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

        private static IntegerField CreateIntegerField(string name, int initialValue, Type _, VisualElement parent, Action<int> onChange, LabelStyle labelStyle)
        {
            var field = new IntegerField(name);
            field.SetValueWithoutNotify(initialValue);
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

        private static FloatField CreateFloatField(string name, float initialValue, Type _, VisualElement parent, Action<float> onChange, LabelStyle labelStyle)
        {
            var field = new FloatField(name);
            field.SetValueWithoutNotify(initialValue);
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

        private static TextField CreateStringField(string name, string initialValue, Type _, VisualElement parent, Action<string> onChange, LabelStyle labelStyle)
        {
            var field = new TextField(name);
            field.SetValueWithoutNotify(initialValue);
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

#if UNITY_2021_1_OR_NEWER

        private static DropdownField CreateDropDownField<T>(T initialValue, IVariantProviderTyped<T> variants, VisualElement parent, Action<string> onChange, LabelStyle labelStyle)
        {
            var variantsList = (List<string>)variants.Variants; // Poor DropdownField API

            var initialIndex = variants.GetVariantIndex(initialValue);
            var field = new DropdownField(variantsList, initialIndex);
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }
#endif

        private static Vector3Field CreateVector3Field(string name, Vector3 initialValue, Type _, VisualElement parent, Action<Vector3> onChange, LabelStyle labelStyle)
        {
            var field = new Vector3Field(name);
            FixVectorField(field);
            field.SetValueWithoutNotify(initialValue);
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

        private static Vector4Field CreateQuaternionField(string name, Quaternion initialValue, VisualElement parent, Action<Vector4> onChange, LabelStyle labelStyle)
        {
            var field = new Vector4Field(name);
            FixVectorField(field);
            field.SetValueWithoutNotify(new Vector4(initialValue.x, initialValue.y, initialValue.z, initialValue.w));
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

        private static ColorField CreateColorField(string name, UnityEngine.Color initialValue, Type _, VisualElement parent, Action<UnityEngine.Color> onChange, LabelStyle labelStyle)
        {
            var field = new ColorField(name);
            field.style.width = 64; // Compact the node
            field.SetValueWithoutNotify(initialValue);
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

        private static ObjectField CreateObjectField(string name, UnityEngine.Object initialValue, Type unityObjectType, VisualElement parent, Action<UnityEngine.Object> onChange, LabelStyle labelStyle)
        {
            var field = new ObjectField(name);
            field.allowSceneObjects = true;
            field.objectType = unityObjectType;
            field.value = initialValue;
            field.GeneralSetup(parent, onChange, labelStyle);

            return field;
        }

        private static void GeneralSetup<T>(this VisualElement field, VisualElement parent, Action<T> onChange, LabelStyle labelStyle)
        {
            field.RegisterCallback<ChangeEvent<T>>((evt) =>
            {
                onChange?.Invoke(evt.newValue);
            });

            if (labelStyle.styling != LabelStyling.none)
                field.FixLabelStyling(labelStyle);

            parent.Add(field);
        }

        private static void FixVectorField(VisualElement vectorField)
        {
            foreach (var item in vectorField.Query<FloatField>().ToList())
            {
                //reset flex so input field resizes based on content size
                {
                    item.style.flexBasis = new StyleLength(StyleKeyword.Auto);
                    item.style.flexGrow = new StyleFloat(StyleKeyword.Auto);
                    item.style.flexShrink = new StyleFloat(StyleKeyword.Auto);
                }

                var label = item.Q<Label>();
                label.style.minWidth = 0;
            }
        }

        internal static void FixLabelStyling(this VisualElement parent, LabelStyle labelStyle)
        {
            var label = parent.Q<Label>();
            if (label != null)
            {
                if (labelStyle.styling == LabelStyling.auto)
                    parent.Q<Label>().style.minWidth = new StyleLength { keyword = StyleKeyword.Auto };
                else if (labelStyle.styling == LabelStyling.minWidth)
                    parent.Q<Label>().style.minWidth = labelStyle.minWidth;
            }
        }
    }

    internal enum LabelStyling
    {
        none,
        auto,
        minWidth
    }

    internal struct LabelStyle
    {
        internal LabelStyling styling;
        internal int minWidth;

        internal LabelStyle(LabelStyling styling, int minWidth)
        {
            this.styling = styling;
            this.minWidth = minWidth;
        }
        internal LabelStyle(LabelStyling styling)
        {
            this.styling = styling;
            this.minWidth = 0;
        }

        internal static LabelStyle defaultBlackboardStyle = new LabelStyle(LabelStyling.minWidth, 100);
    }
}