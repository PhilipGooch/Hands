#define GRABJOINT2

using NBG.Core;
using NBG.Entities;
using Recoil;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{    // structure describing how and object should be grabbed (how it's locked to pivot rotation and how position is fixeded to hands)
    public enum GrabJointCollision
    {
        IgnoreLower, //default if no collision with lower arm or fist
        Ignore, // ignore upper arm as well
        Collide // just fiast is ignored
    }
    public class NoodleHand : MonoBehaviour
    {
        [NonSerialized] NoodleHand other;
        [NonSerialized] public float3 palmAnchor;
        [NonSerialized] public float3 palmDirection;
        [NonSerialized] float handRadius;
        internal Rigidbody handBody;
        internal int handId;
        public float3 handAnchor; // in recoil space
        public float3 worldHandAnchor => World.main.TransformPoint(handId, handAnchor);

        CapsuleCollider upperarmCollider;
        CapsuleCollider forearmCollider;
        public SphereCollider handCollider;
        //public bool grabOnCollision;
        public PhysicMaterial normalMaterial;
        public PhysicMaterial slipperyMaterial;
        // current grabbed state 
        public HandState grabState;
        public Rigidbody grabbedBody;
        public Collider grabbedCollider;

        NoodleHandPullJoint joint1 = new NoodleHandPullJoint();
        NoodleHandPullJoint joint2 = new NoodleHandPullJoint();

        // grab filtering
        Collider grabTargetCollider;
        float3 grabNormalDir;
        float grabNormalThreshold;
        float3 grabTargetPosition;
        [NonSerialized]
        public LayerMask collisionLayers = -1;
        [NonSerialized]
        public LayerMask targetLayers = -1;

        [NonSerialized] public int entityId;
        [NonSerialized] public bool isLeft;

        float blockGrab = 0;

        NoodleArmDimensions dimensions;
        int chestId;
        float3 chestAnchor;
        Entity entity;
        public float3 GetHandAnchor()
        {
            var handBody = GetComponent<Rigidbody>();
            float3 localHandPos = handBody.InverseTransformPoint(handCollider.transform.position);
            return localHandPos - (float3)handBody.centerOfMass;
        }

        public void OnCreate(Entity entity, NoodleArmDimensions dimensions, NoodleHand other)
        {
            this.other = other;
            this.entity = entity;
            this.dimensions = dimensions;
            handCollider = GetComponentInChildren<SphereCollider>();
            forearmCollider = GetComponentInChildren<CapsuleCollider>();
            upperarmCollider = transform.parent.GetComponentInChildren<CapsuleCollider>();
            normalMaterial = handCollider.sharedMaterial;

            handBody = GetComponent<Rigidbody>();
            handId = ManagedWorld.main.FindBody(handBody);

            float3 localHandPos = handBody.InverseTransformPoint(handCollider.transform.position);
            handAnchor = localHandPos - (float3)handBody.centerOfMass;
            var palm = transform.Find("Palm");
            if (palm == null)
            {
                palmAnchor = handBody.InverseTransformPoint(handCollider.transform.TransformPoint(new Vector3(0, 0.02f, -.05f))) - handBody.centerOfMass; //new Vector3(-.02f,0,.05f)));
                palmDirection = handBody.InverseTransformDirection(handCollider.transform.TransformDirection(-Vector3.forward));
            }
            else
            {
                palmAnchor = handBody.InverseTransformPoint(palm.position) - handBody.centerOfMass;
                palmDirection = handBody.InverseTransformDirection(palm.forward);

            }
            handRadius = handCollider.radius;

            ConfigurableJoint elbowJoint = GetComponent<ConfigurableJoint>();
            var upperArm = elbowJoint.connectedBody;
            ConfigurableJoint shoulderJoint = upperArm.GetComponent<ConfigurableJoint>();
            var chestBody = shoulderJoint.connectedBody;
            chestId = ManagedWorld.main.FindBody(chestBody);
            chestAnchor = shoulderJoint.connectedAnchor - chestBody.centerOfMass;
        }
        public void OnCollisionEnter(Collision collision)
        {
            OnCollisionStay(collision);
        }

        public void OnCollisionStay(Collision collision)
        {
            if (grabState != HandState.Grab) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                if (contact.thisCollider == handCollider)
                {
                    var hitCollider = contact.otherCollider;
                    var hitPos = contact.point - contact.normal * contact.separation;
                    //var handPos = handCollider.transform.position;// HandWorldPos();
                    var handPos = World.main.TransformPoint(handId, handAnchor);
                    //ALT: handPos = worldHandAnchor;
                    HandleCollision(hitCollider, hitPos, handPos, float3.zero);// collision.GetImpulse());
                }
            }
        }

        Dictionary<Rigidbody, ICustomGrabHandler> customGrabHandlers = new Dictionary<Rigidbody, ICustomGrabHandler>();
        public void Grab(HandTargetSubject subject, HandTargetGrip target, in Aim aim)
        {
            if (subject.body != null && subject.body.TryGetComponent<ICustomGrabHandler>(out var customGrabHandler))
            {
                float3 worldPos = target.worldAnchor;
                customGrabHandler.OnGrab(worldPos, out Rigidbody body);
                target = HandTargetGrip.FromWorldHandPos(worldPos, worldPos);
                subject = HandTargetSubject.FromBody(ManagedWorld.main.RegisterBody(body));
                customGrabHandlers.Add(body, customGrabHandler);
            }

            if (blockGrab > 0) return;
            if (subject.collider != null && subject.collider.transform.root == transform.root) return;// don't grab self
                                                                                                      //if (blockGrab > 0 || grabJoint != null || player.state == HumanState.Dead || player.state == HumanState.FreeFall) return;
            grabState = HandState.Hold;
            grabbedCollider = subject.collider;
            grabbedBody = subject.body;

            // convert to local coordinates
            var handAnchor = World.main.InverseTransformDirection(handId, target.relativeHandAnchor) + this.handAnchor;// World.main.InverseTransformPoint(handId, handPos+relativeHandAnchor);// transform.InverseTransformPoint(handPos);
            var targetAnchor = World.main.InverseTransformPoint(subject.bodyId, target.worldAnchor);
            var grabInfo = new GrabJointInfo(subject.bodyId, target.gripId, targetAnchor, handId, handAnchor, target.rotationToPivot);

            ref var carryData = ref EntityStore.GetComponentData<CarryData>(entity);
            Carry.Grab(grabInfo, ref carryData, isLeft, aim); //TODO: carry should accept recoil anchors


            joint1.Create(handId, handAnchor, subject.bodyId, targetAnchor, 0, .15f);
#if GRABJOINT2
            var targetDist = dimensions.upperArmLength + math.length(dimensions.lowerAnchor - handAnchor);
            joint2.Create(chestId, chestAnchor, subject.bodyId, targetAnchor, targetDist, .15f);
#endif
            IgnoreCollision(target.collision);
            NotifyGrabbables(true);
            if(other.grabState == HandState.Hold) 
                Physics.IgnoreCollision(handCollider, other.handCollider, true);
        }


        public void ReleaseGrabOnFixedUpdate(float blockTime = 0.01f)
        {
            blockGrab = Mathf.Max(blockGrab, blockTime);
        }

        public void ReleaseGrab(float blockTime = 0)
        {
            blockGrab = Mathf.Max(blockGrab, blockTime);
            if (grabState == HandState.Hold)
            {
                ref var carryData = ref EntityStore.GetComponentData<CarryData>(entity);
                Carry.Release(ManagedWorld.main.FindBody(grabbedBody, optional: true), ref carryData, isLeft);

                joint1.Destroy();
                joint2.Destroy();

                NotifyGrabbables(false);
                UnignoreCollision();

                if(grabbedBody!= null && customGrabHandlers.TryGetValue(grabbedBody,out ICustomGrabHandler customGrabHandler))
                {
                    customGrabHandlers.Remove(grabbedBody);
                    ManagedWorld.main.UnregisterBody(ManagedWorld.main.FindBody(grabbedBody));
                    customGrabHandler.OnRelease(grabbedBody);
                }

                grabbedBody = null;
                grabbedCollider = null;
            }
            grabState = HandState.Idle;
            Physics.IgnoreCollision(handCollider, other.handCollider, false);

        }

        #region IGrabbables
        static List<IGrabbable> grabbablesToAppend = new List<IGrabbable>(32);
        private static void AppendGrabbablesUntilARigidbodyIsFound(List<IGrabbable> list, Transform root)
        {
            root.GetComponents<IGrabbable>(grabbablesToAppend);
            list.AddRange(grabbablesToAppend);
            grabbablesToAppend.Clear();

            for (int i = 0; i < root.childCount; ++i)
            {
                var child = root.GetChild(i);
                if (child.GetComponent<Rigidbody>() != null)
                    continue;
                AppendGrabbablesUntilARigidbodyIsFound(list, child);
            }
        }

        static List<IGrabbable> grabbables = new List<IGrabbable>(32);
        private unsafe void NotifyGrabbables(bool isGrab)
        {
            grabbables.Clear();
            if (grabbedBody != null)
            {
                AppendGrabbablesUntilARigidbodyIsFound(grabbables, grabbedBody.transform);
            }
            else if (grabbedCollider != null)
            {   
                AppendGrabbablesUntilARigidbodyIsFound(grabbables, grabbedCollider.transform);
            }

            for (int i = 0; i < grabbables.Count; ++i)
            {
                var grabable = grabbables[i];
                if (isGrab)
                    grabable.OnGrab(entity, this);
                else
                    grabable.OnRelease(entity, this);
            }
            grabbables.Clear();
        }
        #endregion

        #region Collisions
        List<Collider> grabbedBodyColliders = new List<Collider>(32);
        private static unsafe void CollectColliders(List<Collider> grabbedBodyColliders, Rigidbody grabbedBody, Collider grabbedCollider)
        {
            if (grabbedBody != null)
            {
                grabbedBody.GetComponentsInChildren<Collider>(false, grabbedBodyColliders);
                for (int i = grabbedBodyColliders.Count - 1; i >= 0; i--)
                {
                    if (grabbedBodyColliders[i].attachedRigidbody != grabbedBody)
                        grabbedBodyColliders.RemoveAt(i);
                }
            }
            else if (grabbedCollider != null)
            {
                grabbedBodyColliders.Add(grabbedCollider);
            }
        }

        private unsafe void IgnoreCollision(GrabJointCollision collision)
        {
            CollectColliders(grabbedBodyColliders, grabbedBody, grabbedCollider);

            if (grabbedBodyColliders.Count > 0)
            {
                foreach (var c in grabbedBodyColliders)
                {
                    Physics.IgnoreCollision(handCollider, c, true);
                    if (collision == GrabJointCollision.Ignore || collision == GrabJointCollision.IgnoreLower) Physics.IgnoreCollision(forearmCollider, c, true);
                    if (collision == GrabJointCollision.Ignore) Physics.IgnoreCollision(upperarmCollider, c, true);
                }
            }
        }
        private void UnignoreCollision()
        {
            if (grabbedBodyColliders.Count > 0)
            {
                foreach (var c in grabbedBodyColliders)
                    if (c != null)
                    {
                        Physics.IgnoreCollision(handCollider, c, false);
                        Physics.IgnoreCollision(forearmCollider, c, false);
                        Physics.IgnoreCollision(upperarmCollider, c, false);
                    }
            }
            grabbedBodyColliders.Clear();
        }
        #endregion

        // A: grab target detection - use capsule from shoulder to hand to detect colliders in range
        public const float capsuleMin = .1f;
        public const float capsuleMax = .3f;

        // B: once target is found, IK will pull hand to it until it's brounght into snap distance
        public const float targetSnapDistance = .05f; // assume we've collided with object if close enough (too big and visible pops will appear, also could pull through thin objects);

        const float raycastSnapDistance = .10f; // will also spherecast along hand trajectory with a bigger tolerance (pop is less visible because we snap to hand target pos, opposed to sanning for targets in wider area that may result in sideways motion)

        // C: if found object contains grab targets (grips), retarget the anchor to grip if grip is not too far away
        public const float gripSnapDistance = .30f; // distance at which grab will be retargeted to grip

        // D: when joint is created it will try to close the distance in few frames, but if we're already close enough simply snap things in place
        public const float grabJointSnapTreshold = .03f;// snap grab joints without animating (pulling by NoodleHandPullJoint) when close to grab


        private void ApplyPhysicsMaterial()
        {
            // set material
            if (grabState == HandState.Grab)
                handCollider.sharedMaterial = forearmCollider.sharedMaterial = slipperyMaterial;
            else
                handCollider.sharedMaterial = forearmCollider.sharedMaterial = normalMaterial;
        }

        public unsafe void HandleInput(bool grab, float3 shoulderPos)
        {
            ref var carryData = ref EntityStore.GetComponentData<CarryData>(entity);
            ref var dim = ref EntityStore.GetComponentData<NoodleDimensions>(entity);
            ref var hand = ref isLeft ? ref carryData.l : ref carryData.r;

            if (grabState == HandState.Hold && grab) // release on overstretch
            {
                var worldAnchor = Carry.GetWorldAnchor(hand);
                // release if stretched more than 50% 
                if (math.distance(shoulderPos, worldAnchor) > (dim.armL.upperArmLength + dim.armL.lowerArmLength) * 1.50f)
                    grab = false;
            }

            // release if grab blocked by timer
            blockGrab -= Time.fixedDeltaTime;
            if (blockGrab > 0) grab = false;

            if (grabState == HandState.Hold && !grab)
                ReleaseGrab(0);
            if (grabState != HandState.Hold)
                grabState = grab ? HandState.Grab : HandState.Idle;
        }
        public void OnFixedUpdate()
        {
            ref var carryData = ref EntityStore.GetComponentData<CarryData>(entity);
            ref var hand = ref isLeft ? ref carryData.l : ref carryData.r;

            joint1.OnFixedUpdate(out var f1);
            joint2.OnFixedUpdate(out var f2);


            ApplyPhysicsMaterial();

            // store state in CarryData
            hand.state = grabState;
            hand.unityJointForce = f1 + f2;
        }


        public HandReachInfo GetReachInfo(float3 shoulder, float3 target)
        {
            var handX = World.main.GetBodyPosition(handId);
            var actualHandPos = math.transform(handX, handAnchor);

            return new HandReachInfo()
            {
                left = isLeft,
                handId = handId,
                shoulderPos = shoulder,
                targetPos = target,
                actualPos = actualHandPos,
                worldPalmX = math.mul(handX, RigidTransform.Translate(palmAnchor)),
                radius = handRadius,
            };
        }


        public int CapsuleCast(Collider[] neighbours, in CarryReachInfo reach, bool left)
        {
            var handReach = left ? reach.handL : reach.handR;
            GetReachCapsule(handReach, out var capsuleLen, out var capsule1, out var capsule2, out var capsuleDir);
            return Physics.OverlapSphereNonAlloc((capsule1 + capsule2) / 2, capsuleMax + capsuleLen / 2, neighbours, collisionLayers, QueryTriggerInteraction.Ignore);
        }

        public static void GetReachCapsule(in HandReachInfo handReach, out float capsuleLen, out float3 capsule1, out float3 capsule2, out float3 capsuleDir)
        {
            capsuleDir = handReach.targetPos - handReach.shoulderPos;
            var handLen = math.length(capsuleDir);
            capsuleDir /= handLen;

            capsuleLen = handLen;
            capsule1 = handReach.shoulderPos + capsuleDir * handLen * .25f;
            capsule2 = capsule1 + capsuleDir * capsuleLen;
        }

        public void Raycast(in CarryReachInfo reach, in HandTargetInfo handTarget)
        {
            var handReach = isLeft ? reach.handL : reach.handR;

            //// Filter1. Collider filtering
            grabTargetCollider = handTarget.subject.collider;
            //grabTargetWeight = targetWeight;
            grabTargetPosition = handReach.targetPos;

            // Filter3. Hit normal - only if desired hand motion is pressing against the normal
            //var snapTarget = upperArm.parent.body.transform.TransformPoint(chestAnchor) + forward * (chestLimit + handRadius) + forward.ZeroY().normalized * handRadius * 2; // calculate target using shoulder as a reference
            var reachPos = handReach.targetPos;
            //if(!handTarget.isEmpty) reachPos = handTarget.grabInfo.worldAnchor; // old algorithm use to reach detected target instead of hand???
            var actualHandPos = handReach.actualPos;
            var reachUp = re.InverseLerp(-30, -80, math.degrees(reach.aim.pitch)); // if looking up
                                                                                   //reachUp *= Mathf.InverseLerp(.1f, .5f, snapTarget.y - actualHandPos.y); // how much left to raise hads to target level
            grabNormalThreshold = math.lerp(math.sin(math.radians(10)), math.sin(math.radians(60)), reachUp); //normally small pressure against normal is needed, but much higher when lifting hands up (to allow wall climb)

            //Debug.Log("----- Weight = " + reachUp + " -----");
            var a = reachPos;// math.lerp(snapTarget, targetPos, targetWeight);
            var b = reachPos.SetY(actualHandPos.y);
            grabNormalDir = math.normalizesafe(a - b);

            // Raycast from hand position towards the target, for nearby objects report collisions
            var rayStart = actualHandPos;
            var rayDir = reachPos - actualHandPos;
            //var rayDir = scanPos - actualHandPos;
            var rayRadius = handRadius * .75f;

            if (Physics.SphereCast(rayStart, rayRadius, rayDir, out var hitInfo, math.length(rayDir) + (handRadius - rayRadius), collisionLayers, QueryTriggerInteraction.Ignore)
                && math.length((float3)hitInfo.point - actualHandPos) < handRadius + raycastSnapDistance // within a snappable distance
                && !hitInfo.collider.transform.IsChildOf(transform.root)) // not part of this human
            {
                HandleCollision(hitInfo.collider, hitInfo.point, actualHandPos, Vector3.zero);
            }

        }

        private void HandleCollision(Collider hitCollider, float3 hitPos, float3 handPos, float3 impulse)
        {
            if (grabState == HandState.Hold) return;
            if (((1 << hitCollider.gameObject.layer) & collisionLayers) == 0) return;
            if (hitCollider.transform.root == transform.root) return;
            //var impulseTreshold = 2;

            var hitDir = math.normalize(hitPos - handPos);

            //if (grabTargetCollider == null //|| grabTargetWeight < .5f // no or weak collider filtering
            //    || hitCollider == grabTargetCollider // or matching the desired collider
            //    //|| ((1 << hitCollider.gameObject.layer) & targerLayers) != 0 // or we hit another grabtarget 
            //    //|| Vector3.Dot(hitDir, -impulse) > impulseTreshold * grabTargetWeight // or we press against an object hard enough and it resists

            //)
            //{
            //Debug.Log(Vector3.Dot(grabNormalDir, hitDir));

            var allowGrabWithoutGrip =
                hitCollider == grabTargetCollider ||  // bumped to desired collider
                math.dot(grabNormalDir, hitDir) > grabNormalThreshold || // pressing against the normal
                math.length(grabTargetPosition - hitPos) < handRadius + .1f; // or hit something close to desired hand positiion
            if(allowGrabWithoutGrip || math.length(grabTargetPosition - hitPos) < handRadius + .25f)
                EntityStore.GetComponentObject<Noodle>(entity).OnHandCollision(hitCollider, hitPos, allowGrabWithoutGrip, isLeft);
        }
    }
}