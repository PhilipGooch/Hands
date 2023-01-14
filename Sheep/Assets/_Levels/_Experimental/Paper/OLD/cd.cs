using UnityEngine;

[ExecuteInEditMode]
public class cd : MonoBehaviour
{
    void Update()
    {
        float angle = -Vector3.Angle(-Manager.a2b23, Manager.XY3 - Manager.a2b23);
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
