using NBG.Core.GameSystems;
using Noodles;
using Recoil;
using Recoil.Util;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

public class PlayerManager : UnityInputPlayerManager<NoodlePlayerController, UnityPlayerControls>
{
    public override UnityPlayerControls CreateControls(bool mainPlayer)
    {
        var controls = new UnityPlayerControls();
        controls.Player.Enable();
        if (mainPlayer)
            controls.devices = InputSystem.devices;
        return controls;
    }
   
}

[UpdateInGroup(typeof(PhysicsBeforeSolve))]
[UpdateBefore(typeof(NoodleExecuteSystem))]
[AlwaysSynchronizeWorld]
public class PlayerManagerFixedUpdate : GameSystem
{
    public PlayerManagerFixedUpdate()
    {
        WritesData(typeof(Noodles.GlobalJobData));
    }

    protected override void OnUpdate()
    {
        if (PlayerManager.instance == null)
            return;
        Profiler.BeginSample("OnFixedUpdate");
        var players = PlayerManager.instance.players;
        for (int i = 0; i < players.Count; i++)
            players[i].OnFixedUpdate();
        Profiler.EndSample();
    }
}

[UpdateInGroup(typeof(PhysicsAfterSolve))]
[UpdateBefore(typeof(OnPhysicsAfterSolveSystem))]
public class PlayerManagerPostFixedUpdate : GameSystem
{
    protected override void OnUpdate()
    {
        if (PlayerManager.instance == null)
            return;
        var players = PlayerManager.instance.players;
        Profiler.BeginSample("OnPostFixedUpdate");
        for (int i = 0; i < players.Count; i++)
            players[i].OnPostFixedUpdate();
        Profiler.EndSample();
    }
}

