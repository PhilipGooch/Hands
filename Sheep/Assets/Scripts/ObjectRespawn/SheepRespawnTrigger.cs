using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepRespawnTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var sheep = other.GetComponentInParent<Sheep>();
        if (sheep != null)
        {
            sheep.NeedsRespawn = true;
        }
    }

#if UNITY_EDITOR
    static Color editorZoneColor = new Color(196f / 255, 2f / 255, 51f / 255, 0.5f);

    private void OnDrawGizmosSelected()
    {
        var oldMatrix = Gizmos.matrix;
        var oldColor = Gizmos.color;

        Gizmos.color = editorZoneColor;
        var parent = transform;
        Matrix4x4 matrix = transform.localToWorldMatrix;
        
        Gizmos.matrix = matrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);

        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }
#endif
}
