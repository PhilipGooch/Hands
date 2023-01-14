using UnityEngine;

namespace NBG.Core
{
    public static class PhysicsUtils
    {
        // Utility function that gets a closest point on collider for convex colliders
        // otherwise it returns the closest point on bounds
        public static Vector3 ClosestPointSafe(this Collider collider, Vector3 point)
        {
            bool isConvex = true;
            if (collider is MeshCollider)
            {
                var meshCol = collider as MeshCollider;
                isConvex = meshCol.convex;
            }
            var nearestPoint = collider.transform.position;
            if (isConvex)
            {
                nearestPoint = collider.ClosestPoint(point);
            }
            else // non-convex meshes can't use ClosestPoint
            {
                nearestPoint = collider.ClosestPointOnBounds(point);
            }
            return nearestPoint;
        }

        public static Vector3 GetApproximateSize(Rigidbody rig)
        {
            var colliders = rig.GetComponentsInChildren<Collider>(false);
            var bounds = new Bounds(rig.position, Vector3.zero);
            foreach (var col in colliders)
            {
                bounds.Encapsulate(col.bounds);
            }
            return bounds.size;
        }
    }
}
