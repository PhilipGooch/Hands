using System;
using System.Collections.Generic;
using UnityEngine;

internal class StickyConnection
{
    internal event Action onJointBroken;
    internal event Action onJointCreated;

    internal bool HasSticked => HasAnyJoints();
    internal bool FullySnapped { get; private set; }

    private float accumulatedAwayForce;
    private float mass;

    private bool stickyAllAround;

    private GameObject thisObject;
    //can be null
    private Rigidbody targetRigidbody;

    private HashSet<ContactJoint> toDestroy = new HashSet<ContactJoint>();
    private List<ContactJoint> contacts = new List<ContactJoint>();

    private float breakForce;
    private float breakTorque;
    private bool wasGrabbed;
    private float targetAccumulatedForce;
    private float minimumDistanceBetweenContacts;
    private int increasedBreakforceFramesLeft;

    //maximum number of configurable joints before setting fixed joint
    private const int kConfigurableJointsCount = 2;
    private const int kIncreasedBreakforceFrames = 10;
    private const float kPullAwayDotAccuracy = 0.5f;
    //how much of the maximum breaking will be used
    private const float kIncreasedCollisionForceMultiplier = 0.5f;
    //maximum allowed distance between colliders
    private const float kMaximumCollisionSeparation = 0.05f;
    //tested multiple setups and this can reliably be used as maximum
    private const float kMaxVelChange = 100;
    private float maxCollissionForce => mass * kMaxVelChange / Time.fixedDeltaTime;
    private float increasedBreakForce => maxCollissionForce * kIncreasedCollisionForceMultiplier;

    internal StickyConnection(
        GameObject thisObject,
        GameObject target,
        float mass,
        bool stickyAllAround,
        float breakForce,
        float breakTorque,
        float snappedObjectAcumulatedForceMulti,
        float minimumDistanceBetweenContacts)
    {
        increasedBreakforceFramesLeft = kIncreasedBreakforceFrames;

        this.breakForce = breakForce;
        this.breakTorque = breakTorque;
        this.thisObject = thisObject;
        this.mass = mass;
        this.targetAccumulatedForce = snappedObjectAcumulatedForceMulti;
        this.stickyAllAround = stickyAllAround;
        this.minimumDistanceBetweenContacts = minimumDistanceBetweenContacts;

        targetRigidbody = target.GetComponentInParent<Rigidbody>();
        accumulatedAwayForce = 0;
    }

    internal void AddContact(ContactPoint contactPoint, Vector3 normal)
    {
        if (!FullySnapped)
        {
            //check only those contacts which are close to collider
            if (contactPoint.separation > kMaximumCollisionSeparation)
                return;

            //if the new contact point is close to existing ones, skip it
            foreach (var item in contacts)
            {
                if (Vector3.Distance(contactPoint.point, item.Point.point) < minimumDistanceBetweenContacts)
                    return;
            }

            if (contacts.Count == kConfigurableJointsCount)
            {
                FullySnapped = true;

                foreach (var contact in contacts)
                {
                    if (contact.Joint != null)
                        GameObject.Destroy(contact.Joint);
                }
                contacts.Clear();
                contacts.Add(new ContactJoint(thisObject, targetRigidbody));
                onJointCreated?.Invoke();

                //reset on full connection?
                accumulatedAwayForce = 0;
            }
            else
            {
                contacts.Add(new ContactJoint(thisObject, targetRigidbody, contactPoint, normal));
                onJointCreated?.Invoke();
            }
        }
    }

    private bool HasAnyJoints()
    {
        foreach (var contact in contacts)
        {
            if (contact.HasSticked)
                return true;
        }

        return false;
    }

    private void Break(Joint joint)
    {
        GameObject.Destroy(joint);
        if (!HasSticked)
            onJointBroken?.Invoke();
    }

    private void SetBreakForceAndTorque(float torque, float force)
    {
        foreach (var contact in contacts)
        {
            contact.SetBreakForceAndTorque(torque, force);
        }
    }

    internal void Update(Vector3 worldStickyNormal, bool shouldUnstickOnItsOwn, bool isGrabbed)
    {
        if (wasGrabbed && !isGrabbed)
            increasedBreakforceFramesLeft = kIncreasedBreakforceFrames;

        if (isGrabbed)
        {
            SetBreakForceAndTorque(Mathf.Infinity, Mathf.Infinity);
        }
        else
        {
            if (increasedBreakforceFramesLeft > 0)
                SetBreakForceAndTorque(increasedBreakForce, increasedBreakForce); // could set this to infinity but that would make this stick despite any forces, and thats not realistic at all
            else if (increasedBreakforceFramesLeft == 0)
                SetBreakForceAndTorque(breakTorque, breakForce);  //joints have settled, can lower break force
        }

        if (!FullySnapped || shouldUnstickOnItsOwn || isGrabbed)
        {
            UpdateForceAccumulation(worldStickyNormal);
        }

        DestroyEmptyContacts();

        wasGrabbed = isGrabbed;

        if (increasedBreakforceFramesLeft >= 0)
            increasedBreakforceFramesLeft--;
    }

    private void UpdateForceAccumulation(Vector3 worldStickyNormal)
    {
        foreach (var contact in contacts)
        {
            if (!contact.HasSticked)
                continue;

            if (stickyAllAround || Vector3.Dot(worldStickyNormal, contact.Joint.currentForce) > kPullAwayDotAccuracy)
            {
                //dont need to accumulate insane numbers
                accumulatedAwayForce += contact.Joint.currentForce.magnitude / mass;

                if (accumulatedAwayForce > targetAccumulatedForce)
                {
                    Break(contact.Joint);
                }
            }
        }
    }

    private void DestroyEmptyContacts()
    {
        toDestroy.Clear();

        foreach (var contact in contacts)
        {
            if (!contact.HasSticked)
                toDestroy.Add(contact);
        }

        foreach (var item in toDestroy)
        {
            contacts.Remove(item);
        }
    }

    internal class ContactJoint
    {
        internal Joint Joint { get; }
        internal ContactPoint Point { get; }
        internal bool HasSticked => Joint != null;

        internal ContactJoint(GameObject thisObject, Rigidbody targetRigidbody, ContactPoint contactPoint, Vector3 normal)
        {
            Point = contactPoint;

            var joint = thisObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = targetRigidbody;
            joint.enableCollision = true;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.anchor = thisObject.transform.InverseTransformPoint(contactPoint.point);
            if (normal != Vector3.zero)
                joint.axis = normal;

            this.Joint = joint;
        }

        public ContactJoint(GameObject thisObject, Rigidbody targetRigidbody)
        {
            var joint = thisObject.AddComponent<FixedJoint>();
            joint.connectedBody = targetRigidbody;
            joint.enableCollision = true;

            this.Joint = joint;
        }

        internal void SetBreakForceAndTorque(float torque, float force)
        {
            if (HasSticked)
            {
                Joint.breakForce = force;
                Joint.breakTorque = torque;
            }
        }
    }
}
