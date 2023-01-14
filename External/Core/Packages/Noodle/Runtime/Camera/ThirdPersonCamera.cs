using NBG.Core.GameSystems;
using NBG.Entities;
using Recoil;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// Core 3rd person camera implementation:
// * follows CameraTarget's interpolated position
// * looks at CameraTarget's aim/pitch
// * input pitch remapping
// * pitch to distance mapping enabling different distance for looking up (camera near ground) and looking down (overview)
// * ground stabilization in jumps - follows last grounded position while ground in range
// * target tracking stabilization
// * camera collision with spring arm

namespace Noodles
{
    public struct CameraTarget
    {
        // target settings
        public float3 position;
        public float3 velocity;
        public bool grounded; // jump targeting

        // camera hints
        public float lookPitch;
        public float lookYaw;

        // camera shake
        public float shake;

        public void AddCameraShake(float amount, bool limited=true)
        {
            if (limited) shake += amount;
            else shake += (1 - shake) * amount;
        }
    }

    public class ThirdPersonCamera : MonoBehaviour ,ICamera
    {
        public Entity trackedEntity { get; set; }

        public struct CameraPos
        {
            public float3 target;
            public float pitch;
            public float yaw;
            public float dist;
            public float fovDeg;
            public quaternion rotation => quaternion.Euler(pitch, yaw, 0);
        }
        public Camera unityCamera => camera;

        public new Camera camera;

        [Tooltip("Camera layers used for wall depenetration")]
        public LayerMask collisionLayers = 1;

        bool lastValid;
        float lastGroundY; // ground level - jump stabilization state
        float3 lastTarget; // target position - smoothing state
        FloatSpring dist; // distancefrom target -  armspring state

        public float DistanceToRagdoll => dist.pos;

        public void OnLateUpdate(in CameraTarget cameraTarget, in CameraConfig cfg)
        {
            // collect overrides
            var velocityOverride = CameraOverrideList<ICameraVelocityOverride>.GetOverride(trackedEntity);
            var groundVelocity = velocityOverride != null ? velocityOverride.cameraVelocity : float3.zero;
            var shakeOverride = CameraOverrideList<ICameraShakeOverride>.GetOverride(trackedEntity);
            var shake = shakeOverride != null ? shakeOverride.cameraShake : 0;

            // assemble camera pos from target
            var pos = new CameraPos();
            pos.target = cameraTarget.position + cameraTarget.velocity * (Time.time - Time.fixedTime);
            pos.pitch = GetPitch(cameraTarget.lookPitch, cfg);
            pos.yaw = cameraTarget.lookYaw + math.radians(cfg.yawOffsetDeg);
            pos.fovDeg = cfg.fovDeg;
            pos.dist = cfg.distanceMap.PitchToDist(pos.pitch);


            if (!lastValid)
            {
                lastTarget = pos.target;
                lastGroundY = pos.target.y;
                dist = new FloatSpring(pos.dist);
                lastValid = true;
            }
            lastTarget += groundVelocity * Time.deltaTime;
            lastGroundY += groundVelocity.y * Time.deltaTime;

            pos.target = StabilizeJump(ref lastGroundY, pos.target, cameraTarget.grounded, Time.deltaTime, cfg);
            pos.target = SmoothTargetFollow(lastTarget, pos.target, cameraTarget.velocity, Time.deltaTime, cfg);
            lastTarget = pos.target;

            pos.target += cfg.targetOffset.RotateY(pos.yaw);
            pos.dist = ProcessWallCollisions(unityCamera, pos, ref dist, collisionLayers, Time.deltaTime, cfg);

            unityCamera.fieldOfView = pos.fovDeg;
            unityCamera.transform.rotation = pos.rotation;
            unityCamera.transform.position = pos.target - math.rotate(pos.rotation, new float3(0, 0, pos.dist));
            float visualShake = cameraTarget.shake + shake;
            if (visualShake > 0)
            {
                visualShake *= visualShake;
                unityCamera.transform.rotation *= Quaternion.Euler(new Vector3(this.shake(5, (float)visualShake, 0), this.shake(5, (float)visualShake, 1), this.shake(5, (float)visualShake, 2)));
            }
        }

        float shake(float max, float shake, float seedOffset)
        {
            return max * shake * noise.cnoise(new float2(Time.time * 500, seedOffset) * 2 - 1);
        }

        //TODO@TS: currently used by grab targeting, refactor so that grab does not rely on camera
        private CameraConfig cachedConfig = CameraConfig.defaults;
        public float GetPitch(float inputPitch, in CameraConfig cfg)
        {
            this.cachedConfig = cfg; // save a cache to enable GetPitch overload without config
            var pitch = inputPitch / 80 * cfg.pitchRangeDeg;
            pitch += math.radians(cfg.pitchOffsetDeg);
            return pitch;
        }
        public float GetPitch(float inputPitch) => GetPitch(inputPitch, cachedConfig);

