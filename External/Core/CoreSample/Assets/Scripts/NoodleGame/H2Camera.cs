using NBG.Entities;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Noodles;
using Unity.Mathematics;
using UnityEngine;
using NBG.Core.Easing;

public enum CameraMode
{
    Walk=0,
    Gun=100,
    Vehicle=200,
    LargeVehicle=300
}

public struct CameraTargetState
{
    public CameraMode mode;
    public float walkSpeed; // changes camera range
    public bool grab;
    public Float3Ease extraOffset;
    public FloatEase distOffset;
}
public interface ICameraModeOverride : ICameraOverride
{
    CameraMode cameraMode { get; }
}
// implements a state machine that calculates camera settings appropriate for game state before passing it to NoodleCamera
public class H2Camera : ThirdPersonCamera
{

    [Tooltip("Base camera settings, will be overridden based on CameraTargetState")]
    private CameraConfig baseConfig = CameraConfig.defaults;
    [Tooltip("Flag that makes the camera follow ground when jumping")]
    public bool stabilizeJump = true;
    
    [Tooltip("Pitch range when standing, in degrees")]
    public float pitchRangeIdleDeg = 40;
    [Tooltip("Pitch range when moving, in degrees")]
    public float pitchRangeWalkDeg = 17;

    // state
    float walkToIdleTransitionUp;
    float walkToIdleTransitionDown;

    public void OnLateUpdate()
    {
        ref var target = ref EntityStore.GetComponentData<CameraTarget>(trackedEntity);
        ref var targetState = ref EntityStore.GetComponentData<CameraTargetState>(trackedEntity);
        OnLateUpdate(target, ref targetState);
    }

    public void OnLateUpdate(in CameraTarget target, ref CameraTargetState targetState)
    {

        var config = baseConfig;
        var walkTargetPhaseUp = Mathf.InverseLerp(.5f, 0, targetState.walkSpeed); // up range depends on walk speed - narrow when running to give better ground visibility
        var walkTargetPhaseDown = target.lookPitch < 0 ? walkToIdleTransitionUp : // when looking up, copy down range from upper (good climbing experience)
            (targetState.grab ? walkToIdleTransitionDown : 1); // when reaching don't adjust range to make predictable grab, otherwise use full range


        float walkToIdleTransitionSpeed = Time.deltaTime / .8f;
        walkToIdleTransitionUp = Mathf.MoveTowards(walkToIdleTransitionUp, walkTargetPhaseUp, walkToIdleTransitionSpeed);
        walkToIdleTransitionDown = Mathf.MoveTowards(walkToIdleTransitionDown, walkTargetPhaseDown, walkToIdleTransitionSpeed);

        config.pitchRangeDeg =  math.lerp(pitchRangeWalkDeg, pitchRangeIdleDeg, target.lookPitch < 0 ? walkToIdleTransitionUp: walkToIdleTransitionDown);

        var modeOverride = CameraOverrideList<ICameraModeOverride>.GetOverride(trackedEntity);
        var targetMode = modeOverride != null ? modeOverride.cameraMode : CameraMode.Walk;
        if (targetMode != targetState.mode)
            TransitionToMode(ref targetState, targetMode);
        config.targetOffset += targetState.extraOffset.Get(Time.time);
        config.distanceMap = config.distanceMap.OffsetDist(targetState.distOffset.Get(Time.time));
        if (!stabilizeJump) config.jumpTrackingLimit = config.fallTrackingLimit = 0;

        base.OnLateUpdate(target,config);
    }

    private void TransitionToMode(ref CameraTargetState targetState, CameraMode targetMode)
    {
        targetState.mode = targetMode;
        switch (targetMode)
        {
            case CameraMode.Walk:
                targetState.extraOffset.TransitionTo(Time.time, new float3(0, 0, 0), 1, EaseType.easeOutSine);
                targetState.distOffset.TransitionTo(Time.time, 0, 1, EaseType.easeOutSine);
                break;
            case CameraMode.Gun:
                targetState.extraOffset.TransitionTo(Time.time, new float3(0, .5f, 0), 1, EaseType.easeOutSine);
                targetState.distOffset.TransitionTo(Time.time, 0, 1, EaseType.easeOutSine);
                break;
            case CameraMode.Vehicle:
                targetState.extraOffset.TransitionTo(Time.time, new float3(0, 0, 0), 1, EaseType.easeOutSine);
                targetState.distOffset.TransitionTo(Time.time, 2, 1, EaseType.easeOutSine);
                break;
            case CameraMode.LargeVehicle:
                targetState.extraOffset.TransitionTo(Time.time, new float3(0, 0, 0), 1, EaseType.easeOutSine);
                targetState.distOffset.TransitionTo(Time.time, 10, 1, EaseType.easeOutSine);
                break;
            default:
                break;
        }
    }
}
