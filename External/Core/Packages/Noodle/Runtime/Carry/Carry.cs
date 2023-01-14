//#define ARMS_PRECISE_CENTER_PITCH
using NBG.Entities;
using Recoil;
using Unity.Burst;
using Unity.Mathematics;
using Noodles.Animation;
using UnityEngine;
using NBG.Unsafe;

namespace Noodles
{

    // how should we grab the object
    public struct GrabJointInfo
    {
        public int bodyId;
        public float3 bodyAnchor;
        public int handId;
        public float3 handAnchor;
        public quaternion rotationToPivot;
        public int gripId;

        public GrabJointInfo(int bodyId, int gripId, float3 bodyAnchor, int handId, float3 handAnchor, quaternion rotationToPivot)
        {
            this.bodyId = bodyId;
            this.gripId = gripId;
            this.bodyAnchor = bodyAnchor;
            this.handId = handId;
            this.handAnchor = handAnchor;
            this.rotationToPivot = rotationToPivot;
        }
    }
   

    public struct HandCarryData
    {
        int _isLeft;
        public bool isLeft { get => _isLeft > 0; set => _isLeft = value ? 1 : 0; }
        //public bool isMaster; // for two handed grab
        public int blockId;
        public int jointId;
        public int bodyId;
        public int gripId;
        public int type;

        public HandState lastState;
        public HandState state;
        public float carryTime;

        public ForceVector jointForce; // total linear force applied to carried object at joint anchor
        public float3 unityJointForce; // linear force reported by unity
        public float3 undoneJointForce;


        public AnimationBlend anim;

        // initial joint rotation
        public quaternion carryableToPivotRot;

        // dynamics settings
        public Spring jointSpring;
        public Spring worldJointSpring;
        public CarryAlgorithmSingle singleCarryFn;
        public bool allowCarry => singleCarryFn != CarryAlgorithmSingle.None;
        public bool allowReach;
        public bool allowTwoHanded;
        

        public static HandCarryData Init(bool isLeft) => new HandCarryData() { isLeft = isLeft, blockId = -1, bodyId = -1 };

        public override string ToString()
        {
            return (isLeft?"L":"R") + $" body:{bodyId} grip:{gripId} block:{blockId} joint:{jointId}";
        }

        public bool IsCarrying(int bodyId) => bodyId == this.bodyId && state == HandState.Hold;
        public bool isHoldingStatic => state == HandState.Hold &&  // holding
            World.IsEnvironment(bodyId);  // environment

    }

    // attached to Noodle entity
    public struct CarryData
    {
        int _leftMain;
        public bool leftMain { get => _leftMain > 0; set => _leftMain = value ? 1 : 0; }
        public quaternion singleHandedRotationToPivotSave;
        public quaternion carryableToPivotRot;
        public HandCarryData l;
        public HandCarryData r;
        public AnimationBlend anim;
        public float3 pivotOffset;

        public static CarryData empty => new CarryData() { l = HandCarryData.Init(true), r = HandCarryData.Init(false) };
    }
    public static class CarryDataExtensions
    {
        public static ref HandCarryData GetHand(this ref CarryData data, bool left)
        {
            if (left) return ref data.l;
            else return ref data.r;
        }
    }

    /// <summary>
    /// Provides API for grabbing <see cref="Grab"/> and releasing <see cref="Release"/> bodies and
    /// implements game step for carryables <see cref="ProcessCarryables"/>.
    /// </summary>
    public unsafe static class Carry
    {
        #region API


        /// <summary>
        /// Returns carryable joint position in pivot space, e.g. useful to calculate which hand if forward
        /// </summary>
        public static float3 GetAnchorRelativeToPivot(in CarryData data, bool left)
        {
            return math.rotate(data.carryableToPivotRot, GetLocalAnchor(left?data.l:data.r));
        }
        /// <summary>
        /// Returns carryable joint position in world coordinates
        /// </summary>
        public static float3 GetWorldAnchor(HandCarryData hand)
        {
            return GetWorldAnchor(World.main.GetConstraint(hand.blockId).solver, hand.jointId + 1);
        }

