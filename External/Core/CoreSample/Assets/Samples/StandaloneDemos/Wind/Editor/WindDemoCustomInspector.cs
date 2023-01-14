using NBG.Wind;
using UnityEditor;

[CustomEditor(typeof(WindDemo), false)]
[CanEditMultipleObjects]
public class WindDemoCustomInspector : WindZoneInspector
{
    public override void OnInspectorGUI()
    {
        DrawDefaultEditor();
    }

    protected override void DrawDefaultEditor()
    {
        base.DrawDefaultEditor();
    }
}
