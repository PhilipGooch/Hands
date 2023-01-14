using UnityEngine;

namespace UnityJointAndSleepingRigidbodyInteraction
{
    public class TimedFixedJoint : MonoBehaviour
    {
        [SerializeField] Rigidbody other;
        FixedJoint joint;

        [Range(1, 1000)]
        [SerializeField] int fixedUpdates = 100;

        void Start()
        {
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = other;
        }

        int iterations = 0;

        void FixedUpdate()
        {
            if (++iterations == fixedUpdates)
            {
                Destroy(joint);
                Debug.Log("Falling!");
            }
        }
    }
}
