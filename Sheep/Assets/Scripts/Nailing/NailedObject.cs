using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Recoil;

public class NailedObject : MonoBehaviour, ITreePhysics, IProjectHandAnchor, IRespawnListener
{
    public Nail nailParent;
    public ConfigurableJoint joint;
    ReBody reBody;
    public ReBody RecoilBody => reBody;
    public Vector3 localNailPenetrationPosition;
    public event Action onConnectionChange;

    Quaternion nailingRotation;
    bool currentlyBeingNailed = true;
    public bool CurrentlyBeingNailed
    {
        get
        {
            return currentlyBeingNailed;
        }
        set
        {
            nailingRotation = transform.rotation;
            currentlyBeingNailed = value;
        }
    }

    public void Initialize(Nail parent, ConfigurableJoint joint, Rigidbody rig, Vector3 localNailPenetrationPosition)
    {
        nailParent = parent;
        this.joint = joint;
        reBody = new ReBody(rig);
        this.localNailPenetrationPosition = localNailPenetrationPosition;
        CurrentlyBeingNailed = true;
    }

    public void AddTreeAcceleration(Vector3 worldPos, Vector6 acc)
    {
        nailParent.AddTreeAcceleration(worldPos, acc);
    }

    public Vector6 CalculateTreeVelocity(Vector3 worldPos)
    {
        return nailParent.CalculateTreeVelocity(worldPos);
    }

    public void RemoveSelf()
    {
        nailParent = null;
        currentlyBeingNailed = false;
        Destroy(joint);
        var allConnections = GetComponents<NailedObject>();
        foreach (var connection in allConnections)
        {
            connection.onConnectionChange?.Invoke();
        }
        Destroy(this);
    }

    public bool TreePhysicsActive()
    {
        return !currentlyBeingNailed && nailParent != null;
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        if (currentlyBeingNailed && nailParent != null)
        {
            var nailWorldDir = nailParent.NailDirection;
            var localDir = transform.InverseTransformDirection(nailWorldDir);
            ProjectAnchorUtils.ProjectLinear(reBody, reBody.worldCenterOfMass, localDir, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
            vrAngular = Vector3.zero;
            vrVel = Vector3.zero;
            vrRot = nailingRotation;
        }
    }

    public void OnRespawn()
    {
    }

    public void OnDespawn()
    {
        nailParent?.OnNailedObjectDespawn();
    }
}
