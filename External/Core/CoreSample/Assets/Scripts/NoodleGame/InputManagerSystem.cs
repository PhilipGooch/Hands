using NBG.Core.GameSystems;
using NBG.Entities;
using Noodles;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


[UpdateInGroup(typeof(UpdateSystemGroup))]
public class InputManagerSystem : GameSystem
{
    public struct InputSettings
    {
        public float controllerSensitivityV;
        public float controllerSensitivityH;
        public static InputSettings DefaultConfig =>
            new InputSettings()
            {
                controllerSensitivityV = 1.25f,
                controllerSensitivityH = 1.25f,
            };
    }

    protected override void OnUpdate()
    {
        var results = EntityStore.Query<InputFrame>().Execute();
        for (int index = 0; index < results.count; index++)
        {
            ref var inputFrame = ref results.GetComponentData<InputFrame>(index);

            var controls = results.GetComponentObject<UnityPlayerControls>(index, optional:true);
            //No controls? -> Input frame will be updated from outside (example: Networking)
            if (controls == null)
                continue;
            ReadInput(controls, ref inputFrame, InputSettings.DefaultConfig, Time.unscaledDeltaTime);
        }
    }

    public static void ResetButtons(ref InputFrame inputFrame)
    {
        inputFrame.jump = false;
        inputFrame.playEmote0 = inputFrame.playEmote1 = inputFrame.playEmote2 = inputFrame.playEmote3 = false;
    }

    private static void ReadInput(UnityPlayerControls controls, ref InputFrame inputFrame, in InputSettings config, float dt)
    {
        var oldYaw = inputFrame.lookYaw;
        var oldPitch = inputFrame.lookPitch;
        var player = controls.Player;

        // read Holding buttons

        //inputFrame.playDead |= player.PlayDead.ReadValue<float>() >= InputSystem.settings.defaultButtonPressPoint;
        //inputFrame.grabL |= player.GrabLeft.ReadValue<float>() >= InputSystem.settings.defaultButtonPressPoint;
        //inputFrame.grabR |= player.GrabRight.ReadValue<float>() >= InputSystem.settings.defaultButtonPressPoint;
        ReadHold(ref inputFrame.playDead, player.PlayDead);
        ReadHold(ref inputFrame.grabL, player.GrabLeft);
        ReadHold(ref inputFrame.grabR, player.GrabRight);

        // read pressed buttons

        //this is reverted from GetButtonDown after discussion about BUG-639
        inputFrame.jump |= player.Jump.WasPressedThisFrame();

        //inputFrame.holdJump = player.GetButton(RewiredConsts.Action.HoldJump);
        //inputFrame.actionButton = player.Action.ReadValue<bool>();
        //Emotes
        inputFrame.playEmote0 |= player.Emote0.WasPressedThisFrame();
        inputFrame.playEmote1 |= player.Emote1.WasPressedThisFrame();
        inputFrame.playEmote2 |= player.Emote2.WasPressedThisFrame();
        inputFrame.playEmote3 |= player.Emote3.WasPressedThisFrame();


        Vector2 moveDir = Vector2.ClampMagnitude(player.Move.ReadValue<Vector2>(), 1);

        inputFrame.moveYaw = math.atan2(moveDir.x, moveDir.y);
        inputFrame.moveMagnitude = Mathf.Clamp01(moveDir.magnitude);

        // snap to straight direction
        inputFrame.moveYaw = math.degrees(inputFrame.moveYaw);
        if (inputFrame.moveYaw > -45 && inputFrame.moveYaw < 0) inputFrame.moveYaw = (inputFrame.moveYaw + 5) / 40 * 45;
        if (inputFrame.moveYaw < +45 && inputFrame.moveYaw > 0) inputFrame.moveYaw = (inputFrame.moveYaw - 5) / 40 * 45;
        inputFrame.moveYaw = math.radians(inputFrame.moveYaw);

        //var activeController = rePlayer.controllers.GetLastActiveController();
        //var activeType = activeController == null ? ControllerType.Joystick : activeController.type;

        var look = player.Look.ReadValue<Vector2>();

        //if (activeType == ControllerType.Joystick)
        //{
        //var runfactor = inputFrame.run ? controllerRunFactorH : 1;
        var runfactor = 1;
        inputFrame.lookYaw += look.x * dt * math.radians(120) * config.controllerSensitivityH * runfactor;
        //}
        //else if (activeType == ControllerType.Mouse)
        //{
        //    var runfactor = inputFrame.run ? mouseRunFactorH : 1;
        //    inputFrame.lookYaw += player.GetAxis(RewiredConsts.Action.Look_Horizontal) * mouseSensitivityH * runfactor;
        //}
        //else
        //{
        //    //Not handled. I leave this in for debugging.
        //    //Debug.Log("activeControllerType unhandled: " + Enum.GetName(typeof(ControllerType), activeType));
        //}

        //inputFrame.run = inputFrame.run && inputFrame.moveMagnitude >= .7f && Mathf.Abs(inputFrame.moveYaw) < 25;
        //if (activeType == ControllerType.Joystick)
        //{

        var controllerPitch = look.y;
        controllerPitch = GetAxisNoJitter(controllerPitch, ref controls.controllerPitchJitterArray);

        var targetPitch = -InputFrame.PITCH_RANGE * controllerPitch * Mathf.Sign(config.controllerSensitivityV);
        var fast = targetPitch * inputFrame.lookPitch < 0 // opposite from target
                || Mathf.Abs(targetPitch) > Mathf.Abs(inputFrame.lookPitch); // or greater in the same direction

        var fastSpeed = inputFrame.grabL || inputFrame.grabR ? Mathf.Abs(controllerPitch) : 1;
        var slowSpeed = inputFrame.grabL || inputFrame.grabR ? 0f : 0.25f; // slow speed super slow when grabbed
        var speedMultiplier = fast ? fastSpeed : slowSpeed;
        speedMultiplier *= Mathf.Abs(config.controllerSensitivityV);

        var deltaPitch = Mathf.Abs(targetPitch - inputFrame.lookPitch);
        inputFrame.lookPitch = Mathf.MoveTowards(inputFrame.lookPitch, targetPitch, speedMultiplier * math.radians(30 + Compress(math.degrees(deltaPitch), 45, .5f) * 5) * dt); // a bit slower when gets over 45deg
        //}
        //else if (activeType == ControllerType.Mouse)
        //{
        //    var pitchValue = player.GetAxis(RewiredConsts.Action.Look_Vertical) * mouseSensitivityV;
        //    inputFrame.lookPitch -= pitchValue; // no limiting
        //}
        //else
        //{
        //    //Not handled. I leave this in for debugging.
        //    //Debug.Log("activeControllerType unhandled: " + Enum.GetName(typeof(ControllerType), activeType));
        //}

        inputFrame.lookPitch = Mathf.Clamp(inputFrame.lookPitch, -InputFrame.PITCH_RANGE, InputFrame.PITCH_RANGE);
        //FIXME: Save this in user profiles
        inputFrame.lookYaw = re.NormalizeAngle(inputFrame.lookYaw);

        inputFrame.lookYawVelocity = re.NormalizeAngle(inputFrame.lookYaw-oldYaw)/dt;
        inputFrame.lookPitchVelocity = (inputFrame.lookPitch-oldPitch)/dt;


    }
    static void ReadHold(ref bool value, InputAction action)
    {
        value = action.ReadValue<float>() >= InputSystem.settings.defaultButtonPressPoint;
    }

