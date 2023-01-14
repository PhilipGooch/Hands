using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class PortsDividerView : VisualElement
    {
        private const string k_UXMLGUID = "c1fc59016ff13af4cb9dba3b2627ac1d";
        public new class UxmlFactory : UxmlFactory<PortsDividerView, VisualElement.UxmlTraits> { }

        public PortsDividerView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);
        }
    }
}