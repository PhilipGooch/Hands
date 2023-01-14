using Recoil;
using Unity.Mathematics;

namespace Noodles
{

    // describes ragdoll and input configuration when reaching for items
    public struct CarryReachInfo
    {
        public Aim aim;
        public HandReachInfo handL;
        public HandReachInfo handR;

    }

    public static class ReachInfoExtensions
    {
        public static ref HandReachInfo GetHand(ref this CarryReachInfo reach, bool left) =>
            ref left ? ref reach.handL : ref reach.handR;
        public static ref HandTargetInfo GetHand(ref this CarryTargetInfo target, bool left) =>
            ref left ? ref target.handL : ref target.handR;
    }

    public struct HandReachInfo
    {
        public bool left;
        public int handId;
        public float3 shoulderPos;
        public float3 targetPos;
        public float3 actualPos;
        public float3 worldPalmPos=>worldPalmX.pos;
        public float3 worldPalmDir=>math.mul(worldPalmX.rot,-re.forward); // rig is with negativeZ axis
        public RigidTransform worldPalmX; 
        public float3 relativePalmAnchor => worldPalmPos - actualPos;
        public float radius;

    }

    public struct Aim
    {
        public float pitch;
        public float pitch01;
        public float yaw;

        // aim steadyness
        public float moveMagnitude;
        public float yawVelocity;
        public float pitchVelocity;
        public float aimVecocity => math.length(new float2(yawVelocity, pitchVelocity));

        public static void ReadInput(in InputFrame inputFrame, ref Aim aim, float dt)
        {
            aim.pitchVelocity = (inputFrame.lookPitch - aim.pitch)/ dt;
            aim.yawVelocity = re.NormalizeAngle(inputFrame.lookYaw - aim.yaw) / dt;
            //aim.pitchVelocity = inputFrame.lookPitchVelocity;// (inputFrame.lookPitch - aim.pitch)/ dt;
            //aim.yawVelocity = inputFrame.lookYawVelocity;// (inputFrame.lookYaw - aim.yaw) / dt;
            aim.pitch = inputFrame.lookPitch;
            aim.pitch01 = inputFrame.lookPitch01;
            aim.yaw = inputFrame.lookYaw;
            aim.moveMagnitude = inputFrame.moveMagnitude;
        }
    }

}