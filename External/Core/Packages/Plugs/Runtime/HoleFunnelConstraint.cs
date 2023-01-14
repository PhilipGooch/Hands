using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Plugs
{
    /// <summary>
    /// Guide joint controller
    /// </summary>
    public class HoleSlotConstraint
    {
        public ConfigurableJoint plane1;
        public ConfigurableJoint plane2;

        /// <summary>
        /// Create guide joints
        /// </summary>
        /// <param name="data"> Joint data</param>
        /// <param name="plugBody"> Plug Rigidbody</param>
        /// <param name="holeBody"> Socket Rigidbody</param>
        public void Connect(ref HoleSlotConstraintData data, Rigidbody plugBody, Rigidbody holeBody)
        {
            var plugAnchor = data.relativePlugX.pos;
            plane1 = CreatePlaneConstraint(plugBody, plugAnchor, holeBody);
            plane2 = CreatePlaneConstraint(plugBody, plugAnchor, holeBody);

            data.isConnected = true;
            data.offsetMin = HoleFunnelConstraint.maxOffset;
            data.offsetMax = HoleFunnelConstraint.maxOffset;
        }

        internal static float CalculateOffset(float3 plugPositionInHole, float disengageDist)
        {
            var x = plugPositionInHole.y;
            var dist = -plugPositionInHole.z;

            Debug.Assert(disengageDist > 0);

            //scale height to 0..1
            var h = math.saturate(dist / disengageDist);

            // calculate position of on approach curve, nomal and angle
            var xLimit = 1 - math.sin((1 - h) * math.PI / 2);

            // scale back from 0..1 to disengageDepth
            xLimit *= disengageDist;

            var offset1 = math.max(0, -xLimit - x);
            var offset2 = math.max(0, -xLimit + x);
            return math.max(offset1, offset2);
        }

        // true if changed considerably

        internal static bool CalculateNormals(ref HoleSlotConstraintData data, float3 plugPositionInHole, float disengageDist)
        {
            var x = plugPositionInHole.y;
            var dist = -plugPositionInHole.z;

            Debug.Assert(disengageDist > 0);

            //scale height to 0..1
            var h = math.saturate(dist / disengageDist);

            //var moveMag = math.max(0, plugPositionInHole.z - localPositionCache.z);
            var moveMag = HoleFunnelConstraint.pullVelocity * World.main.dt * (1 - h);
            var offset1 = re.MoveTowards(data.offsetMin, 0, moveMag);
            var offset2 = re.MoveTowards(data.offsetMax, 0, moveMag);

            // calculate position of on approach curve, nomal and angle
            var xLimit = 1 - math.sin((1 - h) * math.PI / 2);
            var nx = math.cos((1 - h) * math.PI / 2);
            var a = math.atan2(nx, 1);

            // scale back from 0..1 to disengageDepth
            xLimit *= disengageDist;
            h *= disengageDist;

            //offset1 = math.min(offset1, dist);
            //offset2 = math.min(offset2, dist);
            // make sure offsets are monotonously diminishing
            offset1 = math.max(0, math.min(-xLimit - x, offset1));
            offset2 = math.max(0, math.min(-xLimit + x, offset2));

            bool write(ref float a, float b) { if (math.abs(a - b) > .001) { a = b; return true; } return false; }
            var res = false;
            // calculate constraint plane normals
            res |= write(ref data.width, xLimit);
            res |= write(ref data.angle, a - math.PI / 2);
            res |= write(ref data.offsetMin, offset1);
            res |= write(ref data.offsetMax, offset2);
            res |= write(ref data.depth, -h);
            return res;
        }

        internal void WriteNormals(ref HoleSlotConstraintData data)
        {
            WriteNormals(plane1, -data.width - data.offsetMin, data.depth, -data.angle, data.relativeHoleX);
            WriteNormals(plane2, +data.width + data.offsetMax, data.depth, +data.angle, data.relativeHoleX);
        }

        internal void WriteNormals(ConfigurableJoint joint, float offset, float depth, float angle, in RigidTransform holeToBody)
        {
            var normal = math.rotate(holeToBody.rot, new float3(0, 0, -1).RotateX(angle));
            var anchor = math.transform(holeToBody, new float3(0, offset, depth));

            float tolerance = .98f; // 1 is very stiff, but can get stuck because of float precision
            float range = 1;
            joint.axis = normal;
            joint.anchor = anchor + normal * range * tolerance;
        }

        /// <summary>
        /// Destroy guide joint
        /// </summary>
        /// <param name="data">Joint data</param>
        public void Disconnect(ref HoleSlotConstraintData data)
        {
            data.isConnected = false;
            GameObject.Destroy(plane1);
            GameObject.Destroy(plane2);
            plane1 = null;
            plane2 = null;
        }

        /// <summary>
        /// Creates a joint and constrains its X axis movement
        /// </summary>
        /// <param name="plug"> Plug Rigidbody - constraint target</param>
        /// <param name="plugAnchor"> Joint connected anchor</param>
        /// <param name="hole"> Socket rigidbody - joint parent</param>
        /// <returns></returns>
        public static ConfigurableJoint CreatePlaneConstraint(Rigidbody plug, float3 plugAnchor, Rigidbody hole)
        {
            float range = 1;
            var joint = hole.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedBody = plug;
            joint.connectedAnchor = plugAnchor;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.linearLimit = new SoftJointLimit() { limit = range };
            return joint;
        }
    }

    /// <summary>
    /// Snap joint controller - locks plugged plug in place
    /// </summary>
    public class HoleSnapConstraint
    {
        public ConfigurableJoint joint;

        float snapJointSpring;
        float3 holeAnchor;
        float3 axis;

        //increasing this also increases how much plug has to travel to disconnect?
        const int kTension = 200;
        const int kKd = 50;

        public HoleSnapConstraint(float snapJointSpring)
        {
            this.snapJointSpring = snapJointSpring;
        }

        /// <summary>
        /// Create a snap joint which will hold plugged plugBody in place
        /// </summary>
        /// <param name="data">Socket and its active plug data</param>
        /// <param name="plugBody">Plug Rigidbody</param>
        /// <param name="holeBody">Socket Rigidbody</param>
        public void Engage(ref PlugAndHoleData data, Rigidbody plugBody, Rigidbody holeBody)
        {
            data.snapped = true;
            CreateHoldConstraint(plugBody, data.relativePlugX.pos, holeBody, data.relativeHoleX.pos, math.rotate(data.relativeHoleX.rot, re.forward));
        }

        /// <summary>
        /// Destroy snap joint
        /// </summary>
        /// <param name="data">Socket and its active plug data</param>
        public void Destroy(ref PlugAndHoleData data)
        {
            Disengage(ref data);
        }

        /// <summary>
        /// Destroy snap joint
        /// </summary>
        /// <param name="data">Socket and its active plug data</param>
        public void Disengage(ref PlugAndHoleData data)
        {
            data.snapped = false;
            GameObject.Destroy(joint);
            joint = null;
        }

        public void LockJointLinearMovement()
        {
            if (joint != null)
            {
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
            }
        }

        public void UnlockJointLinearMovement()
        {
            if (joint != null)
            {
                joint.xMotion = ConfigurableJointMotion.Free;
                joint.yMotion = ConfigurableJointMotion.Free;
                joint.zMotion = ConfigurableJointMotion.Free;
            }
        }

        public void LockJointAngularMovement()
        {
            if (joint != null)
            {
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
            }
        }

        public void UnlockJointAngularMovement()
        {
            if (joint != null)
            {
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;
            }
        }

        /// <summary>
        /// Creates a snap joint which holds plug in the socket
        /// </summary>
        /// <param name="plug">Plug Rigidbody</param>
        /// <param name="plugAnchor">Plug anchor</param>
        /// <param name="hole">Socket Rigidbody</param>
        /// <param name="holeAnchor">Socket anchor</param>
        /// <param name="axis">Joint axis</param>
        internal void CreateHoldConstraint(Rigidbody plug, float3 plugAnchor, Rigidbody hole, float3 holeAnchor, float3 axis)
        {
            this.holeAnchor = holeAnchor;
            this.axis = axis;

            joint = hole.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.axis = axis;

            SetJointSpring(snapJointSpring);

            joint.connectedBody = plug;
            joint.connectedAnchor = plugAnchor;
        }

        /// <summary>
        /// Set snap joint spring
        /// </summary>
        /// <param name="snapJointSpring">How strong the spring that holds the plug in the socket will be. Larger values allow joint to hold heavier plugs. Cannot be 0.</param>
        public void SetJointSpring(float snapJointSpring)
        {
            this.snapJointSpring = snapJointSpring;

            if (snapJointSpring == 0)
            {
                snapJointSpring = 1;
                Debug.LogError("snapJointSpring cannot be 0");
            }

            if (joint != null)
            {
                joint.anchor = holeAnchor + axis * kTension / snapJointSpring; // preload
                joint.xDrive = joint.yDrive = joint.zDrive = new JointDrive() { positionDamper = kKd, positionSpring = snapJointSpring, maximumForce = float.MaxValue };
            }
        }

        internal static bool ShouldSnap(in PlugAndHoleData data, RigidTransform plugX, RigidTransform holeX)
        {
            return data.allowSnap && !data.snapped && HoleFunnelConstraint.GetPinDistance(data, plugX, holeX) < data.plugDist;
        }

        internal static bool ShouldUnsnap(in PlugAndHoleData data, RigidTransform plugX, RigidTransform holeX)
        {
            return data.snapped && HoleFunnelConstraint.GetPinDistance(data, plugX, holeX) > data.unplugDist;
        }
    }

    /// <summary>
    /// Controls guide joints
    /// </summary>
    public class HoleFunnelConstraint
    {
        public HoleSlotConstraint funnelH = new HoleSlotConstraint();
        public HoleSlotConstraint funnelV = new HoleSlotConstraint();

        public const float maxOffset = .2f;
        public const float pullVelocity = 1;// pull velocity to force constraint

        /// <summary>
        /// Setup guide joints controller
        /// </summary>
        /// <param name="data"></param>
        /// <param name="plug"></param>
        /// <param name="hole"></param>
        /// <param name="bothAxes"></param>
        /// <param name="disengageDist"></param>
        public void Create(ref HoleFunnelConstraintData data, RigidTransform plug, RigidTransform hole, bool bothAxes, float disengageDist)
        {
            data.disengageDist = disengageDist;
            data.bothAxes = bothAxes;
            data.isCreated = true;
            data.slotH.relativeHoleX = hole;
            data.slotV.relativeHoleX = math.mul(hole, RigidTransform.RotateZ(math.PI / 2));
            data.slotH.relativePlugX = plug;
            data.slotV.relativePlugX = plug;
        }

        public void Destroy(ref HoleFunnelConstraintData data)
        {
            data.isCreated = false;
            Disengage(ref data);
        }

        /// <summary>
        /// Creates guide joints if possible
        /// </summary>
        /// <param name="data"></param>
        /// <param name="plugX"></param>
        /// <param name="holeX"></param>
        /// <param name="plugBody"></param>
        /// <param name="holeBody"></param>
        public void Engage(ref HoleFunnelConstraintData data, RigidTransform plugX, RigidTransform holeX, Rigidbody plugBody, Rigidbody holeBody)
        {
            if (!data.isCreated) return;

            funnelH.Connect(ref data.slotH, plugBody, holeBody);
            if (data.bothAxes)
                funnelV.Connect(ref data.slotV, plugBody, holeBody);
        }

        /// <summary>
        /// Disconnects guide joints if connected
        /// </summary>
        /// <param name="data"></param>
        public void Disengage(ref HoleFunnelConstraintData data)
        {
            if (data.slotH.isConnected) funnelH.Disconnect(ref data.slotH);
            if (data.slotV.isConnected) funnelV.Disconnect(ref data.slotV);
        }

        /// <summary>
        /// Get distance between plug pin and socket
        /// </summary>
        /// <param name="data"></param>
        /// <param name="plugX"></param>
        /// <param name="holeX"></param>
        /// <returns></returns>
        public static float GetPinDistance(in HoleSlotConstraintData data, RigidTransform plugX, RigidTransform holeX)
        {
            return GetPinDistance(plugX, holeX, data.relativeHoleX, data.relativePlugX.pos);
        }

        public static float3 InverseTransformPinToHole(in HoleSlotConstraintData data, RigidTransform plugX, RigidTransform holeX)
        {
            var holeWorld = math.mul(holeX, data.relativeHoleX);
            var plugWorldPos = math.transform(plugX, data.relativePlugX.pos);
            return re.invmul(holeWorld, plugWorldPos);
        }

        /// <summary>
        /// Get distance between plug pin and socket
        /// </summary>
        /// <param name="data"></param>
        /// <param name="plugX"></param>
        /// <param name="holeX"></param>
        /// <returns></returns>
        public static float GetPinDistance(in PlugAndHoleData data, RigidTransform plugX, RigidTransform holeX)
        {
            return GetPinDistance(plugX, holeX, data.relativeHoleX, data.relativePlugX.pos);
        }

        static float GetPinDistance(RigidTransform plugX, RigidTransform holeX, RigidTransform relativeHoleX, float3 relativeHoleXPos)
        {
            var holeWorld = math.mul(holeX, relativeHoleX);
            var plugWorldPos = math.transform(plugX, relativeHoleXPos);
            return -re.invmul(holeWorld, plugWorldPos).z;
        }

        public static float3 InverseTransformPinToSocket(in PlugAndHoleData data, RigidTransform plugX, RigidTransform holeX)
        {
            var holeWorld = math.mul(holeX, data.relativeHoleX);
            var plugWorldPos = math.transform(plugX, data.relativePlugX.pos);
            return re.invmul(holeWorld, plugWorldPos);
        }

        internal static bool ShouldEngage(in HoleFunnelConstraintData data, RigidTransform plugX, RigidTransform holeX)
        {
            return GetPinDistance(data.slotH, plugX, holeX) < data.disengageDist &&
                HoleSlotConstraint.CalculateOffset(InverseTransformPinToHole(data.slotH, plugX, holeX), data.disengageDist) < maxOffset &&
                (!data.bothAxes || HoleSlotConstraint.CalculateOffset(InverseTransformPinToHole(data.slotV, plugX, holeX), data.disengageDist) < maxOffset);
        }

        internal static bool ShouldDisengage(in HoleFunnelConstraintData data, RigidTransform plugX, RigidTransform holeX)
        {
            return GetPinDistance(data.slotH, plugX, holeX) > data.disengageDist;
        }

        public static void CalculateConstraints(ref HoleFunnelConstraintData data, RigidTransform plugX, RigidTransform holeX)
        {
            data.normalsDirty = false;
            if (data.slotH.isConnected) data.normalsDirty = HoleSlotConstraint.CalculateNormals(ref data.slotH, InverseTransformPinToHole(data.slotH, plugX, holeX), data.disengageDist);
            if (data.slotV.isConnected) data.normalsDirty = HoleSlotConstraint.CalculateNormals(ref data.slotV, InverseTransformPinToHole(data.slotV, plugX, holeX), data.disengageDist);
        }

        /// <summary>
        /// Updates guide joints normals if needed
        /// </summary>
        /// <param name="data"></param>
        public void WriteNormals(ref HoleFunnelConstraintData data)
        {
            if (!data.normalsDirty) return;
            data.normalsDirty = false;
            funnelH.WriteNormals(ref data.slotH);
            funnelV.WriteNormals(ref data.slotV);
        }
    }
}