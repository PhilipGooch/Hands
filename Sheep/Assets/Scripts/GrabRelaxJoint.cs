using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGrabNotifications { 
    void OnGrab(Hand hand, bool firstGrab);
    void OnRelease(Hand hand, bool firstGrab);
}

public interface ITreePhysics
{
    bool TreePhysicsActive();
    Vector6 CalculateTreeVelocity(Vector3 worldPos);
    void AddTreeAcceleration(Vector3 worldPos, Vector6 acc);
}

public class GrabRelaxJoint : MonoBehaviour, IGrabNotifications, ITreePhysics
{
    Rigidbody thisBody;
    ReBody thisReBody;
    public Rigidbody otherBody; // the body to attach to (null - world)

    public bool requireOtherBodyGrab = false; // if true, joint will only be relaxed when thing AND other bodies are held (needs two hand interaction)
    bool authored = true;
    GrabRelaxJoint parent;
    List<GrabRelaxJoint> children = new List<GrabRelaxJoint>();

    FixedJoint joint; // joint attaching to other body when not held

    private void Start()
    {
        thisBody = GetComponent<Rigidbody>();
        thisReBody = new ReBody(thisBody);
        if (!authored) return;

        Attach();

        if(otherBody!=null && requireOtherBodyGrab) // bind to parent sensor
        {
            parent = otherBody?.gameObject.GetComponent<GrabRelaxJoint>();
            if (parent == null) // if not present create a runtime instance
            {
                parent = otherBody.gameObject.AddComponent<GrabRelaxJoint>();
                parent.authored = false;
            }
            parent.children.Add(this);
        }

    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (!firstGrab) return;

        // authored, not parented
        if (authored && parent == null)
            Detach();
        // if parent exists, it must be already grabbed by other hand to relax
        if (parent != null && hand.otherHand.attachedBody == parent.thisBody)
            Detach();

        // check if children are being held
        foreach (var c in children)
            if (hand.otherHand.attachedBody == c.thisBody)
                c.Detach();
    }


    public void OnRelease(Hand hand, bool lastRelease)
    {
        if (!lastRelease) return;

        // authored, not parented
        if (authored && parent == null)
            Attach();
        // if parent exists, and held by other hand
        if (parent != null && hand.otherHand.attachedBody == parent.thisBody)
            Attach();

        // check if children are being held
        foreach (var c in children)
            if (hand.otherHand.attachedBody == c.thisBody)
                c.Attach();

    }

    void Attach()
    {
        joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = otherBody;

    }
    private void Detach()
    {
        Destroy(joint);
    }


    public void CalculateTreeMoments(Vector3 worldPos, out Inertia I, out Vector6 M)
    {
        I = Inertia.FromRigidAtPoint(thisReBody,worldPos);
        var v = new Vector6(thisReBody.angularVelocity, thisReBody.GetPointVelocity(worldPos));
        M = I * v;

        foreach (var c in children)
            if (c.joint!=null)
            {
                c.CalculateTreeMoments(worldPos, out var i, out var m);
                I += i;
                M += m;
            }
                
    }

    public void AddTreeAcceleration(Vector3 worldPos, Vector6 acc)
    {
        var p = this;
        while (p.joint != null && p.parent != null)
            p = p.parent;

        p.AddTreeAccelerationRecursive(worldPos, acc);
    }

    private void AddTreeAccelerationRecursive(Vector3 worldPos, Vector6 acc)
    {
        Dynamics.AddAccelerationAtPosition(thisReBody, acc, worldPos);
        foreach (var c in children)
            if (c.joint != null)
                c.AddTreeAccelerationRecursive(worldPos, acc);
    }

    public Vector6 CalculateTreeVelocity(Vector3 worldPos)
    {
        var p = this;
        while (p.joint != null && p.parent != null)
            p = p.parent;
        p.CalculateTreeMoments(worldPos,out var i, out var m);
        var r = i.centerOfMass / i.mass;
        p.CalculateTreeMoments(worldPos+r, out i, out m);
        return new PluckerTranslate(-r).TransformVelocity(i.inverse * m);
    }

    public bool TreePhysicsActive()
    {
        return true;
    }
}
