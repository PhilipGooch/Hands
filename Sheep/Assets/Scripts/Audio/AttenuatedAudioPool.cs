using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Attenuation))]
public class AttenuatedAudioPool : BaseAudioPool
{
    private Attenuation attenuation;

    //public float volumeDb = 0;
    //public float falloffStart = 1;
    //public float maxDistance = 25;
    //public float falloffPower = .7f;
    //public float lpStart = 2f;
    //public float lpPower = .7f;

    //public AudioSource prefab;
    private void Awake()
    {
        attenuation = GetComponent<Attenuation>();

        //pool.Enqueue(prefab);

        //prefab.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CalculateAttenuation.VolumeFalloffFromTo(1, 0, falloffStart, maxDistance, falloffPower));
        //prefab.maxDistance = maxDistance;
        //var lowPass = prefab.GetComponent<AudioLowPassFilter>();
        //    lowPass.customCutoffCurve = CalculateAttenuation.LowPassFalloff(lpStart / maxDistance, lpPower);
    }

    public void PlayOneShot(Transform transform, AudioClip clip, float volume=1, float pitch =1)
    {
        PlayOneShot(transform, clip, attenuation, volume, pitch);

    }

    public void PlayOneShot(Vector3 pos, AudioClip clip, float volume = 1, float pitch = 1)
    {
        PlayOneShot(pos, clip, attenuation, volume, pitch);
    }

}
