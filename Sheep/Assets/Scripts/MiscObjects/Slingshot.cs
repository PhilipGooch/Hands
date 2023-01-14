using Recoil;
using System.Collections;
using UnityEngine;

public class Slingshot : MonoBehaviour, IGrabNotifications
{
    [SerializeField]
    float maxPull = 3f;
    [SerializeField]
    float lengthAtEase = 0.5f;
    [SerializeField]
    float pullSpring = 1000;
    [SerializeField]
    float rotationSpring = 100;
    [SerializeField]
    GrabEventsSender grabEventsSender;
    [SerializeField]
    float maxFiringForce = 1000;
    [SerializeField]
    float minFiringForce = 10;
    [SerializeField]
    Transform ammoSlot;

    ConfigurableJoint[] joints;
    new Rigidbody rigidbody;
    new Collider collider;
    Rigidbody attachedBody;
    ReBody attachedReBody;
    Quaternion attachedBodyRotation;

    bool grabbed = false;
    bool baseGrabbed = false;

    void Start()
    {
        joints = GetComponents<ConfigurableJoint>();
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponentInChildren<Collider>();
        initialLocalRotation = transform.localRotation;

        grabEventsSender.onGrab += BaseGrabbed;
        grabEventsSender.onRelease += BaseReleased;
    }

    void BaseGrabbed(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            baseGrabbed = true;
        }
    }

    void BaseReleased(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            baseGrabbed = false;
        }
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbed = false;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbed = false;
            DetachBody();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var rotation = CalculateTargetRotation();
        foreach (var joint in joints)
        {
            UpdateRotation(joint, rotation);
            UpdatePull(joint);
        }
        UpdateAttachedBody();
    }

    Quaternion initialLocalRotation;
    Quaternion CalculateTargetRotation()
    {
        var center = GetSlingCenter();

        var mainJoint = joints[0];
        var connectedRig = mainJoint.connectedBody;
        //var forward = Vector3.Cross(mainJoint.axis, mainJoint.secondaryAxis).normalized;
        //var up = Vector3.Cross(forward, mainJoint.axis).normalized;
        //var jointToLocalSpace = Quaternion.LookRotation(forward, up);

        var targetWorldRotation = Quaternion.LookRotation(center - transform.position, connectedRig.transform.up);
        var targetLocalRotation = Quaternion.Inverse(connectedRig.transform.rotation) * targetWorldRotation;

        var finalRot = Quaternion.Inverse(targetLocalRotation);
        return finalRot;
    }

    Vector3 GetSlingCenter()
    {
        var center = Vector3.zero;
        foreach (var joint in joints)
        {
            center += joint.connectedBody.TransformPoint(joint.connectedAnchor);
        }
        center /= joints.Length;
        return center;
    }

    void UpdateRotation(ConfigurableJoint joint, Quaternion targetRotation)
    {
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;
        joint.targetRotation = targetRotation;
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        var slerpDrive = joint.slerpDrive;
        slerpDrive.positionSpring = rotationSpring;
        joint.slerpDrive = slerpDrive;
    }

    void UpdatePull(ConfigurableJoint joint)
    {
        var limitSpring = joint.linearLimitSpring;
        limitSpring.spring = pullSpring;
        limitSpring.damper = pullSpring / 100;
        joint.linearLimitSpring = limitSpring;

        var limit = joint.linearLimit;
        if (grabbed && baseGrabbed)
        {
            limit.limit = maxPull;
        }
        else
        {
            limit.limit = lengthAtEase;
        }
        joint.linearLimit = limit;
    }

    void OnCollisionEnter(Collision collision)
    {
        var targetBody = collision.rigidbody;
        if (targetBody != null)
        {
            if (targetBody == joints[0].connectedBody)
            {
                return;
            }
            var handThatIsGrabbing = Player.Instance.GetHandThatIsGrabbingBody(targetBody);
            if (handThatIsGrabbing != null)
            {
                handThatIsGrabbing.InterceptGrab(rigidbody, transform.position);
                AttachBody(targetBody);
            }
        }
    }

    void AttachBody(Rigidbody body)
    {
        attachedBody = body;
        attachedReBody = new ReBody(body);
        attachedBodyRotation = Quaternion.Inverse(transform.rotation) * attachedReBody.rotation;
        SetIgnoreCollision(body.GetComponentsInChildren<Collider>(true), true);
    }

    void DetachBody()
    {
        if (attachedBody != null)
        {
            StartCoroutine(ReenableCollisionForBodyDelayed(attachedBody));
            ApplySlingshotForce(attachedReBody);
            attachedBody = null;
            attachedReBody = ReBody.Empty();
            attachedBodyRotation = Quaternion.identity;
        }
    }

    IEnumerator ReenableCollisionForBodyDelayed(Rigidbody body)
    {
        const int framesBeforeCollisionEnabled = 30;
        for (int i = 0; i < framesBeforeCollisionEnabled; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        SetIgnoreCollision(body.GetComponentsInChildren<Collider>(true), false);
    }

    void ApplySlingshotForce(ReBody body)
    {
        var pullAmount = GetPullAmount();
        var forceAmount = Mathf.Lerp(minFiringForce, maxFiringForce, pullAmount);

        var forceDirection = (GetSlingCenter() - transform.position).normalized;
        body.AddForce(forceDirection * forceAmount, ForceMode.Impulse);
    }

    float GetPullAmount()
    {
        var pullDistance = 0f;
        foreach (var joint in joints)
        {
            var distance = (joint.connectedBody.TransformPoint(joint.connectedAnchor) - transform.position).magnitude;
            if (distance > pullDistance)
            {
                pullDistance = distance;
            }
        }

        return Mathf.InverseLerp(0f, maxPull, pullDistance);
    }

    void SetIgnoreCollision(Collider[] colliders, bool ignore)
    {
        foreach (var col in colliders)
        {
            Physics.IgnoreCollision(collider, col, ignore);
        }
    }

    void UpdateAttachedBody()
    {
        if (attachedBody != null)
        {
            attachedReBody.position = ammoSlot.position;
            attachedReBody.rotation = ammoSlot.rotation * attachedBodyRotation;
            attachedReBody.velocity = Vector3.zero;
            attachedReBody.angularVelocity = Vector3.zero;
        }
    }

    void OnDestroy()
    {
        grabEventsSender.onGrab -= BaseGrabbed;
        grabEventsSender.onRelease -= BaseReleased;
    }
}
