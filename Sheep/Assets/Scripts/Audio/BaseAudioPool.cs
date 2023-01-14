using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class BaseAudioPool : MonoBehaviour
{
    public int voices = 16;
    public AudioMixerGroup mixerGroup;

    Queue<AudioSource> pool = new Queue<AudioSource>();
    List<AudioSource> playingSources = new List<AudioSource>();

    public void PlayOneShot(Transform transform, AudioClip clip, Attenuation attenuation, float volume = 1, float pitch = 1)
    {
        var source = GetAudioSource();
        if (source == null) return;
        source.pitch = pitch;
        source.transform.SetParent(transform, false);
        source.transform.localPosition = Vector3.zero;
        attenuation.Apply(source, applyVolume: false);
        volume *= Attenuation.DBToValue(attenuation.volumeDb);
        source.PlayOneShot(clip, volume);
    }

    public void PlayOneShot(Vector3 pos, AudioClip clip, Attenuation attenuation, float volume = 1, float pitch = 1)
    {
        var source = GetAudioSource();
        if (source == null) return;
        source.pitch = pitch;
        source.transform.position = pos;
        attenuation.Apply(source, applyVolume: false);
        volume *= Attenuation.DBToValue(attenuation.volumeDb);
        source.PlayOneShot(clip, volume);
    }

    public AudioSource PlayLooping(Transform transform, AudioClip clip, Attenuation attenuation, float volume = 1, float pitch = 1)
    {
        var source = GetAudioSource();
        if (source == null) return null;

        source.pitch = pitch;

        attenuation.Apply(source, applyVolume: false);
        volume *= Attenuation.DBToValue(attenuation.volumeDb);

        source.clip = clip;
        source.volume = volume;
        source.loop = true;
        source.Play();

        return source;
    }

    public AudioSource GetAudioSource()
    {
        while (pool.Count > 0)
        {
            var result = pool.Dequeue(); // source got destroyed
            if (result != null)
            {
                ResetAudioSource(result);
                return result;
            }
        }

        for (int i = 0; i < playingSources.Count; i++)
        {
            var source = playingSources[i];
            if (source == null) // source got destroyed
            {
                playingSources.RemoveAt(i);
                i--;
                continue;
            }
            if (!source.isPlaying)
            {

                //playingSources.RemoveAt(i);
                ResetAudioSource(source);
                return source;
            }
        }

        if (playingSources.Count >= voices)
            return null;

        var go = new GameObject("pooled audio");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        var lp = go.AddComponent<AudioLowPassFilter>();
        src.outputAudioMixerGroup = mixerGroup;

        //var src = Instantiate(prefab, transform);
        playingSources.Add(src);
        ResetAudioSource(src);
        return src;
    }

    public void ReleaseAudioSource(AudioSource source)
    {
        pool.Enqueue(source);
        ResetAudioSource(source);
        source.transform.SetParent(transform, false);
    }

    void ResetAudioSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = 1;
        source.pitch = 1;
    }
}
