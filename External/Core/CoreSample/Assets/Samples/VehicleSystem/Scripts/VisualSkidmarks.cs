using Recoil;
using System;
using UnityEngine;

namespace NBG.VehicleSystem
{
    /// <summary>
    /// Vehicle addon which decides if tire needs to leave skidmark based on velocity and position of the tire
    /// </summary>
    //[RequireComponent(typeof(IWheel))]
    public class VisualSkidmarks : MonoBehaviour//, IAddon
    {
        /// <summary>
        /// Vector3 - point in worl
        /// Vector3 - normal
        /// flaot - intensity [0;1]
        /// int - index of last left skidmark (could be used for remembering which skidmark is left by which wheel or similar
        /// result - return new laskSkid index
        /// </summary>
        /*public event Func<Vector3, Vector3, float, int, int> OnSkidmarksLeft;

        private Rigidbody wheelRigidbody;
        private ReBody wheelRigidbodyConverter;
        //private IWheel wheel;
        private Rigidbody mainRigidbody;
        private ReBody rigidbodyToRecoilConverter;

        private float radius;

        private const float RAYCAST_DISTANCE_MULTIPLY = 1.1f;
        private const float MAX_SKID_INTENSITY = 20.0f;
        //We don't have width of the wheel right now, so we are using constant for that
        private const float WHEEL_WIDTH = 0.5f;

        private Vector3 collisionPoint;
        private Vector3 normal;
        private int lastSkid;

        void IChassisComponent.AttachTo(IChassis chassis)
        {
            mainRigidbody = chassis.MainRigidbody;
            rigidbodyToRecoilConverter = new ReBody(mainRigidbody);

            wheel = GetComponent<IWheel>();
            Debug.Assert(wheel != null);

            wheelRigidbody = GetComponent<Rigidbody>();
            Debug.Assert(wheelRigidbody != null, "VisualSkidmarks needs rigidbody on wheel");

            wheelRigidbodyConverter = new ReBody(wheelRigidbody);

            radius = wheel.Radius;
        }

        void IChassisComponent.OnFixedUpdate(float fixedDeltaTime)
        {
            if (NeedToCalculateSkidmarks())
            {
                LeaveSkidmark();
            }
        }

        //check if vehicle is grounded or not(by shooting a ray downwards)
        //TODO: more than one optional helper scripts are doing similar things, maybe it could be exposed somewhere for performance
        private bool NeedToCalculateSkidmarks() 
        {
            if (Physics.Raycast(transform.position, -mainRigidbody.transform.up, out RaycastHit hit, radius * RAYCAST_DISTANCE_MULTIPLY))
            {
                if (hit.rigidbody != null)
                {
                    //dont leave skidmarks if on another rigidbody
                    return false;
                }

                collisionPoint = hit.point;
                normal = hit.normal;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void LeaveSkidmark()
        {
            float wheelAngularVelocity = Mathf.Abs(transform.InverseTransformDirection(wheelRigidbodyConverter.angularVelocity).x);
            float wheelForwardVel = Mathf.Abs(Vector3.Dot(transform.InverseTransformDirection(wheelRigidbodyConverter.velocity), mainRigidbody.transform.forward));
            float skidTotal = Mathf.Abs(wheelAngularVelocity * radius - transform.InverseTransformDirection(wheelRigidbodyConverter.velocity).magnitude);

            if (wheelAngularVelocity * WHEEL_WIDTH > wheelForwardVel + 1)
            {
                float intensity = Mathf.Clamp01(skidTotal / MAX_SKID_INTENSITY);

                Vector3 skidPoint = collisionPoint;

                if (OnSkidmarksLeft != null)
                {
                    lastSkid = OnSkidmarksLeft.Invoke(skidPoint, normal, intensity, lastSkid);
                }
            }
            else if (wheelAngularVelocity * WHEEL_WIDTH < wheelForwardVel - 1)
            {
                float intensity = Mathf.Clamp01(skidTotal / MAX_SKID_INTENSITY);

                Vector3 skidPoint = collisionPoint;

                if (OnSkidmarksLeft != null)
                {
                    lastSkid = OnSkidmarksLeft.Invoke(skidPoint, normal, intensity, lastSkid);
                }
            }
            else
            {
                lastSkid = -1;
            }
        }*/
    }
}
