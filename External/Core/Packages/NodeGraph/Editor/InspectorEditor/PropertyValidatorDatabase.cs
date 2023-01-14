using System;
using System.Collections.Generic;
using UnityEditor;

namespace NBG.NodeGraph.Editor
{

    static class PropertyValidatorDatabase
    {
        private static Dictionary<Type, PropertyValidator> validatorsByAttributeType;

        static PropertyValidatorDatabase()
        {
            validatorsByAttributeType = new Dictionary<Type, PropertyValidator>();
            validatorsByAttributeType[typeof(RequiredAttribute)] = new RequiredPropertyValidator();
        }

        public static PropertyValidator GetValidatorForAttribute(Type attributeType)
        {
            PropertyValidator validator;
            if (validatorsByAttributeType.TryGetValue(attributeType, out validator))
            {
                return validator;
            }
            else
            {
                return null;
            }
        }
    }

    public abstract class PropertyValidator
    {
        public abstract void Validate(SerializedProperty property);
    }

    public class RequiredPropertyValidator : PropertyValidator
    {
        public override void Validate(SerializedProperty property)
        {
            RequiredAttribute requiredAttribute = ReflectionUtility.GetAttribute<RequiredAttribute>(property);

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (property.objectReferenceValue == null)
                {
                    string errorMessage = property.name + " is required";
                    if (!string.IsNullOrEmpty(requiredAttribute.Message))
                    {
                        errorMessage = requiredAttribute.Message;
                    }

                    EditorDrawUtility.DrawHelpBox(errorMessage, MessageType.Error, context: ReflectionUtility.GetTargetObject(property));
                }
            }
            else
            {
                string warning = requiredAttribute.GetType().Name + " works only on reference types";
                EditorDrawUtility.DrawHelpBox(warning, MessageType.Warning, context: ReflectionUtility.GetTargetObject(property));
            }
        }
    }
}