        public static float3 GetWorldAnchor(in Solver solver, int idx)
        {
            var bodies = solver.GetBodies();
            var joint = solver.joints.GetJoint<LinearArticulationJoint>(idx);
            return bodies.TransformPoint(joint.link, joint.anchor);
        }
        private static float3 GetLocalAnchor(in HandCarryData hand)
        {
            return GetLocalAnchor(World.main.GetConstraint(hand.blockId), hand.jointId + 1);
        }
        private static float3 GetLocalAnchor(in ConstraintBlock block, int idx)
        {
            var joint = block.solver.joints.GetJoint<LinearArticulationJoint>(idx);
            return joint.anchor;
        }
        private static float3 GetAnchorVelocity(in HandCarryData hand)
        {
            return World.main.GetLocalPointVelocity(hand.bodyId, GetLocalAnchor(hand)).linear;
        }
        #endregion

        /// <summary>
        /// Main 
        /// </summary>
        public static void ProcessCarryables(ref NoodleAnimatorData animator, in NoodleState state, ref CarryData carry, in Articulation articulation, in Aim aim, ref NoodlePose pose, in NoodleDimensions dim)
        {
            ref var poseL = ref pose.handL;
            ref var poseR = ref pose.handR;
            var world = World.main;


            // could be modified by apply fall damping???
            if (carry.l.blockId >= 0)
                ResetCarrySpring(ref world.GetConstraint(carry.l.blockId), carry.l.jointId);
            if (carry.r.blockId >= 0)
                ResetCarrySpring(ref world.GetConstraint(carry.r.blockId), carry.r.jointId);
            //CalculateJointForce(ref carry.l);
            //CalculateJointForce(ref carry.r);

            // prototype one handed carry with right arm
            if (carry.l.blockId >= 0 && carry.r.blockId == carry.l.blockId) // two hanged hold
            {
                if (carry.leftMain)
                    CarryAlgorithms.CarryableCarryTwoHanded(carry, carry.l, carry.r, pose.pivotL, ref poseL, ref poseR, articulation, aim);
                else
                    CarryAlgorithms.CarryableCarryTwoHanded(carry, carry.r, carry.l, pose.pivotR, ref poseR, ref poseL, articulation, aim);
            }
            else
            {
                if (carry.r.blockId >= 0)// && carry.r.wCarry > 0)
                    CarryAlgorithms.CarryableCarryOneHanded(carry.r, pose.pivotR, ref poseR, aim);
                if (carry.l.blockId >= 0)// && carry.l.wCarry > 0)
                    CarryAlgorithms.CarryableCarryOneHanded(carry.l, pose.pivotL, ref poseL, aim);
            }

            // add extra 
            ApplyFallGrabDamping(ref carry, articulation.solver.joints);


            
        }

        public static void GetPivotPose(in NoodleAnimatorData animator, in CarryData carry, in Aim aim, ref NoodlePose pose)
        {
            ref var poseL = ref pose.handL;
            ref var poseR = ref pose.handR;
            if (carry.l.blockId >= 0)
                CarryAnimator.ApplyPivot(carry.l, carry.r, animator, ref poseL, ref pose.pivotL, aim.pitch01, left: true);
            if (carry.r.blockId >= 0)
                CarryAnimator.ApplyPivot(carry.r, carry.l, animator, ref poseR, ref pose.pivotR, aim.pitch01, left: false);
            if (carry.l.blockId >= 0 && carry.l.blockId == carry.r.blockId && math.any( carry.pivotOffset!=0))// two handed carry - update pivot ik to match anchors
                pose.pivotL.offset = pose.pivotR.offset = carry.pivotOffset; 
        }

        static void SendGrabNotifications(int bodyId, bool grabbing, bool firstGrab)
        {

        }

        public static void Grab(in GrabJointInfo grabInfo, ref CarryData data, bool left, in Aim aim)
        {
            ref var hand = ref data.GetHand(left);
            ref var other = ref data.GetHand(!left);

            var carryable = CarryableBase.GetCarryableFromBodyId(grabInfo.bodyId);
            if (carryable != null)
                carryable.TakeGrip(grabInfo.gripId);

            hand.state = HandState.Hold;
            hand.carryTime = 0;
            if (other.blockId >= 0 && other.bodyId == grabInfo.bodyId)
            {
                //data.singleHandedRotationToPivotSave = other.carryableToPivotRot;
                ConvertToTwoHanded(carryable, ref data, ref other, ref hand, grabInfo, aim);
                data.leftMain = !left;// the first one holding is main
                if (carryable != null)
                {
                    carryable.SelectMainHand(ref data);
                    if (carryable.recalculatePivotOffsetTwoHanded)
                        data.pivotOffset = GetAnchorRelativeToPivot(data,false) - GetAnchorRelativeToPivot(data, true);
                    else
                        data.pivotOffset = float3.zero;
                }
                SendGrabNotifications(grabInfo.bodyId, true, false);
            }
            else
            {
                AttachObject(carryable, ref hand, grabInfo, aim);
                SendGrabNotifications(grabInfo.bodyId, true, true);
            }
            //Debug.Log($"Grab {hand} {other}");
        }


