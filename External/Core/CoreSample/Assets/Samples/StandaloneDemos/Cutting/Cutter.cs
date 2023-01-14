using UnityEngine;

namespace CoreSample.Cutting
{
    public class Cutter : MonoBehaviour
    {
        public float velocityThresholdForCutting = 2.0f;
        public float cutWidth;

        private void OnCollisionEnter(Collision collision)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            float velocity = rb.velocity.magnitude;
            CuttableCylinder cuttableCylinder = collision.collider.gameObject.GetComponent<CuttableCylinder>();
            Transform cylinderTransform = collision.collider.gameObject.transform;

            if (velocity > velocityThresholdForCutting && cuttableCylinder != null)
            {
                cuttableCylinder.Cut(
                    cylinderTransform.TransformPoint(collision.contacts[0].point),
                    cylinderTransform.TransformDirection(transform.forward),
                    cutWidth
                    );
            }
        }
    }
}
