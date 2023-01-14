using UnityEditor;
using UnityEngine;

namespace MeshProcess
{
    [CustomEditor(typeof(VHACDParameters))]
    internal class VHACDParametersEditor : Editor
    {
        VHACDParameters parameters;
        void OnEnable()
        {
            parameters = (VHACDParameters)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(parameters.sourceMeshReference, typeof(Mesh),false);
            EditorGUI.EndDisabledGroup();

        }
    }
}