        public static void Release(int bodyId, ref CarryData data, bool left)
        {
            ref var hand = ref data.GetHand(left);
            ref var other = ref data.GetHand(!left);

            hand.state = HandState.Idle;
            var carryable = CarryableBase.GetCarryableFromBodyId(bodyId);
            if (carryable != null) carryable.ReleaseGrip(hand.gripId);
            if (hand.blockId == other.blockId) // two hand carry
            {
                SendGrabNotifications(bodyId, false, false);
                ConvertToOneHanded(carryable, ref hand, ref other, bodyId);
                //other.carryableToPivotRot= data.singleHandedRotationToPivotSave;
            }
            else  // single hand carry
            {
                SendGrabNotifications(bodyId, false, true);
                DetachObject(carryable, ref hand);
            }
            //Debug.Log($"Release {hand} {other}");
        }
        private static void AttachObject(CarryableBase carryable, ref HandCarryData hand, in GrabJointInfo grabInfo, in Aim aim)
        {
            // single handed grab
            var builder = new ConstraintBlockBuilder();
            var bodyId = grabInfo.bodyId;
            var handId = grabInfo.handId;
            var gripId = grabInfo.gripId;


            var relativeRot = math.normalize(re.invmul(World.main.GetBodyPosition(handId).rot, World.IsEnvironment(bodyId)?quaternion.identity: World.main.GetBodyPosition(bodyId).rot));
            builder.AddAngularJoint(bodyId, handId, relativeRot);
            builder.AddLinearJoint(bodyId, grabInfo.bodyAnchor, handId, grabInfo.handAnchor);
            hand.jointId = 0;
            hand.bodyId = bodyId;
            hand.gripId = gripId;
            if (carryable != null) // world orientation
            {
                builder.AddAngularJoint(bodyId, World.environmentId, quaternion.identity);
                carryable.SetupOneHanded(ref hand);
            }

            hand.carryableToPivotRot = grabInfo.rotationToPivot;

            ref var constraint = ref builder.BuildConstraint(out hand.blockId);
            
        }

        private static void DetachObject(CarryableBase carryable, ref HandCarryData hand)
        {
            ConstraintBlockBuilder.ReleaseConstraintSolver(hand.blockId);
            hand.bodyId = hand.blockId = hand.gripId = - 1;
            CarryableBase.Clear(ref hand);
        }
        private static void ConvertToTwoHanded(CarryableBase carryable, ref CarryData carry, ref HandCarryData first, ref HandCarryData second, in GrabJointInfo grabInfo, in Aim aim)
        {
            ref var solver = ref World.main.GetConstraint(first.blockId).solver; // get existing block

            var builder = new ConstraintBlockBuilder();
            var bodyId = grabInfo.bodyId;
            var handId = grabInfo.handId;
            builder.ReAddAngularJoint(solver, solver.GetJoint<AngularArticulationJoint>(first.jointId + 0));
            builder.ReAddLinearJoint(solver, solver.GetJoint<LinearArticulationJoint>(first.jointId + 1));
            first.jointId = 0;
            var relativeRot = math.normalize(re.invmul(World.main.GetBodyPosition(handId).rot, World.IsEnvironment(bodyId) ? quaternion.identity : World.main.GetBodyPosition(bodyId).rot));
            builder.AddAngularJoint(bodyId, handId, relativeRot);
            builder.AddLinearJoint(bodyId, grabInfo.bodyAnchor, handId, grabInfo.handAnchor);
            second.jointId = 2;
            if (carryable != null) // world orientation
                builder.AddAngularJoint(bodyId, World.environmentId, quaternion.identity);

            var firstCopy = first;
            DetachObject(carryable, ref first);
            first = firstCopy;

            builder.BuildConstraint(out second.blockId);
            first.blockId = second.blockId;
            first.bodyId = second.bodyId = bodyId;
            second.gripId = grabInfo.gripId;

            if (carryable != null)
            {
                second.carryableToPivotRot = grabInfo.rotationToPivot;
                if (!carryable.useRotationToPivotFromFirstGrip)
                    carry.carryableToPivotRot = grabInfo.rotationToPivot;
                else
                    carry.carryableToPivotRot = first.carryableToPivotRot;

                carryable.SetupTwoHanded(ref first, ref second);
            }

        }

