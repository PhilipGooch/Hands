//#define AXE_TEST_DEFINED

using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Axe : Nail
{
    [SerializeField]
    float velocityToPenetrate = 5f;
    [SerializeField]
    float jointStrength = float.PositiveInfinity;

    ConfigurableJoint currentJoint = null;
    Vector3 penetrationPosition = Vector3.zero;
    Rigidbody connectedRigidbody = null;
    ReBody connectedReBody;
    GameObject connectedGameObject = null;
    Vector3 localPenetrationPosition = Vector3.zero;
    int averageVelocityFrames = 5;
    Queue<Vector3> velocities = new Queue<Vector3>();

    protected override void Start()
    {
        base.Start();
        partialPenetrationDistance = 0.1f;
#if AXE_TEST_DEFINED
        StartCoroutine(Test());
#endif
    }

#if AXE_TEST_DEFINED
    [SerializeField]
    bool reset;
    [SerializeField]
    bool testGrabbedState;
    [SerializeField]
    Vector3 addForce;
    [SerializeField]
    Vector3 addTorque;
    [SerializeField]
    Vector3 originalPos;
    [SerializeField]
    Quaternion originalRot;

    IEnumerator Test()
    {

        originalPos = transform.position;
        originalRot = transform.rotation;

        while (true)
        {
            // SetGrabState(testGrabbedState);
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            //  yield return new WaitForSeconds(1);
            rigidbody.constraints = RigidbodyConstraints.None;

            rigidbody.AddTorque(addTorque, ForceMode.VelocityChange);
            rigidbody.AddForce(addForce, ForceMode.VelocityChange);

            yield return new WaitUntil(() => reset == true);
            reset = false;

            int c = connectedBodies.Count - 1;

            //remove connections
            for (int i = c; i >= 0; i--)
            {
                if (connectedBodies.Count <= i)
                    continue;

                RemoveJointForBody(connectedBodies[i].gameObject);

            }

            transform.position = originalPos;
            transform.rotation = originalRot;
        }
    }
#endif
    protected override void OnObjectPenetrated(GameObject target, Vector3 penetrationPosition)
    {
        if (currentJoint == null)
        {
            AddJointForBody(target, penetrationPosition);
            RegenerateConnectedBodies();
        }
    }

    protected override void OnObjectDepenetrated(GameObject target)
    {
        if (target == connectedGameObject)
        {
            currentJoint = null;
            connectedRigidbody = null;
            connectedReBody = ReBody.Empty();
            connectedGameObject = null;
            base.OnObjectDepenetrated(target);
        }
    }

    protected override void OnImpale()
    {
        SetNailedStatus(false);
    }
    protected override void OnUnimpale()
    {
        SetNailedStatus(true);
    }

    void SetNailedStatus(bool status)
    {
        foreach (var obj in nailedObjects)
        {
            obj.CurrentlyBeingNailed = status;
        }
    }
    public GameObject pref;
    void AddJointForBody(GameObject obj, Vector3 penetrationPosition)
    {
        var otherRig = obj.GetComponentInParent<Rigidbody>();
        if (rigidbody == otherRig) // avoid connecting nail to itself
        {
            return;
        }

        connectedReBody = new ReBody(otherRig);

        var joint = rigidbody.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = otherRig;
        joint.autoConfigureConnectedAnchor = false;

        var connectedAnchorPos = penetrationPosition;
        if (otherRig != null)
        {
            // rigidbody.InverseTransformPoint ignores scale
            connectedAnchorPos = connectedReBody.InverseTransformPoint(connectedAnchorPos);
        }

        joint.connectedAnchor = connectedAnchorPos;
        joint.axis = impaleDirection;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.enableCollision = true;

        var linearLimit = joint.linearLimit;
        // Avoid nail reaching its limit when being pulled out and getting stuck
        linearLimit.limit = nailLength - 0.1f;
        joint.linearLimit = linearLimit;

        joint.anchor = reBody.InverseTransformPoint(NailTip);

        currentJoint = joint;
        this.penetrationPosition = penetrationPosition;
        connectedRigidbody = otherRig;
        connectedGameObject = obj;
        localPenetrationPosition = connectedAnchorPos;

        AddNailedObject(obj, joint, otherRig, connectedAnchorPos);
    }


    private void OnJointBreak(float breakForce)
    {
        if (currentJoint != null && currentJoint.currentForce.sqrMagnitude > jointStrength * jointStrength)
        {
            // Our joint broke! Reset to a joint-free state.
            RemoveAllInsideObjects();
        }
    }

    bool VelocityEnoughForPenetration()
    {
        return reBody.GetPointVelocity(nailCollider.bounds.center).sqrMagnitude > velocityToPenetrate * velocityToPenetrate;
    }

    bool IsMovingTowardsPoint(Vector3 point)
    {
        var movementDirection = GetAverageVelocity().normalized;
        var worldImpaleDirection = reBody.TransformDirection(impaleDirection);
        var dot = Vector3.Dot(movementDirection, worldImpaleDirection);
        return dot > 0.1f;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        velocities.Enqueue(reBody.velocity);
        if (velocities.Count > averageVelocityFrames)
        {
            velocities.Dequeue();
        }

        if (currentJoint != null)
        {
            UpdateJointBreakability();

            if (!VelocityEnoughForPenetration())
            {
                if (isGrabbed)
                {
                    currentJoint.xMotion = ConfigurableJointMotion.Limited;
                    currentJoint.connectedAnchor = localPenetrationPosition;
                }
                else if (currentJoint.xMotion != ConfigurableJointMotion.Locked)
                {
                    var connectedAnchorPos = reBody.TransformPoint(currentJoint.anchor);
                    if (connectedRigidbody != null)
                    {
                        connectedAnchorPos = connectedReBody.InverseTransformPoint(connectedAnchorPos);
                    }

                    currentJoint.connectedAnchor = connectedAnchorPos;
                    currentJoint.xMotion = ConfigurableJointMotion.Locked;
                }
            }
        }
    }



    void UpdateJointBreakability()
    {
        if (currentJoint != null)
        {
            var unbreakable = (GetAverageVelocity().sqrMagnitude > 0.0001f || isGrabbed);
            var targetForce = unbreakable ? float.PositiveInfinity : jointStrength;
            currentJoint.breakForce = targetForce;
            currentJoint.breakTorque = targetForce;
        }
    }

    Vector3 GetAverageVelocity()
    {
        if (velocities.Count > 0)
        {
            var sum = Vector3.zero;
            foreach (var velocity in velocities)
            {
                sum += velocity;
            }
            return sum / velocities.Count;
        }
        else
        {
            return reBody.velocity;
        }
    }

    protected override bool CanPenetrate(Vector3 position)
    {
        if (!initialized)
        {
            return base.CanPenetrate(position);
        }
        return base.CanPenetrate(position) && VelocityEnoughForPenetration() && IsMovingTowardsPoint(position);
    }

    protected override bool ShouldProject()
    {
        return false;
    }
}
