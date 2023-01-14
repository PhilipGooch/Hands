using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Recoil;
using NBG.Entities;
using NBG.Core.GameSystems;

public class ReBodyTests : BaseRecoilPlaymodeTest
{
    static ForceMode[] forceModes =
    {
        ForceMode.Acceleration,
        ForceMode.Force,
        ForceMode.Impulse,
        ForceMode.VelocityChange
    };

    [UnityTest]
    public IEnumerator AddForceWorksExactlyLikeUnity([ValueSource("forceModes")] ForceMode mode)
    {
        (var reBody, var rig) = SetupTestCubes();
        yield return ApplyForce(reBody, rig, GetForceForTest(mode), mode, 30);
        AssertPositionsAreEqual(reBody, rig);
        AssertRotationsAreEqual(reBody, rig);
    }

    // Seems like there are some inconsistencies and unity produces 6 times larger angular velocity values than anticipated.
    // CASE NUMBER: UUM-7401
    [UnityTest]
    [Ignore("Unity angular velocity values appear to be 6 times larger than anticipated")]
    public IEnumerator AddForceAtPositionWorksExactlyLikeUnity([ValueSource("forceModes")] ForceMode mode)
    {
        (var reBody, var rig) = SetupTestCubes();
        yield return ApplyForceAtPosition(reBody, rig, GetForceForTest(mode), Vector3.right, mode, 30);
        AssertPositionsAreEqual(reBody, rig);
        AssertRotationsAreEqual(reBody, rig);
    }

    [UnityTest]
    public IEnumerator AddTorqueWorksExactlyLikeUnity([ValueSource("forceModes")] ForceMode mode)
    {
        (var reBody, var rig) = SetupTestCubes();
        yield return ApplyTorque(reBody, rig, GetTorqueForTest(mode), mode, 30);
        AssertPositionsAreEqual(reBody, rig);
        AssertRotationsAreEqual(reBody, rig);
    }

    Vector3 GetForceForTest(ForceMode mode)
    {
        // Use larger force values for slower force modes
        if (mode == ForceMode.Force || mode == ForceMode.Acceleration)
            return Vector3.up;
        return Vector3.up * 0.1f;
    }

    Vector3 GetTorqueForTest(ForceMode mode)
    {
        // Use larger force values for slower force modes
        if (mode == ForceMode.Force || mode == ForceMode.Acceleration)
            return Vector3.up * 20f;
        return Vector3.up * 0.1f;
    }

    void AssertPositionsAreEqual(ReBody body, Rigidbody rig)
    {
        var diff = body.position - rig.position;
        Assert.Less(diff.magnitude, 0.01f, $"Positions did not match! Expected: {rig.position} but was {body.position}");
    }

    void AssertRotationsAreEqual(ReBody body, Rigidbody rig)
    {
        var diff = (body.rotation * Quaternion.Inverse(rig.rotation)).eulerAngles;
        var angleSum = Mathf.Abs(Mathf.DeltaAngle(diff.x, 0)) + Mathf.Abs(Mathf.DeltaAngle(diff.y, 0)) + Mathf.Abs(Mathf.DeltaAngle(diff.z, 0));
        Assert.Less(angleSum, 0.5f, $"Rotations did not match! Expected: {rig.rotation.eulerAngles} but was {body.rotation.eulerAngles}");
    }

    IEnumerator ApplyForce(ReBody reBody, Rigidbody rig, Vector3 force, ForceMode mode, int frames)
    {
        for(int i = 0; i < frames; i++)
        {
            yield return new WaitForFixedUpdate();
            reBody.AddForce(force, mode);
            rig.AddForce(force, mode);
        }
    }

    IEnumerator ApplyForceAtPosition(ReBody reBody, Rigidbody rig, Vector3 force, Vector3 position, ForceMode mode, int frames)
    {
        for(int i = 0; i < frames; i++)
        {
            yield return new WaitForFixedUpdate();
            reBody.AddForceAtPosition(force, position, mode);
            rig.AddForceAtPosition(force, position, mode);
        }
    }

    IEnumerator ApplyTorque(ReBody reBody, Rigidbody rig, Vector3 force, ForceMode mode, int frames)
    {
        for(int i = 0; i < frames; i++)
        {
            yield return new WaitForFixedUpdate();
            reBody.AddTorque(force, mode);
            rig.AddTorque(force, mode);
        }
    }

    Rigidbody SetupBody()
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.parent = parent;
        obj.transform.position = Vector3.zero;
        var rig = obj.AddComponent<Rigidbody>();
        rig.useGravity = false;
        rig.mass = 10f;
        return rig;
    }

    (ReBody, Rigidbody) SetupTestCubes()
    {
        var first = SetupBody();
        var second = SetupBody();
        Physics.IgnoreCollision(first.GetComponent<Collider>(), second.GetComponent<Collider>());
        RigidbodyRegistration.RegisterHierarchy(first.gameObject);
        var reBody = new ReBody(first);
        return (reBody, second);
    }
}
