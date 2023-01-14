using Recoil;
using UnityEngine;

public class SynchronizeRotation : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y,
        Z
    }

    [SerializeField]
    Transform copyTarget;

    [SerializeField]
    float rotationMultiplier = 1;

    public Axis copyFromLocalAxis;
    public Axis copyTolocalAxis; // not used with hindge joint

    Rigidbody targetRigidbody;
    ReBody reBody;
    new HingeJoint hingeJoint;
    ConfigurableJoint configurableJoint; 

    Vector3 startingAngles;

    private void Start()
    {
        targetRigidbody = copyTarget.GetComponent<Rigidbody>();
        reBody = new ReBody(targetRigidbody);
        startingAngles = transform.localEulerAngles;
        hingeJoint = GetComponent<HingeJoint>();
        configurableJoint = GetComponent<ConfigurableJoint>();

        //coonfigurable joint rotate if damping is 0, only touch if no hinge joint 
        if (configurableJoint && !hingeJoint)
        {
            var xDrive = configurableJoint.angularXDrive;
            xDrive.positionDamper = 100;
            configurableJoint.angularXDrive = xDrive;

            var yzDrive = configurableJoint.angularYZDrive;
            yzDrive.positionDamper = 100;
            configurableJoint.angularYZDrive = yzDrive;
        }
    }

    private void FixedUpdate()
    {
      
        if (hingeJoint && targetRigidbody)
        {
            var targetLocalAngularVel = reBody.InverseTransformDirection(reBody.angularVelocity);
            var motor = hingeJoint.motor;
            //hinge joint motor uses degrees for velocity
            motor.targetVelocity = Mathf.Rad2Deg * GetSingleAxisVelocity(targetLocalAngularVel, copyFromLocalAxis) * rotationMultiplier;

            hingeJoint.motor = motor;
        }
        else if (configurableJoint && targetRigidbody)
        {
            var targetLocalAngularVel = reBody.InverseTransformDirection(reBody.angularVelocity);
            //configurable joint angular velocity uses radians
            configurableJoint.targetAngularVelocity = MapAngleToAxis(Vector3.zero, GetSingleAxisVelocity(targetLocalAngularVel, copyFromLocalAxis), copyTolocalAxis) * rotationMultiplier;
        }
        else if ((!hingeJoint && !configurableJoint) || !targetRigidbody) // move this object as a non physics object
        {
            var copyTargetRotation = copyTarget.localEulerAngles;
            var targetRotation = MapAngleToAxis(startingAngles, GetSingleAxisVelocity(copyTargetRotation, copyFromLocalAxis), copyTolocalAxis) * rotationMultiplier;

            transform.localRotation = Quaternion.Euler(targetRotation);
        }
    }

    float GetSingleAxisVelocity(Vector3 from, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                return from.x;
            case Axis.Y:
                return from.y;
            case Axis.Z:
                return from.z;
            default:
                return 0;
        }

    }

    Vector3 MapAngleToAxis(Vector3 mapTo, float angle, Axis axis)
    {
        switch (axis)
        {
            case Axis.X:
                return new Vector3(angle, mapTo.y, mapTo.z);
            case Axis.Y:
                return new Vector3(mapTo.x, angle, mapTo.z);
            case Axis.Z:
                return new Vector3(mapTo.x, mapTo.y, angle);
            default:
                return mapTo;
        }
    }
}
