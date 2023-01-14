using UnityEngine;

[ExecuteInEditMode]
public class L : MonoBehaviour
{
    void Update()
    {
        float angle = 180 + Manager.Theta;
        transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
