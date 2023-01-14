#define JOINTPULL

using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class NoodleHandPullJoint
    {
        //A-hand, B-carried
        int bodyA;
        int bodyB;
        float3 anchorA;
        float3 anchorB;
        float targetDistance;
        ConfigurableJoint grabJoint;
        float pullDist;
        float pullSpeed = 0;
        public void Create(int bodyA, float3 anchorA, int bodyB, float3 anchorB, float targetDistance, float timeToSnap)
        {
            this.bodyA = bodyA;
            this.anchorA = anchorA;
            this.bodyB = bodyB;
            this.anchorB = anchorB;
            this.targetDistance = targetDistance;

            var rigidA = ManagedWorld.main.GetRigidbody(bodyA);
            var rigidB = ManagedWorld.main.GetRigidbody(bodyB);
            grabJoint = rigidA.gameObject.AddComponent<ConfigurableJoint>();
            grabJoint.anchor = World.main.LocalBodyToPhysX(bodyA, anchorA); 
            grabJoint.autoConfigureConnectedAnchor = false;
            grabJoint.connectedBody = rigidB;
            grabJoint.connectedAnchor = World.main.TransformPoint(bodyB, anchorB);// World.main.LocalBodyToPhysX(bodyB, anchorB);

            if (rigidB != null)
                grabJoint.connectedAnchor = rigidB.transform.InverseTransformPoint(grabJoint.connectedAnchor);

            pullDist = math.max(0, math.length(World.main.TransformPoint(bodyA, anchorA) - World.main.TransformPoint(bodyB, anchorB)) - targetDistance);
            if (pullDist < NoodleHand.grabJointSnapTreshold) pullDist = 0;// snap when close
#if !JOINTPULL
            pullDist = 0; // snap when joint pulling is disabled
#endif
            pullSpeed = pullDist / timeToSnap;
            
            grabJoint.xMotion = ConfigurableJointMotion.Limited;
            grabJoint.yMotion = ConfigurableJointMotion.Limited;
            grabJoint.zMotion = ConfigurableJointMotion.Limited;
            grabJoint.linearLimit = new SoftJointLimit() { limit = pullDist+targetDistance };
            grabJoint.angularXMotion = ConfigurableJointMotion.Free;
            grabJoint.angularYMotion = ConfigurableJointMotion.Free;
            grabJoint.angularZMotion = ConfigurableJointMotion.Free;
            // grabJoint.breakForce = JOINT_BREAK_FORCE;// float.PositiveInfinity;// 40000f;
            // grabJoint.breakTorque = JOINT_BREAK_FORCE;// float.PositiveInfinity; //40000f;
            grabJoint.enablePreprocessing = false;
            grabJoint.enableCollision = true;

        }

        public void Destroy()
        {
            if(grabJoint!=null)
                GameObject.DestroyImmediate(grabJoint);
            grabJoint = null;
        }

        public void OnFixedUpdate(out float3 jointForce)
        {
            jointForce = (grabJoint != null) ? -grabJoint.currentForce : default;
            if (pullSpeed == 0) return;
            if (grabJoint != null && pullDist > targetDistance)
            {
                var dist = math.max(0, math.length(World.main.TransformPoint(bodyA, anchorA) - World.main.TransformPoint(bodyB, anchorB))-targetDistance);
                pullDist = math.min(pullDist, dist);
                pullDist = re.MoveTowards(pullDist, 0, pullSpeed*World.main.dt);
                grabJoint.linearLimit = new SoftJointLimit() { limit = pullDist +targetDistance};
            }
        }
    }
}
