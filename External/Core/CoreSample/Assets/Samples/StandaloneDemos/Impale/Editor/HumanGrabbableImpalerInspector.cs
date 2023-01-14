using NBG.Impale;
using UnityEditor;

namespace CoreSample.ImpaleDemo
{
    [CustomEditor(typeof(HumanGrabbableImpaler), false)]
    [CanEditMultipleObjects]
    public class HumanGrabbableImpalerInspector : ImpalerInspector
    {
        public override void OnInspectorGUI()
        {
            DrawInspector();
        }

        protected override void DrawInspector()
        {
            base.DrawInspector();

            AddProperty(serializedObject, "impaleOnlyIfGrabbed");
            AddProperty(serializedObject, "allowPullOutIfGrabbed");

            serializedObject.ApplyModifiedProperties();

        }
    }
}
