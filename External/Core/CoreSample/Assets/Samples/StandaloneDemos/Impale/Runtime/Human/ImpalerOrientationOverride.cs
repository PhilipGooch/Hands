using NBG.Impale;
using UnityEngine;

namespace CoreSample.ImpaleDemo
{
    public class ImpalerOrientationOverride : MonoBehaviour, IImpalerOrientationOverride
    {
        [Tooltip("In world space")]
        [SerializeField]
        Vector3 normal;
        public Vector3 Normal => normal;

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawRay(transform.position, normal);
        }
    }
}
