using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public LoopingAttenuatedAudioPool firePool;
    public AttenuatedAudioPool stepPool;
    public AttenuatedAudioPool voicePool;
    public AttenuatedAudioPool grabbedPool;
    public AttenuatedAudioPool passPool;
    public BaseAudioPool sfxSoundPool;
    public AudioClip[] fire;
    public AudioClip[] steps;
    public AudioClip[] voices;
    public AudioClip[] pass;
    public AudioClip success;
    public AudioClip collect;
    public AudioClip uiClickPositive;
    public AudioClip uiClickNegative;

    public AudioMixer mainMixer;

    AudioSource audioSource;
    bool active = true;

    public static AudioManager instance;
    private void OnEnable()
    {
        if (instance == null)
            instance = this;

        audioSource = GetComponent<AudioSource>();
        SetMasterLevel(GameSettings.Instance.masterVolume.Value);
    }

    public void SetMasterLevel(float level)
    {
        mainMixer.SetFloat("MasterVolume", LogMap(level / 20f, 0, 1, -80, -24, 0));
    }
    float LogMap(float value, float sourceLow, float sourceHigh, float targetFrom, float targetCenter, float targetTo)
    {
        var pow = Mathf.Log((targetCenter - targetFrom) / (targetTo - targetFrom), 0.5f);
        return Mathf.Lerp(targetFrom, targetTo, Mathf.Pow(Mathf.InverseLerp(sourceLow, sourceHigh, value), pow));
    }
    public void PlayFire(Transform target)
    {
        if (active)
        {
            firePool.PlayLoopingFollow(target, fire[Random.Range(0, fire.Length)], 1, 1);
        }
    }
    public void ReleaseFire(Transform target)
    {
        firePool.ReleaseAudioSource(target);
    }

    public void ReleaseAllLoopingSoundsForTarget(Transform target)
    {
        ReleaseFire(target);
    }

    public void PlayStep(Vector3 pos, float pitch)
    {
        PlayAudioPool(stepPool, pos, steps[Random.Range(0, steps.Length)], 1, pitch);
    }

    public void PlayVoice(Transform transform, float volume)
    {
        PlayAudioPool(voicePool, transform, voices[Random.Range(0, voices.Length)], volume);
    }

    public void PlayGrabbedVoice(Transform transform)
    {
        PlayAudioPool(voicePool, transform, voices[Random.Range(0, voices.Length)]);
    }

    public void PlayPass(Vector3 pos, int sheep)
    {
        int snd = Mathf.Clamp(sheep - 1, 0, pass.Length);
        PlayAudioPool(passPool, pos, pass[snd]);
        //var source = passPool.GetAudioSource();
        //if (source == null) return;
        //source.transform.position = pos;
        //source.PlayOneShot(pass[snd], Attenuation.DBToValue(passPool.volumeDb));
    }

    void PlayAudioPool(AttenuatedAudioPool pool, Vector3 pos, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (active)
        {
            pool.PlayOneShot(pos, clip, volume, pitch);
        }
    }

    void PlayAudioPool(AttenuatedAudioPool pool, Transform transform, AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (active)
        {
            pool.PlayOneShot(transform, clip, volume, pitch);
        }
    }


    public void PlaySuccess()
    {
        PlayOneShot(success, .5f);
    }
    public void PlayCollect()
    {
        PlayOneShot(collect, .5f);
    }
    public void PlayUIClickPositive()
    {
        PlayOneShot(uiClickPositive, .5f);
    }
    public void PlayUIClickNegative()
    {
        PlayOneShot(uiClickNegative, .5f);
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (active)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayOneShotSfx(Vector3 pos, AudioClip clip, Attenuation attenuation, float volume = 1f, float pitch = 1f)
    {
        if (active)
        {
            sfxSoundPool.PlayOneShot(pos, clip, attenuation, volume, pitch);
        }
    }

    public void SetAudioActive(bool active)
    {
        this.active = active;
    }
}
