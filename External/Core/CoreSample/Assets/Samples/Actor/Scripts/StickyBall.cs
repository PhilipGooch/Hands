using NBG.Actor;
using NBG.Core;
using Noodles;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyBall : MonoBehaviour, IManagedBehaviour
{
    BodyIdTriggerProximityTracker bodyCollisions = new BodyIdTriggerProximityTracker();

    private ActorSystem.IActor myActor;

    private List<Joint> jointMemory = new List<Joint>();

    void IManagedBehaviour.OnLevelLoaded()
    {
        myActor = GetComponent<ActorSystem.IActor>();
    }

    void IManagedBehaviour.OnAfterLevelLoaded() { }

    private bool UntrackedCollider(Collider collider)
    {
        int exclusions = (int)NoodleLayers.Player + (int)NoodleLayers.Ball;
        int layerMask = 1 << collider.gameObject.layer;


        return collider.attachedRigidbody == null || (exclusions & layerMask) > 0;
    }

    void OnTriggerEnter(Collider collider)
    {
        if (UntrackedCollider(collider))
            return;

        int bodyID = bodyCollisions.OnTriggerEnter(collider);
        if (bodyID == World.environmentId)
            return;

        Rigidbody connectTarget = ManagedWorld.main.GetRigidbody(bodyID);

        bool createNewJoint = true;
        for (int i = jointMemory.Count - 1; i >= 0; i--)
        {
            if (jointMemory[i] == null)
                jointMemory.RemoveAt(i);
            if (jointMemory[i].connectedBody == connectTarget)
                createNewJoint = false;
        }

        if (createNewJoint)
        {
            Joint joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = connectTarget;
            ActorSystem.JointModule.RegisterDynamicJoint(joint, myActor);
        }
    }

    public void OnTriggerExit(Collider collider)
    {
        if (UntrackedCollider(collider))
            return;

        bodyCollisions.OnTriggerLeave(collider);
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {

    }
}
