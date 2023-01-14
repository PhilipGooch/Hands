using UnityEditor;

//Needed because custom inspectors for base class (FlickSwitchBaseInspector) dont work
[CustomEditor(typeof(FlickDualSwitchActivator))]
public class FlickDualSwitchInspector : FlickSwitchBaseInspector
{

}

