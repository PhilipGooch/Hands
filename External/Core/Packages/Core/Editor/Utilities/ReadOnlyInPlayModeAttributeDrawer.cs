using UnityEngine;
using UnityEditor;

namespace NBG.Core.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyInPlayModeFieldAttribute))]
    public class ReadOnlyInPlayModeFieldAttributeDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            return 0.0f;
        }

        public override void OnGUI(Rect position)
        {
            GUI.enabled = !Application.isPlaying;
            // PropertyDrawer that draws the property will restore GUI state. Note that this is a DecoratorDrawer.
            // Is this a hack?
            // Who knows!
        }
    }
}
