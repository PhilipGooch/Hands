using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core.Editor
{
    /// <summary>
    /// UI Toolkit Elements Editor extensions
    /// </summary>
    public static class VisualElementsEditorExtensions
    {
        public static void FillDefaultInspector(VisualElement container, UnityEngine.Object toSerialize, bool hideScript)
        {
            var editor = UnityEditor.Editor.CreateEditor(toSerialize);
            IMGUIContainer inspectorIMGUI = new IMGUIContainer(() => { editor.OnInspectorGUI(); });
            inspectorIMGUI.style.flexGrow = 1;

            container.Add(inspectorIMGUI);;
        }

        public static Texture2D GetIconBaseOnObjectType(UnityEngine.Object target)
        {
            var content = EditorGUIUtility.ObjectContent(target, target.GetType());
            if (content == null)
                content = EditorGUIUtility.IconContent("d_GameObject Icon");

            if (content != null)
            {
                return content.image as Texture2D;
            }

            Debug.LogWarning("NO ICON FOUND");
            return new Texture2D(1, 1);
        }

        public static Texture2D GetUnityBuiltinIcon(string iconName)
        {
            var content = EditorGUIUtility.IconContent(iconName);

            if (content != null)
            {
                return content.image as Texture2D;
            }

            return new Texture2D(1, 1);
        }
    }
}