        public virtual void NotifyTeleport(float3 offset)
        {
            //Debug.Log(Time.time.ToString("F5") + " " + Time.renderedFrameCount + " Camera Teleport updated" + lastTarget + " to " + (lastTarget + offset));
            lastTarget += offset;
            lastGroundY += offset.y;
        }

        public static float ProcessWallCollisions(Camera camera, CameraPos pos, ref FloatSpring dist, int layers, float dt, in CameraConfig cfg)
        {
            if (!CameraCast(camera, pos, .1f, layers, out float limit))
                limit = 100;
            return SpringArm(ref dist, pos.dist, limit, camera.nearClipPlane, dt, cfg);

        }

        public static bool CameraCast(Camera camera, CameraPos pos, float minDist, int layers, out float limit)
        {
            var nearClip = camera.nearClipPlane;
            var fovYHalfRad = math.radians(pos.fovDeg * 0.5f);
            var tangent = math.tan(fovYHalfRad);
            var clipV = tangent * nearClip;
            var clipH = clipV * camera.aspect;

            var rot = pos.rotation;
            var lookDir = math.rotate(rot, re.forward);
            RaycastHit hit;
            if (Physics.BoxCast(pos.target, new Vector3(clipH, clipV, 0), -lookDir, out hit, rot, pos.dist - nearClip, layers, QueryTriggerInteraction.Ignore))
            {
                limit = math.max(minDist, hit.distance) + nearClip;
                return true;
            }
            limit = 0;//Mathf.Max(minDist+nearClip,pos.dist);
            return false;
        }

        public static float SpringArm(ref FloatSpring dist, float target, float limit, float nearClipPlane, float dt, in CameraConfig cfg)
        {
            if (dist.pos <= target && dist.pos <= limit) // expand
                dist.Step(math.min(limit, target), cfg.springPeriodExtend, dt);
            else
                dist.Set(math.min(limit, target));

            return math.max(math.max(nearClipPlane, cfg.springMinDist), dist.pos);
        }
      

        public static float3 StabilizeJump(ref float lastGroundY, float3 newPos, bool grounded, float dt, in CameraConfig cfg)
        {
            if (cfg.fallTrackingLimit == 0 && cfg.jumpTrackingLimit == 0)
            {
                lastGroundY = newPos.y;
                return newPos;
            }
            var diff = newPos.y - lastGroundY;

            if (grounded && dt > 0) // re-base ground
                diff = re.MoveTowardsExp(diff, 0, cfg.groundCatchupHalflife, dt);

            // limit ground
            if (diff < cfg.fallTrackingLimit) diff = cfg.fallTrackingLimit;
            if (diff > cfg.jumpTrackingLimit) diff = cfg.jumpTrackingLimit;

            lastGroundY = newPos.y - diff;
            return newPos.SetY(lastGroundY);
        }

        public static float3 SmoothTargetFollow(float3 lastTarget, float3 target, float3 v, float dt, in CameraConfig cfg)
        {
            if (dt == 0) return lastTarget;
            if (cfg.targetCatchupHalflife < re.FLT_EPSILON) return target;
            var diff = target - lastTarget;
            if (math.length(diff) > 5) Debug.Log(Time.time.ToString("F5") + " " + Time.renderedFrameCount + " LARGE camera offset: " + math.length(diff) + " lastTarget: " + lastTarget + " newPos: " + target);


            // ALT0: basic decay without velocity
            //diff *= math.exp2(-dt / cfg.targetCatchupHalflife); // decay

            // ALT1: decay solved with velocity in diff equation
            // dx/dt = v + hx, x0=diff
            // x=v/h+ce^{-ht} 
            // c=diff-v/h
            // x=v/h+(X-v/h)e^{-ht}
            
            var h = -math.log(.5f)/ cfg.targetCatchupHalflife; // convert halflife to h

            diff -= v * dt; // undo integration of velocity
            var vh = v / h;
            diff = vh + (diff - vh) * math.exp(-dt * h);
            diff = re.Clamp(diff, cfg.maxTargetOffset);

            return target - diff;
        }
    }
    [UpdateInGroup(typeof(PhysicsAfterSolve))]
    public class CameraShakeCooldown : QuerySystem<CameraTarget>
    {
        public CameraShakeCooldown()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Noodles.GlobalJobData));
        }

        const float SHAKE_RELAX_TIME = 1f;
        public override void Execute(EntityReference entity)
        {
            ref var target = ref entity.GetComponentData<CameraTarget>();
            target.shake -= Recoil.World.main.dt / SHAKE_RELAX_TIME;
            target.shake = math.max(0, target.shake);
        }
    }

    
}