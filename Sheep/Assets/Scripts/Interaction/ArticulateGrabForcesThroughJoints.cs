using Recoil;
using System.Collections.Generic;
using UnityEngine;

public class ArticulateGrabForcesThroughJoints : MonoBehaviour, ITreePhysics
{
    new Rigidbody rigidbody;
    ReBody reBody;

    public List<Rigidbody> connectedBodies = new List<Rigidbody>();
    List<ReBody> connectedReBodies = new List<ReBody>();
    SimpleTreePhysics simpleTreePhysics;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        reBody = new ReBody(rigidbody);
        connectedReBodies.Add(reBody);
        for(int i = 0; i < connectedBodies.Count; i++)
        {
            connectedReBodies.Add(new ReBody(connectedBodies[i]));
        }
        simpleTreePhysics = new SimpleTreePhysics(connectedReBodies);
    }

    public void AddTreeAcceleration(Vector3 worldPos, Vector6 acc)
    {
        simpleTreePhysics.AddTreeAcceleration(worldPos, acc);
    }

    public Vector6 CalculateTreeVelocity(Vector3 worldPos)
    {
        return simpleTreePhysics.CalculateTreeVelocity(worldPos);
    }

    public bool TreePhysicsActive()
    {
        return connectedBodies != null && rigidbody != null && connectedBodies.Count > 0;
    }
}
