using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AttenuatedAudioPool))]
public class SoundFromHinge : MonoBehaviour
{
    [SerializeField]
    PlaySoundFromChange soundPlayer;

    HingeJoint joint;
    AttenuatedAudioPool audioPool;
    float operationRange;

    private void Awake()
    {
        joint = GetComponent<HingeJoint>();
        audioPool = GetComponent<AttenuatedAudioPool>();

        SetupOperationRange();
        soundPlayer.Initialize(GetCurrentProgress(), audioPool, transform);
    }

    void SetupOperationRange()
    {
        if (joint.useLimits)
        {
            var min = joint.limits.min;
            var max = joint.limits.max;
            operationRange = Mathf.DeltaAngle(max, min);
        }
        else
        {
            operationRange = 360f;
        }
    }

    float GetCurrentProgress()
    {
        var currentAngle = joint.angle;
        if (joint.useLimits)
        {
            currentAngle += joint.limits.min;
        }
        
        currentAngle %= operationRange;
        return currentAngle / operationRange;
    }

    private void FixedUpdate()
    {
        soundPlayer.SetActivation(GetCurrentProgress());
    }
}
