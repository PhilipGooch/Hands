using UnityEngine;

public class GizmoBox : MonoBehaviour
{
    [SerializeField]
    Vector3 size;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, size);
    }
}
