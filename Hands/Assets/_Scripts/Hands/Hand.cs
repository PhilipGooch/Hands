using System;
using System.Collections.Generic;
using UnityEngine;
using VR.System;

public class Hand : MonoBehaviour
{
    [SerializeField]
    HandPositions handPositions;
    [SerializeField]
    HandVisuals handVisuals;

    public Hand otherHand;

    public float Trigger => VRSystem.Instance.ReadTriggerValue(handDirection);
    public float Grab => VRSystem.Instance.GetGrabAmount(handDirection);
    public Vector2 MoveDir => VRSystem.Instance.ReadMoveInput(handDirection);
    public HandPositions HandPositions => handPositions;

    readonly Dictionary<HandInputType, bool> fixedUpdateHandInputs = new();

    public HandDirection handDirection = HandDirection.Left;

    public Vector3 pos;
    public Quaternion rot;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public static event Action<Hand, float> OnHandFreeTrigger;

    public bool GetInput(HandInputType inputType)
    {
        if (Time.inFixedTimeStep)
        {
            if (fixedUpdateHandInputs.ContainsKey(inputType))
            {
                return fixedUpdateHandInputs[inputType];
            }
            return false;
        }
        else
        {
            return ReadInputFromDevice(inputType);
        }
    }

    bool ReadInputFromDevice(HandInputType inputType)
    {
        var result = VRSystem.Instance.ReadInputFromDevice(handDirection, inputType);
        return result;
    }

    public void ReadPose(Vector3 vrPos, Quaternion vrRot, Vector3 vrVelocity, Vector3 vrAngularVelocity)
    {
        if (!vrPos.IsFinite()) vrPos = pos;

        pos = vrPos;
        rot = vrRot;
        velocity = vrVelocity;
        angularVelocity = vrAngularVelocity;
    }

    void Update()
    {
        SetInputsForFixedUpdate();
    }

    static HandInputType[] cachedHandInputTypes;

    void SetInputsForFixedUpdate()
    {
        if (cachedHandInputTypes == null)
        {
            // This generates garbage, cache the values to avoid it
            cachedHandInputTypes = (HandInputType[])Enum.GetValues(typeof(HandInputType));
        }

        foreach (HandInputType inputType in cachedHandInputTypes)
        {
            var realInput = ReadInputFromDevice(inputType);
            if (fixedUpdateHandInputs.ContainsKey(inputType))
            {
                fixedUpdateHandInputs[inputType] = fixedUpdateHandInputs[inputType] || realInput;
            }
            else
            {
                fixedUpdateHandInputs[inputType] = realInput;
            }
        }
    }

    public void FlushFixedUpdateInputs()
    {
        fixedUpdateHandInputs.Clear();
    }

    public void OnLateUpdate()
    {
        handVisuals.UpdateVisuals(pos, rot);
    }

    public void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude)
    {
        VRSystem.Instance.Vibrate(secondsFromNow, duration, vibrationFrequency, amplitude, handDirection);
    }

    public void OnFixedStep()
    {
        OnHandFreeTrigger?.Invoke(this, Trigger);
    }
}


