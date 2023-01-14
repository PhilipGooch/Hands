using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace NBG.NodeGraph.Editor
{
    public abstract class PropertyDrawCondition
    {
        public abstract bool CanDrawProperty(SerializedProperty property);
    }

    public class ShowIfPropertyDrawCondition : PropertyDrawCondition
    {
        public override bool CanDrawProperty(SerializedProperty property)
        {
            ShowIfAttribute showIfAttribute = ReflectionUtility.GetAttribute<ShowIfAttribute>(property);
            UnityEngine.Object target = ReflectionUtility.GetTargetObject(property);

            List<bool> conditionValues = new List<bool>();
            foreach (var condition in showIfAttribute.Conditions)
            {
                FieldInfo conditionField = ReflectionUtility.GetField(target, condition);
                if (conditionField != null &&
                    conditionField.FieldType == typeof(bool))
                {
                    conditionValues.Add((bool)conditionField.GetValue(target));
                }

                var conditionProperty = ReflectionUtility.GetProperty(target, condition);
                if (conditionProperty != null &&
                    conditionProperty.PropertyType == typeof(bool))
                {
                    conditionValues.Add((bool)conditionProperty.GetValue(target, null));
                }

                MethodInfo conditionMethod = ReflectionUtility.GetMethod(target, condition);
                if (conditionMethod != null &&
                    conditionMethod.ReturnType == typeof(bool) &&
                    conditionMethod.GetParameters().Length == 0)
                {
                    conditionValues.Add((bool)conditionMethod.Invoke(target, null));
                }
            }

            if (conditionValues.Count > 0)
            {
                bool draw;
                if (showIfAttribute.ConditionOperator == NBGAttribute.ConditionOperator.And)
                {
                    draw = true;
                    foreach (var value in conditionValues)
                    {
                        draw = draw && value;
                    }
                }
                else
                {
                    draw = false;
                    foreach (var value in conditionValues)
                    {
                        draw = draw || value;
                    }
                }

                if (showIfAttribute.Reversed)
                {
                    draw = !draw;
                }

                return draw;
            }
            else
            {
                string warning = showIfAttribute.GetType().Name + " needs a valid boolean condition field or method name to work";
                EditorDrawUtility.DrawHelpBox(warning, MessageType.Warning, context: target);

                return true;
            }
        }
    }
}
