using System;
using UnityEngine;

namespace Recoil
{

    public class BodyDetector : MonoBehaviour
    {
        private BodyIdTriggerProximityTracker detectedObjects = new BodyIdTriggerProximityTracker();

        public event Action<int> OnRigidBodyEnter;
        public event Action<int> OnRigidBodyLeave;

        private Collider myCollider = null;
        private Collider MyCollider
        {
            get
            {
                if (myCollider == null)
                    myCollider = GetComponent<Collider>();
                return myCollider;
            }
        }

        /// <summary>
        /// Checking only on triggerEnter / exit might be error prone to edge cases, 
        /// when object is disabled / reenabled inside as enter calls are not invoked.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            int bodyId = detectedObjects.OnTriggerEnter(other);

            if (!World.IsEnvironment(bodyId))
            {
                OnRigidBodyEnter?.Invoke(bodyId);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            int bodyId = detectedObjects.OnTriggerLeave(other);

            if (!World.IsEnvironment(bodyId))
            {
                OnRigidBodyLeave?.Invoke(bodyId);
            }
        }

        public void ForceRemoveEntry(int bodyId)
        {
            detectedObjects.ForceRemoveEntry(bodyId);
        }

        public bool IsNearCollider(Vector3 worldPoint, float distance)
        {
            const float kEpsilon = 1e-5f;
            if (distance < kEpsilon)
            {
                Debug.LogWarning($"Checking for a very small distance between point and collider. Possibility of rounding errors, " +
                    $"please use at least {kEpsilon} even if you want to check only whether the point is inside the collider.");
                distance = kEpsilon;
            }
            
            Vector3 closestPoint = MyCollider.ClosestPoint(worldPoint);
            return (closestPoint - worldPoint).sqrMagnitude <= distance * distance;
        }
    }
}
