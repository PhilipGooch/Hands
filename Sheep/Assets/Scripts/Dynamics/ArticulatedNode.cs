using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArticulatedNode : MonoBehaviour, IGrabNotifications, ITreePhysics
{

    Rigidbody body;
    ReBody reBody;
    public bool affectParent = true;
    public ArticulatedNode parent;
    List<ArticulatedNode> children = new List<ArticulatedNode>();
    private void OnEnable()
    {
        body = GetComponent<Rigidbody>();
        if (parent != null)
            parent.children.Add(this);
    }
    private void OnDisable()
    {
        if (parent != null)
            parent.children.Remove(this);
    }

    void Start()
    {
        reBody = new ReBody(body);
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        //throw new System.NotImplementedException();
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        //throw new System.NotImplementedException();
    }

    public Vector6 CalculateTreeVelocity(Vector3 worldPos)
    {
        var i = Inertia.zero;
        var m = Vector6.zero;
        AppendTreeMoments(worldPos, ref i, ref m, null);
        var r = i.centerOfMass / i.mass;
        i = Inertia.zero;
        m = Vector6.zero;
        AppendTreeMoments(worldPos + r, ref i, ref m, null);
        return new PluckerTranslate(-r).TransformVelocity(i.inverse * m);

        //return new Vector6(body.angularVelocity, body.GetPointVelocity(worldPos));
    }

    void AppendTreeMoments(Vector3 worldPos, ref Inertia I, ref Vector6 M, ArticulatedNode incomingNode)
    {
        var Iown = Inertia.FromRigidAtPoint(reBody, worldPos);
        var v = new Vector6(reBody.angularVelocity, reBody.GetPointVelocity(worldPos));
        M += Iown * v;
        I += Iown;

        if (affectParent && parent != null && parent != incomingNode)
            parent.AppendTreeMoments(worldPos, ref I, ref M, this);
        for (int i = 0; i < children.Count; i++)
            if (children[i] != incomingNode)
                children[i].AppendTreeMoments(worldPos, ref I, ref M, this);

    }

    void ITreePhysics.AddTreeAcceleration(Vector3 worldPos, Vector6 acc)
    {
        AddTreeAccelerationRecurse(worldPos, acc, null);
    }
    void AddTreeAccelerationRecurse(Vector3 worldPos, Vector6 acc, ArticulatedNode incomingNode)
    {
        reBody.AddForceAtPosition(acc.linear, worldPos, ForceMode.Acceleration);
        reBody.AddAngularAccelerationAtPosition(acc.angular, worldPos);

        if (affectParent && parent != null && parent != incomingNode)
            parent.AddTreeAccelerationRecurse(worldPos,acc, this);
        for (int i = 0; i < children.Count; i++)
            if(children[i]!=incomingNode)
                children[i].AddTreeAccelerationRecurse(worldPos, acc, this);
    }

    public bool TreePhysicsActive()
    {
        return true;
    }
}
