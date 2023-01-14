using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
    public class H2Inspector : UnityEditor.Editor
    {
        protected SerializedProperty script;
        protected FieldInfo[] fields;
        protected bool useDefaultInspector;
        Dictionary<string, SerializedProperty> serializedPropertiesByFieldName;

        private HashSet<FieldInfo> groupedFields;
        private Dictionary<string, List<FieldInfo>> groupedFieldsByGroupName;

        protected virtual void OnEnable()
        {
            if (target == null)
                return;

            script = this.serializedObject.FindProperty("m_Script");

            fields = GetPublicFields(target, f => this.serializedObject.FindProperty(f.Name) != null);
            // Cache serialized properties by field name
            serializedPropertiesByFieldName = new Dictionary<string, SerializedProperty>();
            groupedFieldsByGroupName = new Dictionary<string, List<FieldInfo>>();
            drawnGroups = new HashSet<string>();
            foreach (var field in fields)
                serializedPropertiesByFieldName[field.Name] = serializedObject.FindProperty(field.Name);

            useDefaultInspector = fields.Where(f => f.GetCustomAttributes(typeof(NBGAttribute), true).Length > 0).Count() == 0;

            if (useDefaultInspector == false)
            {
                // Cache grouped fields
                groupedFields = new HashSet<FieldInfo>(fields.Where(f => f.GetCustomAttributes(typeof(GroupAttribute), true).Length > 0));

                foreach (var groupedField in groupedFields)
                {
                    string groupName = (groupedField.GetCustomAttributes(typeof(GroupAttribute), true)[0] as GroupAttribute).Name;
                    if (this.groupedFieldsByGroupName.ContainsKey(groupName))
                        groupedFieldsByGroupName[groupName].Add(groupedField);
                    else
                        groupedFieldsByGroupName[groupName] = new List<FieldInfo>() { groupedField };
                }

                // Cache serialized properties by field name
                serializedPropertiesByFieldName = new Dictionary<string, SerializedProperty>();
                foreach (var field in fields)
                    serializedPropertiesByFieldName[field.Name] = serializedObject.FindProperty(field.Name);
            }
        }

        public override void OnInspectorGUI()
        {
            if (useDefaultInspector)
            {
                DrawDefaultInspector();
            }
            else
            {
                NBGInspectorOverride();
                serializedObject.ApplyModifiedProperties();
            }
        }
        HashSet<string> drawnGroups;
        protected virtual void NBGInspectorOverride()
        {
            this.serializedObject.Update();
            if (script != null)
                DrawReadOnly(script);
            if (drawnGroups == null)
                drawnGroups = new HashSet<string>();
            else
                drawnGroups.Clear();
            if (fields == null) return;
            // Draw fields
            foreach (var field in fields)
            {
                if (this.groupedFields.Contains(field))
                {
                    // Draw grouped fields
                    string groupName = (field.GetCustomAttributes(typeof(GroupAttribute), true)[0] as GroupAttribute).Name;
                    if (!drawnGroups.Contains(groupName))
                    {
                        drawnGroups.Add(groupName);
                        BeginGroup(groupName);
                        EditorGUI.indentLevel++;
                        ValidateAndDrawFields(groupedFieldsByGroupName[groupName]);
                        EditorGUI.indentLevel--;
                        EndGroup();
                    }
                }
                else
                {
                    // Draw non-grouped field
                    ValidateAndDrawField(field);
                }
            }

        }

        void ValidateAndDrawFields(List<FieldInfo> fields)
        {
            foreach (var field in fields)
                ValidateAndDrawField(field);
        }

        void ValidateAndDrawField(FieldInfo field)
        {
            if (ShouldDrawField(field) == false)
                return;
            ValidateField(field);
            DrawMetas(field);
            DrawField(field);
        }
        void DrawMetas(FieldInfo field)
        {
            NBGPropertyDrawer[] drawers = GetMetaDrawerForField(field);
            if (drawers != null)
                foreach (var drawer in drawers)
                    drawer.DrawProperty(serializedPropertiesByFieldName[field.Name]);
        }
        bool ShouldDrawField(FieldInfo field)
        {
            // Check if the field has draw conditions
            PropertyDrawCondition drawCondition = GetPropertyDrawConditionForField(field);
            if (drawCondition != null)
            {
                bool canDrawProperty = drawCondition.CanDrawProperty(serializedPropertiesByFieldName[field.Name]);
                if (!canDrawProperty)
                {
                    return false;
                }
            }

            // Check if the field has HideInInspectorAttribute
            HideInInspector[] hideInInspectorAttributes = (HideInInspector[])field.GetCustomAttributes(typeof(HideInInspector), true);
            if (hideInInspectorAttributes.Length > 0)
            {
                return false;
            }

            return true;
        }

        private PropertyDrawCondition GetPropertyDrawConditionForField(FieldInfo field)
        {
            DrawConditionAttribute[] drawConditionAttributes = (DrawConditionAttribute[])field.GetCustomAttributes(typeof(DrawConditionAttribute), true);
            if (drawConditionAttributes.Length > 0)
            {
                PropertyDrawCondition drawCondition = DrawConditionDatabase.GetDrawConditionForAttribute(drawConditionAttributes[0].GetType());
                return drawCondition;
            }
            else
            {
                return null;
            }
        }

        private void ValidateField(FieldInfo field)
        {
            ValidatorAttribute[] validatorAttributes = (ValidatorAttribute[])field.GetCustomAttributes(typeof(ValidatorAttribute), true);

            foreach (var attribute in validatorAttributes)
            {
                var validator = PropertyValidatorDatabase.GetValidatorForAttribute(attribute.GetType());
                validator.Validate(serializedPropertiesByFieldName[field.Name]);
            }
        }

        void DrawField(FieldInfo field)
        {
            EditorGUI.BeginChangeCheck();
            NBGPropertyDrawer drawer = GetPropertyDrawerForField(field);
            if (drawer != null)
            {
                drawer.DrawProperty(this.serializedPropertiesByFieldName[field.Name]);
            }
            else
            {
                EditorDrawUtility.DrawPropertyField(this.serializedPropertiesByFieldName[field.Name]);
            }

            if (EditorGUI.EndChangeCheck())
            {
                //OnValueChangedAttribute[] onValueChangedAttributes = (OnValueChangedAttribute[])field.GetCustomAttributes(typeof(OnValueChangedAttribute), true);
                //foreach (var onValueChangedAttribute in onValueChangedAttributes)
                //{
                //    PropertyMeta meta = PropertyMetaDatabase.GetMetaForAttribute(onValueChangedAttribute.GetType());
                //    if (meta != null)
                //    {
                //        meta.ApplyPropertyMeta(this.serializedPropertiesByFieldName[field.Name], onValueChangedAttribute);
                //    }
                //}
            }
        }

        private NBGPropertyDrawer GetPropertyDrawerForField(FieldInfo field)
        {
            DrawerAttribute[] drawerAttributes = (DrawerAttribute[])field.GetCustomAttributes(typeof(DrawerAttribute), true);
            if (drawerAttributes.Length > 0)
            {
                NBGPropertyDrawer drawer = AttributeDatabase.GetDrawerForAttribute(drawerAttributes[0].GetType());
                return drawer;
            }
            else
            {
                return null;
            }
        }
        private NBGPropertyDrawer[] GetMetaDrawerForField(FieldInfo field)
        {
            MetaAttribute[] drawerAttributes = (MetaAttribute[])field.GetCustomAttributes(typeof(MetaAttribute), true);
            if (drawerAttributes.Length > 0)
            {
                var metaDrawers = new NBGPropertyDrawer[drawerAttributes.Length];
                for (int i = 0; i < drawerAttributes.Length; i++)
                    metaDrawers[i] = AttributeDatabase.GetMetaForAttribute(drawerAttributes[i].GetType());
                return metaDrawers;
            }
            else
                return null;
        }

        void BeginGroup(string label)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            }
        }

        void EndGroup()
        {
            EditorGUILayout.EndVertical();
        }


        public void DrawReadOnly(SerializedProperty prop)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(prop);
            GUI.enabled = true;
        }

        public FieldInfo[] GetPublicFields(object target, Func<FieldInfo, bool> predicate)
        {
            List<Type> types = new List<Type>()
            {
                target.GetType()
            };

            while (types.Last().BaseType != null)
            {
                types.Add(types.Last().BaseType);
            }
            List<FieldInfo> infos = new List<FieldInfo>();

            for (int i = types.Count - 1; i >= 0; i--)
            {
                IEnumerable<FieldInfo> fieldInfos = types[i]
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(predicate);
                infos.AddRange(fieldInfos);
            }
            return infos.ToArray();
        }

        protected void DrawPropertiesExcluding(params string[] propNames)
        {
            if (script != null)
                DrawReadOnly(script);
            foreach (var field in fields)
            {
                if (propNames.Contains(field.Name))
                    continue;

                if (field.IsDefined(typeof(ReadOnlyAttribute)))
                    DrawReadOnly(serializedPropertiesByFieldName[field.Name]);
                else if (field.IsDefined(typeof(HideInInspector)) == false)
                    EditorGUILayout.PropertyField(serializedPropertiesByFieldName[field.Name], true);
            }
        }

        protected void DrawAllProperties()
        {
            DrawPropertiesExcluding();
        }
    }
}
