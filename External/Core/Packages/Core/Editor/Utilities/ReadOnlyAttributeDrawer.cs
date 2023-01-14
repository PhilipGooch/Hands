using UnityEngine;
using UnityEditor;

namespace NBG.Core.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
    public class ReadOnlyFieldAttributeDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            return 0.0f;
        }

        public override void OnGUI(Rect position)
        {
            GUI.enabled = false;
            // PropertyDrawer that draws the property will restore GUI state. Note that this is a DecoratorDrawer.
            // Is this a hack?
            // Who knows!
        }
    }
}