    static float Compress(float vec, float threshold, float power)
    {
        var mag = Mathf.Abs(vec);
        if (mag > threshold)
            return vec / mag * Mathf.Pow(mag / threshold, power) * threshold;
        else
            return vec;
    }
    private static float GetAxisNoJitter(float axisThisFrame, ref float [] controllerHistory)
    {
        if(controllerHistory==null)
        {
            var framesToCache = Application.targetFrameRate / 3;
            framesToCache = math.clamp(framesToCache, 10, 20);
            controllerHistory = new float[framesToCache];
        }
        Array.Copy(controllerHistory, 0, controllerHistory, 1, controllerHistory.Length - 1);
        controllerHistory[0] = axisThisFrame;
        //Calculate average change and delta
        float axisSum = 0;
        float axisMin = float.PositiveInfinity;
        float axisMax = float.NegativeInfinity;
        for (int i = 1; i < controllerHistory.Length; i++)
        {
            axisSum += controllerHistory[i];
            if (controllerHistory[i] < axisMin)
                axisMin = controllerHistory[i];
            if (controllerHistory[i] > axisMax)
                axisMax = controllerHistory[i];
        }
        var histLen = controllerHistory.Length - 1;
        //float axisSpread = Math.Abs(axisMax - axisMin);
        float axisAvg = axisSum / histLen;
        float deltaThisFrame = math.abs(axisThisFrame - axisAvg);

        float ret = 0;
        if (deltaThisFrame > 0.04f) ret = axisThisFrame;
        else ret = axisAvg;

        /*Debug.Log("in " + axisThisFrame.y + " delta: " + deltaThisFrame.y + " spread " + axisSpread.y + " avg: " + axisAvg.y
				+ " value " + ret.y + "\nhist: " + string.Join(", ", controllerHistory.Select(x => "" + x.y).ToArray()));*/
        return ret;
    }
}

[UpdateInGroup(typeof(UpdateSystemGroup))]
public class PlayerManagerDebugSpawner : GameSystem
{
    protected override void OnUpdate()
    {
        if (PlayerManager.instance == null)
            return;
        if (Keyboard.current == null)
            return;
        if (Keyboard.current.digit0Key.wasPressedThisFrame)
            PlayerManager.instance.AddPlayer(new Vector3(0, 10, 0) + UnityEngine.Random.insideUnitSphere * 10);
        if (Keyboard.current.digit9Key.wasPressedThisFrame)
            PlayerManager.instance.StartCoroutine(FillPlayers());
    }
    private IEnumerator FillPlayers()
    {

        while (PlayerManager.instance.players.Count < PlayerManager.instance.maxPlayers)
        {
            PlayerManager.instance.AddPlayer(new Vector3(0, 10, 0) + UnityEngine.Random.insideUnitSphere * 10);
            yield return null;
            yield return null;
            yield return null;
            //yield return new WaitForSeconds(.25f);
        }

    }
}
