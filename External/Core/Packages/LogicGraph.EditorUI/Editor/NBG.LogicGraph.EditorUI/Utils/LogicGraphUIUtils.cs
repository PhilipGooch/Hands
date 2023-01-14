using NBG.LogicGraph.EditorInterface;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    //acts as a temporary holding place for methods which will be moved to core
    internal static class LogicGraphUtils
    {
        public static void FixLightSkinLabel(this Label label)
        {
            label.style.color = Color.black;
        }

        public static void FixTabBackgroundColor(this VisualElement tab)
        {
            if (EditorGUIUtility.isProSkin)
                tab.style.backgroundColor = Parameters.darkSkinTabBackgroundColor;
            else
                tab.style.backgroundColor = Parameters.lightSkinTabBackgroundColor;
        }

        public static void Ping(INodeObjectReference reference)
        {
            if (reference != null && reference.Target != null)
            {
                EditorGUIUtility.PingObject(reference.Target);
            }
        }

        public static void Ping(UnityEngine.Object relativeObj)
        {
            if (relativeObj != null)
            {
                EditorGUIUtility.PingObject(relativeObj);
            }
        }
    }
}