        private static void ConvertToOneHanded(CarryableBase carryable, ref HandCarryData release, ref HandCarryData keep, int bodyId)
        {
            ref var solver = ref World.main.GetConstraint(keep.blockId).solver; // get existing block
            var builder = new ConstraintBlockBuilder();
            builder.ReAddAngularJoint(solver, solver.GetJoint<AngularArticulationJoint>(keep.jointId + 0));
            builder.ReAddLinearJoint(solver, solver.GetJoint<LinearArticulationJoint>(keep.jointId + 1));
            keep.jointId = 0;
            if (carryable != null) // world orientation
            {
                builder.AddAngularJoint(bodyId, World.environmentId, quaternion.identity);
                carryable.SetupOneHanded(ref keep);
            }

            DetachObject(carryable, ref release);

            builder.BuildConstraint(out keep.blockId);

            
        }
     

        public static void SetWorldAnchor(HandCarryData hand, float3 pos)
        {
            SetWorldAnchor(World.main.GetConstraint(hand.blockId), hand.jointId + 1, pos);
        }

        private static void SetWorldAnchor(in ConstraintBlock block, int idx, float3 worldPos)
        {
            var bodies = block.GetBodies();
            var joint = block.solver.joints.GetJoint<LinearArticulationJoint>(idx);
            joint.anchor = bodies.InverseTransformPoint(joint.link, worldPos);
        }
       

        #region Fall Grab Damping - stabilize simulation when grabbing something during high velocity fly
        private static void ResetCarrySpring(ref ConstraintBlock constraint, int idx)
        {
            if (constraint.destroyed)
                throw new System.Exception("Constraint is destroyed.");
            constraint.solver.GetJoint<AngularArticulationJoint>(idx + 0).spring = new Spring(0,5);// small damper
            constraint.solver.GetJoint<LinearArticulationJoint>(idx + 1).spring = Spring.stiff;
        }


        private static void ApplyFallGrabDamping(ref CarryData carry, in ArticulationJointArray joints)
        {
            var n = new NoodleJoints(joints);
            if (carry.l.blockId >= 0) ApplyFallGrabDamping(ref World.main.GetConstraint(carry.l.blockId), carry.l.jointId, n.armL);
            if (carry.r.blockId >= 0) ApplyFallGrabDamping(ref World.main.GetConstraint(carry.r.blockId), carry.r.jointId, n.armR);
        }

        private static void ApplyFallGrabDamping(ref ConstraintBlock constraint, int idx, in NoodleArmJoints arm)
        {
            if (constraint.destroyed)
                throw new System.Exception("Constraint is destroyed.");
            ref var angular = ref constraint.solver.GetJoint<AngularArticulationJoint>(idx + 0);
            ref var linear = ref constraint.solver.GetJoint<LinearArticulationJoint>(idx + 1);
            var bodies = constraint.GetBodies();
            var vA = bodies.GetLocalPointVelocity( linear.connectedLink, linear.connectedAnchor).linear;
            var vB = bodies.GetLocalPointVelocity( linear.link, linear.anchor).linear;
            var dampen = math.max(0, math.length(vA - vB) - 5);
            dampen *= dampen;// square

            angular.spring.kd += dampen * 25;
            arm.upper.springX.kd += dampen * 2;
            arm.upper.springY.kd += dampen * 2;
            arm.upper.springZ.kd += dampen * 2;
            arm.lower.springX.kd += dampen * .5f;
            arm.lower.springY.kd += dampen * .5f;
            arm.lower.springZ.kd += dampen * .5f;
        }


        #endregion
        #region Inferring Carried Mass from joint impulses


