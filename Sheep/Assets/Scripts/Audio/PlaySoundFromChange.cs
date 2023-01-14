using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Reusable class that can play sounds from some form of value change
// Used to play sounds from moving hinges and changing activation values
[System.Serializable]
public class PlaySoundFromChange
{
    [SerializeField]
    AudioClip forwardMovementSound;
    [SerializeField]
    AudioClip backwardMovementSound;
    [SerializeField]
    float pitchShiftFromVelocity = 0f;
    [SerializeField]
    [Range(0f, 1f)]
    float speedForMaxPitch = 0.1f;
    [SerializeField]
    int soundsToPlayForFullChange = 10;
    [SerializeField]
    float basePitch = 1f;
    [SerializeField]
    float volumeShiftFromVelocity = 0f;
    [SerializeField]
    [Range(0f, 1f)]
    float speedForMaxVolume = 0.1f;
    [SerializeField]
    float baseVolume = 1f;

    float lastValue = 0f;
    float lastSoundValue = 0f;
    float minChangeToPlaySound = 0f;
    AttenuatedAudioPool audioPool;
    Transform transform;

    public void Initialize(float initialValue, AttenuatedAudioPool audioPool, Transform transform)
    {
        lastValue = initialValue;
        lastSoundValue = lastValue;
        minChangeToPlaySound = 1f / soundsToPlayForFullChange;
        // If we want to play a sound once, play it at the middle of the activation
        if (soundsToPlayForFullChange < 2)
        {
            minChangeToPlaySound = 0.5f;
        }
        this.audioPool = audioPool;
        this.transform = transform;
    }

    // Expects value from -1 to 1
    public void SetActivation(float newValue)
    {
        var delta = newValue - lastValue;
        var deltaFromLastSound = Mathf.Abs(newValue - lastSoundValue);
        if (deltaFromLastSound > minChangeToPlaySound)
        {
            var soundToPlay = delta > 0 ? forwardMovementSound : backwardMovementSound;
            if (soundToPlay == null)
            {
                soundToPlay = forwardMovementSound ?? backwardMovementSound;
            }
            var extraPitch = Mathf.Clamp01(Mathf.Abs(delta) / speedForMaxPitch) * pitchShiftFromVelocity;
            var extraVolume = Mathf.Clamp01(Mathf.Abs(delta) / speedForMaxVolume) * volumeShiftFromVelocity;
            var pitch = basePitch + extraPitch;
            var volume = baseVolume + extraVolume;
            audioPool.PlayOneShot(transform, soundToPlay, volume, pitch);
            lastSoundValue = newValue;
        }

        lastValue = newValue;
    }
}
