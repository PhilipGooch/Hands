using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NBG.VehicleSystem.Editor
{
    /// <summary>
    /// This is helper for creating wheel collider from multiple shere smaller colliders around the wheel and one box collider in the middle.
    /// Should be used just while making vehicle and deleted aftar that.
    /// Bevare, as it doesn't mark object as dirty.
    /// </summary>
    [ExecuteAlways] //TODO: this should be reviewed
    public class CreateWheelColliderFromSpheres : MonoBehaviour
    {
        [SerializeField]
        private float wheelRadius;
        [SerializeField]
        private PhysicMaterial material;

        [SerializeField]
        private int colliderCount;
        [SerializeField]
        private float newColliderRadius;

        [Header("Edit")]
        [SerializeField]
        private bool createCollidersNow;
        [SerializeField]
        private bool removeAllColliders;

        private void Start()
        {
            Debug.Assert(false, "This object is just helper for wheel creation. It should never be present in runtime", gameObject);    
        }

        void Update()
        {
            if (createCollidersNow)
            {
                createCollidersNow = false;

                CreateWheelNow();
            }

            if (removeAllColliders)
            {
                removeAllColliders = false;

                var colliders = transform.GetComponents<Collider>().ToList();
                for (int i = colliders.Count - 1; i >= 0; i--)
                {
                    DestroyImmediate(colliders[i]);
                }
            }
        }

        private void CreateWheelNow()
        {
            for (int i = 0; i < colliderCount; i++)
            {
                float angle = 360f / colliderCount * i;
                Vector3 pointForColliderCenter = RandomPointOnXZCircle(transform.localPosition, wheelRadius - newColliderRadius, angle);
                var collider = transform.gameObject.AddComponent<SphereCollider>();
                collider.center = pointForColliderCenter;
                collider.radius = newColliderRadius;
                collider.material = material;
            }
            var boxCollider = transform.gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(newColliderRadius, wheelRadius, wheelRadius);
        }

        Vector3 RandomPointOnXZCircle(Vector3 center, float radius, float angle)
        {
            angle = Mathf.Deg2Rad * angle;
            return center + new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }
}