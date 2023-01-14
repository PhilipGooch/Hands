using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{
    [CustomEditor(typeof(NodeGraph))]
    public class NodeGraphEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            //var atten = (target as CalculateAttenuation);
            //if (atten != null)
            if (GUILayout.Button("Show Graph"))
            {
                NodeWindow.Init(target as NodeGraph);

            }
        }

        [MenuItem("No Brakes Games/Node Graph...")]
        public static void OpenWindow()
        {
            NodeGraph graph = null;
            Transform selected = Selection.activeTransform;
            while (graph == null && selected != null)
            {
                graph = selected.GetComponent<NodeGraph>();
                selected = selected.parent;
            }

            if (graph == null)
            {
                var window = EditorWindow.GetWindow(typeof(NodeWindow)) as NodeWindow;
                if (window && window.activeGraph)
                    NodeWindow.Init(window.activeGraph);
            }
            else
                NodeWindow.Init(graph);
        }
    }
}
