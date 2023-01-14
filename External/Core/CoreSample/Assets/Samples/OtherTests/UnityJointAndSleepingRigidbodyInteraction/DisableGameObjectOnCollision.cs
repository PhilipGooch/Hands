using UnityEngine;

namespace UnityJointAndSleepingRigidbodyInteraction
{
    public class DisableGameObjectOnCollision : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("DisableGameObjectOnCollision.OnCollisionEnter");
            gameObject.SetActive(false);
        }
    }
}
