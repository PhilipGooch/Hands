using UnityEngine;

namespace NBG.Audio
{
    public class ChildrenGizmo : MonoBehaviour
    {
        [SerializeField] private bool debug = false;
        [SerializeField] private Color color = new Color(0, 0, 1, 0.3f);

        public void OnDrawGizmos()
        {
            if (!debug) return;

            BoxCollider[] boxes = GetComponentsInChildren<BoxCollider>();
            foreach (var col in boxes)
            {
                var backup = Gizmos.matrix;
                Gizmos.matrix = col.transform.localToWorldMatrix;
                Gizmos.color = color;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.matrix = backup;
            }

            SphereCollider[] spheres = GetComponentsInChildren<SphereCollider>();
            foreach (var col in spheres)
            {

                // var backup = Gizmos.matrix;
                // Gizmos.matrix = col.transform.localToWorldMatrix;
                // Gizmos.color = color;
                Gizmos.DrawSphere(col.transform.position, col.radius);
                //Gizmos.matrix = backup;
            }
        }
    }
}
