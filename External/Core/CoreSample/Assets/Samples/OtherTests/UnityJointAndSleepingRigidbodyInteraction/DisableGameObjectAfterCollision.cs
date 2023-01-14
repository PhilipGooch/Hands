using UnityEngine;

namespace UnityJointAndSleepingRigidbodyInteraction
{
    public class DisableGameObjectAfterCollision : MonoBehaviour
    {
        [SerializeField] Rigidbody optionalBodyToAwaken;

        bool trigger;

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("DisableGameObjectAfterCollision.OnCollisionEnter");
            trigger = true;
        }

        private void FixedUpdate()
        {
            if (trigger)
            {
                trigger = false;
                gameObject.SetActive(false);
                if (optionalBodyToAwaken != null)
                    optionalBodyToAwaken.WakeUp();
            }
        }
    }
}
