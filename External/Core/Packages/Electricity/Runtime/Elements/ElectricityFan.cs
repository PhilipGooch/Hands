using System.Collections.Generic;
using UnityEngine;

namespace NBG.Electricity
{
    public class ElectricityFan : ElectricityReceiver
    {
        [SerializeField] float forceFullPower = 100;
        [SerializeField] float fanSpeedFullPower = 1000;
        [SerializeField] float maxDistanceToReceiveForce = 10;
        [SerializeField] HingeJoint fanHinge;
        [SerializeField] AnimationCurve forceFalloffCurve;
        List<Rigidbody> bodiesInCollider = new List<Rigidbody>(); // Can contain duplicates
        JointMotor tempJointMotor;

        protected override void Start()
        {
            base.Start();
            if (fanHinge != null)
            {
                fanHinge.gameObject.GetComponent<Rigidbody>().maxAngularVelocity = 10;
                tempJointMotor = fanHinge.motor;
                tempJointMotor.force = fanHinge.motor.force;
            }
        }

        void FixedUpdate()
        {
            if (Input > 0)
            {
                Vector3 forceToAdd = transform.forward * forceFullPower * (Input / maxAmps);
                foreach (Rigidbody rb in bodiesInCollider)
                {
                    if (rb == null)
                        continue;
                    rb.AddForce(forceToAdd * (forceFalloffCurve.Evaluate(Vector3.Distance(transform.position, rb.position) / maxDistanceToReceiveForce))); // add force based on distance to fan
                }
            }
            tempJointMotor = fanHinge.motor;
            tempJointMotor.targetVelocity = fanSpeedFullPower * (Input / maxAmps);
            fanHinge.motor = tempJointMotor;
        }

        private void OnTriggerEnter(Collider other)
        {
            var body = other.GetComponentInParent<Rigidbody>();
            if (body == null)
                return;
            if (!bodiesInCollider.Contains(body))
                bodiesInCollider.Add(body);
        }
        private void OnTriggerExit(Collider other)
        {
            var body = other.GetComponentInParent<Rigidbody>();
            if (body == null)
                return;
            if (bodiesInCollider.Contains(body))
                bodiesInCollider.Remove(body);
        }
    }
}
