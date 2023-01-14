using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Attenuation))]

public class LoopingAttenuatedAudioPool : BaseAudioPool
{
    private Attenuation attenuation;
    Dictionary<Transform, (Transform audioSourceTransform, AudioSource source)> audioSourceFollowPairs = new Dictionary<Transform, (Transform audioSourceTransform, AudioSource source)> ();

    private void Awake()
    {
        attenuation = GetComponent<Attenuation>();
    }

    void Update()
    {
        foreach (var pair in audioSourceFollowPairs)
        {
            pair.Value.audioSourceTransform.position = pair.Key.position;
        }
    }

    public void PlayLoopingFollow(Transform target, AudioClip clip, float volume = 1, float pitch = 1)
    {
        var source = PlayLooping(target, clip, attenuation, volume, pitch);

        if (source != null)
        {
            audioSourceFollowPairs.Add(target,(source.transform, source));
        }
    }

    public void ReleaseAudioSource(Transform target)
    {
        if (audioSourceFollowPairs.ContainsKey(target))
        {
            ReleaseAudioSource(audioSourceFollowPairs[target].source);
            audioSourceFollowPairs.Remove(target);
        }
    }
}