        internal static void ResetJointForces(ref CarryData carry)
        {
            carry.l.undoneJointForce = carry.r.undoneJointForce = float3.zero;
        }
        public static void InferCarriedMass(in Articulation articulation, ref CarryData carry, out float3 pos, out float mass)
        {
            // calculate total force applied to carried objects in ball origin
            ApplyJointLimit(articulation, ref carry.l);
            ApplyJointLimit(articulation, ref carry.r);
            CalculateJointForce(ref carry.l);
            CalculateJointForce(ref carry.r);


            if (carry.l.blockId >= 0 && carry.r.blockId >= 0)
            {
                var feedbackPos = (GetWorldAnchor(carry.l) + GetWorldAnchor(carry.r)) / 2;
                var totalFeedback =
                    carry.l.jointForce.TranslateBy(feedbackPos - GetWorldAnchor(carry.l)) +
                    carry.r.jointForce.TranslateBy(feedbackPos - GetWorldAnchor(carry.r));
                mass = CalculateCariedMass(totalFeedback, feedbackPos, out pos);
            } 
            else if(carry.l.blockId >= 0)
            {
                var feedbackPos = GetWorldAnchor(carry.l);
                var totalFeedback = carry.l.jointForce;
                mass = CalculateCariedMass(totalFeedback, feedbackPos, out pos);
            }
            else if (carry.r.blockId >= 0)
            {
                var feedbackPos = GetWorldAnchor(carry.r);
                var totalFeedback = carry.r.jointForce;
                mass = CalculateCariedMass(totalFeedback, feedbackPos, out pos);
            }
            else
            {
                pos = default;
                mass = default;
            }

            // clamp offset to 1m from ball
            var cg = articulation.GetBodies().GetBody(NoodleBones.Ball).x.pos;
            pos = cg+(pos-cg).ZeroY().Clamp(1);
            // and mass to 50kg
            mass = math.clamp(mass, -10, 50);
            //NoodleDebug.builder.Ray(pos, re.up*mass / 10, new Color( World.main.currentIteration*.1f,0,0));
        }
        private static void ApplyJointLimit(in Articulation articulation, ref HandCarryData hand)
        {
            if (hand.blockId >= 0)
            {
                var solver = articulation.solver;
                var n = new NoodleJoints(solver.joints);
                var arm = hand.isLeft ? n.armL : n.armR;
                ref var joint = ref solver.joints.GetJoint(arm.ikID);

                var _v = articulation.ExtractWorldVelocityCopy();
                using (var context = articulation.GetContext(_v))
                    arm.IK.ApplyImpulseLimit(solver, context, joint.row, World.main.dt, ref hand.undoneJointForce);
                articulation.WriteAndDisposeVelocityCopy(_v);
            }
        }

        private static void CalculateJointForce(ref HandCarryData hand)
        {
            if (hand.blockId >= 0)
            {
                var block = World.main.GetConstraint(hand.blockId);
                var pos = GetWorldAnchor(block.solver, hand.jointId + 1);
                var impulse = GetImpulseAtPos(block.solver, hand.jointId + 0, pos)
                    + GetImpulseAtPos(block.solver, hand.jointId + 1, pos);
                if (block.solver.nJoints == 3) // extra joint for carryable alignment
                    impulse += GetImpulseAtPos(block.solver, 2, pos);
                if (block.solver.nJoints == 5) // two handed carryable alignment joint - distribute equally for both arms
                    impulse += GetImpulseAtPos(block.solver, 4, pos) * .5f;

                hand.jointForce = ForceVector.Linear(hand.unityJointForce) + impulse * (1 / World.main.dt);// + ForceVector.Linear(hand.undoneJointForce);
            }
            else
                hand.jointForce = ForceVector.zero;
        }


        public static ForceVector GetImpulseAtPos(in Solver solver, int idx, float3 pos)
        {
            ref var joint = ref solver.joints.GetJoint(idx);
            var impulse = *(float3*)((float*)solver.impulse4 + joint.row);
            if (joint.jointType == Recoil.ArticulationJointType.Angular)
                return ForceVector.Angular(impulse);
            else
                return ForceVector.Linear(impulse).TranslateBy(pos - solver.GetBodies().TransformPoint(joint.linear.connectedLink, joint.linear.connectedAnchor));
        }
        private static float CalculateCariedMass(ForceVector force, float3 forcePos, out float3 cg)
        {
            cg = forcePos;
            // clamp linear to lift only
            if (force.linear.y < 0)
                force.linear = force.linear.ZeroY();
            Articulation.MoveToFulcrum(ref force, ref cg);
            return force.linear.y / math.length(World.main.gravity);

            //NoodleDebug.builder.Ray(p, re.up * m/ 100, Color.magenta);
            
        }

       
        #endregion
    }
}