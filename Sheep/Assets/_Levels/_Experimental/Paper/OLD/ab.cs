using UnityEngine;

[ExecuteInEditMode]
public class ab : MonoBehaviour
{
    void Update()
    {
        float angle = Vector3.Angle(-Manager.a1b13, Manager.XY3 - Manager.a1b13); 
        transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
