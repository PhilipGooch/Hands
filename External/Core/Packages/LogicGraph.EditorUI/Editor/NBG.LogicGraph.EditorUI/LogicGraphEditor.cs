using UnityEditor;
using UnityEngine;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// LogicGraphPlayer Inspector extension
    /// </summary>
    [CustomEditor(typeof(LogicGraphPlayer))]
    public class LogicGraphEditor : Editor
    {
        private LogicGraphPlayer nodeGraphPlayer;

        public override void OnInspectorGUI()
        {
            nodeGraphPlayer = target as LogicGraphPlayer;
            //EditorGUI.BeginDisabledGroup(true);
            //DrawDefaultInspector();
            //EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Show Graph"))
            {
                LogicGraphWindow.Init(new LogicGraphPlayerEditor(nodeGraphPlayer, nodeGraphPlayer.gameObject));
            }
        }
    }
}
