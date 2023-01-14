using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public class CheckboxReverse
    {
        const string uxmlGUID = "1c49fdea3a4e6024a883106c9629c899";

        public VisualElement component;

        public Toggle toggle;
        public TextElement label;
        public CheckboxReverse(string text, VisualElement root)
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(uxmlGUID));
            component = visualTreeAsset.Instantiate();
            root.Add(component);

            toggle = component.Q<Toggle>("toggle");
            label = component.Q<TextElement>("label");


            label.text = text;
            
        }
    }
}
