using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph.Editor
{
    public abstract class NBGPropertyDrawer
    {
        public abstract void DrawProperty(SerializedProperty property);
    }

    public class ReadOnlyPropertyDrawer : NBGPropertyDrawer
    {
        public override void DrawProperty(SerializedProperty property)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(property, true);
            GUI.enabled = true;
        }
    }
    public class RenamePropertyDrawer : NBGPropertyDrawer
    {
        public override void DrawProperty(SerializedProperty property)
        {
            var attribute = ReflectionUtility.GetAttribute<RenameAttribute>(property);
            EditorGUILayout.PropertyField(property, new GUIContent(attribute.name), true);
        }
    }

    public class InfoBoxPropertyDrawer : NBGPropertyDrawer
    {
        public override void DrawProperty(SerializedProperty property)
        {
            var infos = ReflectionUtility.GetAttributes<InfoBoxAttribute>(property);
            foreach (var info in infos)
            {
                EditorDrawUtility.DrawHelpBox(info.text, (MessageType)((int)info.type), context: ReflectionUtility.GetTargetObject(property));
            }
        }
    }
}
