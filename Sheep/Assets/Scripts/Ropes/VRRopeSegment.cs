using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Recoil;
using NBG.XPBDRope;

public class VRRopeSegment : MonoBehaviour, IGrabNotifications, IPositionBasedDynamics
{
    [SerializeField]
    [HideInInspector]
    RopeSegment segment;
    [SerializeField]
    [HideInInspector]
    Rigidbody body;
    ReBody reBody;

    const float maxGrabLinearVelocity = 100;

    Vector3 manipulationAnchor;
    Vector3 manipulationPos;
    Quaternion manipulationRot;
    Hand grabbingHand;

    SimpleTreePhysics treePhysics;
    List<ReBody> bodies = new List<ReBody>();

    public void SetupVRRopeSegment()
    {
        segment = GetComponent<RopeSegment>();
        body = GetComponent<Rigidbody>();
    }

    void Start()
    {
        reBody = new ReBody(body);
        var rope = GetComponentInParent<Rope>();
        foreach(var bone in rope.Bones)
        {
            bodies.Add(new ReBody(bone.body));
        }

        if (rope.BodyEndIsAttachedTo)
            bodies.Add(new ReBody(rope.BodyEndIsAttachedTo));
        if (rope.BodyStartIsAttachedTo)
            bodies.Add(new ReBody(rope.BodyStartIsAttachedTo));

        treePhysics = new SimpleTreePhysics(bodies);
    }   

    public void ApplyPosition(Vector3 pos, Quaternion rot, Vector3 anchor)
    {
        manipulationPos = pos;
        manipulationRot = rot;
        manipulationAnchor = anchor;
    }

    void FixedUpdate()
    {
        if (grabbingHand)
        {
            var relativeAnchor = (Vector3)segment.RecoilBody.TransformDirection(manipulationAnchor);
            var targetPos = manipulationPos - relativeAnchor;

            ref var bod = ref segment.RecoilBody;
            var wantedPos = targetPos;
            var currentPos = (Vector3)bod.x.pos;
            var linDelta = wantedPos - currentPos;
            var linear = linDelta / Time.fixedDeltaTime;
            linear = linear.Clamp(maxGrabLinearVelocity);

            var angularDelta = manipulationRot * Quaternion.Inverse(bod.x.rot);
            angularDelta.ToAngleAxis(out var angle, out var axis);
            var angular = axis * angle * Mathf.Deg2Rad / Time.fixedDeltaTime;
            bod.x = new RigidTransform(manipulationRot, currentPos);
            var v = bod.v;
            v.linear =  linear;
            v.angular = angular;
            bod.v = v;

            // Apply light damping to all rope bodies to make ropes more stable
            for(int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b == reBody)
                    continue;

                b.velocity *= 0.95f;
            }
        }
    }

    void IGrabNotifications.OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbingHand = hand;
        }
    }

    void IGrabNotifications.OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbingHand = null;
        }
    }
}
