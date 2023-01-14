using UnityEditor;
using UnityEngine;

namespace NBG.Conveyors
{
    [CustomEditor(typeof(Conveyor))]
    public class ConveyorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Generate"))
            {
                var conveyor = (Conveyor)target;
                conveyor.Generate();
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
