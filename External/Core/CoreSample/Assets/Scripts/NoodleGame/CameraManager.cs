using NBG.Core.GameSystems;
using NBG.Entities;
using Noodles;
using Recoil;
using System;
using System.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;


public class CameraManager : CameraManagerBase<H2Camera>
{
    public static void Teleport(Entity entity, float3 offset)
    {
        foreach (var cam in CameraManager.instance.cameras)
        {
            //if (!cam.isActiveAndEnabled) //TODO: decide if this check should exist
            //    continue;

            if (cam.trackedEntity == entity)
                cam.NotifyTeleport(offset);
        }
    }
}

[UpdateInGroup(typeof(LateUpdateSystemGroup))]
public class CameraManagerLateUpdate : GameSystem
{
    public CameraManagerLateUpdate()
    {
        ReadsData(typeof(Recoil.WorldJobData));
        WritesData(typeof(Noodles.GlobalJobData));
    }
    protected override void OnUpdate()
    {
        if (CameraManager.instance == null)
            return;

        foreach (var cam in CameraManager.instance.cameras)
        {
            if (!cam.isActiveAndEnabled)
                continue;

            cam.OnLateUpdate();
        }
    }
}

[UpdateInGroup(typeof(PhysicsAfterSolve))]
[UpdateBefore(typeof(CameraShakeCooldown))]
[AlwaysSynchronizeSystem]
public class CameraManagerNoodleToTarget : QuerySystem<CameraTarget>
{
    public CameraManagerNoodleToTarget()
    {
        ReadsData(typeof(Recoil.WorldJobData));
        WritesData(typeof(Noodles.GlobalJobData));
    }

    public override void Execute(EntityReference entity)
    {
        ref var target = ref entity.GetComponentData<CameraTarget>();
        var noodle = entity.GetComponentObject<Noodle>();
        target.velocity = noodle.rig.velocity;
        target.position= noodle.rig.position;
        target.grounded = entity.GetComponentData<NoodleData>().grounded;
    }
}

[BurstCompile]
[UpdateInGroup(typeof(LateUpdateSystemGroup))]
[UpdateBefore(typeof(CameraManagerLateUpdate))]
public class CameraManagerInputFrameToTarget : JobQuerySystem<CameraTarget, CameraManagerInputFrameToTarget.ExecuteImpl>
{
    public CameraManagerInputFrameToTarget()
    {
        ReadsData(typeof(Recoil.WorldJobData));
        WritesData(typeof(Noodles.GlobalJobData));
    }

    public struct ExecuteImpl : IExecutyEntity
    {
        public void ExecuteEntity(in EntityReference entity)
        {
            ref var target = ref entity.GetComponentData<CameraTarget>();
            ref var inputFrame = ref entity.GetComponentData<InputFrame>();
            target.lookPitch = inputFrame.lookPitch;
            target.lookYaw = inputFrame.lookYaw;

            ref var targetState = ref entity.GetComponentData<CameraTargetState>();
            targetState.walkSpeed = inputFrame.moveMagnitude;
            targetState.grab = inputFrame.grabL || inputFrame.grabR;
        }
    }
}
