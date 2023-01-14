using Noodles;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using NBG.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;




// responsible for input and camera
public class NoodlePlayerController : PlayerControllerBase
{
    public static EntityArchetype playerArchetype;
    Noodle noodle;

    static ComponentTypeList types;
    public override void OnCreate()
    {
        entity = EntityStore.AddEntity();

        base.OnCreate(entity);
        if (!noodle) noodle = GetComponent<Noodle>();
        EntityStore.AddComponentObject(entity, noodle);

        noodle.OnCreate(entity);
        if(types.Length==0)
        {
            types = ComponentTypeList.Create();
            types.AddType<InputFrame>();
            types.AddType<CameraTarget>();
            types.AddType<CameraTargetState>();
        }
        EntityStore.AddComponents(entity, types);
        CameraOverrideList<ICameraModeOverride>.EnableOverride(entity);
        CameraOverrideList<ICameraShakeOverride>.EnableOverride(entity);
        CameraOverrideList<ICameraVelocityOverride>.EnableOverride(entity);

        EntityStore.GetComponentObject<Ball>(entity).collisionLayers = (int)NoodleLayers.CollideWithBall;
        var hands = GetComponentsInChildren<NoodleHand>();
        hands[0].collisionLayers = hands[1].collisionLayers = (int)NoodleLayers.Grabbable;
        hands[0].targetLayers = hands[1].targetLayers = (int)NoodleLayers.GrabTargets;
    }

    public override void Dispose()
    {
        EntityStore.RemoveComponentObject(entity, noodle);
        noodle.Dispose();
    }


    public void OnFixedUpdate()
    {
        if(noodle.rig.rootPosition.y<-20)
        {
            noodle.rig.Teleport(re.up * 50);
            CameraManager.Teleport(entity, re.up * 50);
        }
        ref var inputFrame = ref EntityStore.GetComponentData<InputFrame>(entity);
        noodle.OnFixedUpdate(ref inputFrame);
    }
    public void OnPostFixedUpdate()
    {
        
        noodle.PostFixedUpdate();
        ref var inputFrame = ref EntityStore.GetComponentData<InputFrame>(entity);
        InputManagerSystem.ResetButtons(ref inputFrame);
    }


}
