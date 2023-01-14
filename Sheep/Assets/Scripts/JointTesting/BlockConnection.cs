using NBG.Core;
using Recoil;
using UnityEngine;

public class BlockConnection
{
    public BlockSocket A { get; private set; }
    public BlockSocket B { get; private set; }

    ConfigurableJoint joint;

    // Needed for graph traversal condition.
    public bool Locked { get; set; }

    // When a joint is created, it saves the relative rotation to the other body and uses that as a base.
    // To correctly calculate target rotations, we need to memorize that start rotation.
    Quaternion JointRelativeStartRotation;

    public BlockConnection(BlockSocket a, BlockSocket b)
    {
        A = a;
        B = b;
        AddJoint();
    }

    private void AddJoint()
    {
        joint = A.ParentBlock.gameObject.AddComponent<ConfigurableJoint>();
        joint.anchor = A.transform.localPosition;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = B.transform.localPosition;
        joint.configuredInWorldSpace = false;
        joint.enableCollision = true;
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        JointRelativeStartRotation = GetRelativeStartRotation(A.Normal, B.Normal, A.transform.parent, B.transform.parent);
        joint.targetRotation = CalculateTargetRotation(A.Normal, B.Normal, A.transform.parent, B.transform.parent, JointRelativeStartRotation);
        joint.connectedBody = B.GetComponentInParent<Rigidbody>();
    }

    public void RemoveJoint()
    {
        Object.Destroy(joint);
    }

    public void Lock()
    {
        Locked = true;
    }

    public void Unlock()
    {
        Locked = false;
    }

    public bool CloseEnoughToLock(float lockDistance)
    {
        return Vector3.SqrMagnitude(A.transform.position - B.transform.position) <= Mathf.Pow(lockDistance, 2);
    }

    public void UpdateDriveForces(float maxDriveForce, float maxAngularDriveForce)
    {
        Debug.Assert(joint);
        float distance = Vector3.Distance(A.transform.position, B.transform.position);
        float driveForce;
        float angularDriveForce;
        if (Locked)
        {
            driveForce = float.MaxValue;
            angularDriveForce = float.MaxValue;
        }
        else
        {
            driveForce = SheepMathUtils.Map(distance, 0, A.WorldDiameter, maxDriveForce, 0);
            angularDriveForce = SheepMathUtils.Map(distance, 0, A.WorldDiameter, maxAngularDriveForce, 0);
        }
        JointDrive drive = joint.xDrive;
        JointDrive angularDrive = joint.slerpDrive;
        drive.positionSpring = driveForce;
        angularDrive.positionSpring = angularDriveForce;
        joint.xDrive = joint.yDrive = joint.zDrive = drive;
        joint.slerpDrive = angularDrive;
        joint.targetRotation = CalculateTargetRotation(A.Normal, B.Normal, A.transform.parent, B.transform.parent, JointRelativeStartRotation);
    }

    Quaternion GetRelativeStartRotation(Vector3 normal, Vector3 otherNormal, Transform ourTransform, Transform otherTransform)
    {
        Vector3 otherNormalInLocalSpace = -FromLocalToLocal(otherNormal, otherTransform.rotation, ourTransform.rotation);
        Vector3 otherPerpendicularInLocalSpace = FromLocalToLocal(Perpendicular(otherNormal), otherTransform.rotation, ourTransform.rotation);
        var rotationToMatch = Quaternion.LookRotation(otherNormalInLocalSpace, otherPerpendicularInLocalSpace);
        Quaternion fromSocketToBlock = Quaternion.Inverse(Quaternion.LookRotation(normal, Perpendicular(normal)));
        return rotationToMatch * fromSocketToBlock;
    }

    public Quaternion CalculateTargetRotation(Vector3 normal, Vector3 otherNormal, Transform ourTransform, Transform otherTransform, Quaternion jointStartRotation)
    {
        Vector3 otherNormalInLocalSpace = -FromLocalToLocal(otherNormal, otherTransform.rotation, ourTransform.rotation);
        Vector3 otherPerpendicularInLocalSpace = FromLocalToLocal(Perpendicular(otherNormal), otherTransform.rotation, ourTransform.rotation);
        var rotationToMatch = Quaternion.LookRotation(otherNormalInLocalSpace, otherPerpendicularInLocalSpace);

        //Debug.DrawRay(otherTransform.position, ourTransform.rotation * otherNormalInLocalSpace * 4, Color.red);
        //Debug.DrawRay(otherTransform.position, ourTransform.rotation * otherPerpendicularInLocalSpace * 4, Color.green);

        Vector3 perpendicular = Perpendicular(normal);

        //Debug.DrawRay(ourTransform.position, ourTransform.rotation * normal * 4, Color.red);
        //Debug.DrawRay(ourTransform.position, ourTransform.rotation * perpendicular * 4, Color.green);

        float smallestTwistAngle = float.MaxValue;
        Quaternion smallestRotation = Quaternion.identity;

        Quaternion fromSocketToBlock = Quaternion.Inverse(Quaternion.LookRotation(normal, perpendicular));

        int twistSnapAngle = Mathf.Min(A.TwistSnapAngle, B.TwistSnapAngle);
        Debug.Assert(twistSnapAngle >= 1, "TwistSnapAngle must be greater than or equal to 1.");
        Debug.Assert(360 % twistSnapAngle == 0, "TwistSnapAngle must be a divisor of 360.");
        if (twistSnapAngle >= 1)
        {
            for (int i = 0; i < 360 / twistSnapAngle; i++)
            {
                Vector3 snapVector = perpendicular.Rotate(normal, i * twistSnapAngle);
                Quaternion snapQuaternion = Quaternion.LookRotation(normal, snapVector);
                // difference = destination - origin
                Quaternion localRotationDelta = snapQuaternion * Quaternion.Inverse(rotationToMatch);

                float twistAngle = Mathf.Abs(re.GetTwistAngle(localRotationDelta, normal));

                if (twistAngle < smallestTwistAngle)
                {
                    smallestTwistAngle = twistAngle;
                    smallestRotation = snapQuaternion * fromSocketToBlock;
                }
            }
        }

        return smallestRotation * Quaternion.Inverse(jointStartRotation);
    }

    Vector3 Perpendicular(Vector3 targetVector) // TODO: Make generic. Not just cardinal directions.
    {
        return (targetVector == Vector3.up || targetVector == Vector3.down) ? Vector3.forward : Vector3.up;   
    }

    Vector3 ToWorldSpace(Vector3 vector, Quaternion worldToLocal)
    {
        return worldToLocal * vector;
    }

    Vector3 ToLocalSpace(Vector3 vector, Quaternion localToWorld)
    {
        return Quaternion.Inverse(localToWorld) * vector;
    }

    Vector3 FromLocalToLocal(Vector3 vector, Quaternion from, Quaternion to)
    {
        return ToLocalSpace(ToWorldSpace(vector, from), to);
    }
}
