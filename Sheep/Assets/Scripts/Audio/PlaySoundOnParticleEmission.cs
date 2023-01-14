using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlaySoundOnParticleEmissionData
{
    [SerializeField]
    public List<AudioClip> onParticlesStartSounds;
    [SerializeField]
    public List<AudioClip> onParticlesEndSounds;
    [SerializeField]
    public List<AudioClip> onParticlesIncreaseSounds;
    [SerializeField]
    public List<AudioClip> onParticlesDecreaseSounds;

    [SerializeField]
    public float minParticlesStartSoundInterval = 0.1f;
    [SerializeField]
    public float minParticlesEndSoundInterval = 0.1f;
    [SerializeField]
    public float minParticlesIncreaseSoundInterval = 0.1f;
    [SerializeField]
    public float minParticlesDecreaseSoundInterval = 0.1f;
}

[RequireComponent(typeof(Attenuation))]
[Serializable]
public class PlaySoundOnParticleEmission : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField]
    float volume = 1f;
    [Range(0f, 3f)]
    [SerializeField]
    float pitch = 1f;

    [SerializeField]
    PlaySoundOnParticleEmissionData data;

    float lastFireOnStartTimestamp;
    float lastFireOnEndTimestamp;
    float lastFireOnIncreaseTimestamp;
    float lastFireOnDecreaseTimestamp;

    float lastFrameParticlesCount;

    ParticleSystem particleSystem;
    Attenuation attenuation;


    private void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        attenuation = GetComponent<Attenuation>();

        lastFrameParticlesCount = particleSystem.particleCount;
    }


    private void Update()
    {
        var currentCount = particleSystem.particleCount;
        float time = Time.timeSinceLevelLoad;
        if (data.onParticlesStartSounds.Count > 0 && lastFrameParticlesCount == 0 && currentCount > 0 && time - lastFireOnStartTimestamp > data.minParticlesStartSoundInterval)
        {
            //on particles start

            PlaySound(RandomSoundFromPool(data.onParticlesStartSounds));
            lastFireOnStartTimestamp = time;
        }
        else if (data.onParticlesEndSounds.Count > 0 && lastFrameParticlesCount > 0 && currentCount == 0 && time - lastFireOnEndTimestamp > data.minParticlesEndSoundInterval)
        {
            //on particles end

            PlaySound(RandomSoundFromPool(data.onParticlesEndSounds));
            lastFireOnEndTimestamp = time;
        }
        else if (data.onParticlesIncreaseSounds.Count > 0 && currentCount > lastFrameParticlesCount && time - lastFireOnIncreaseTimestamp > data.minParticlesIncreaseSoundInterval)
        {
            //increasing

            PlaySound(RandomSoundFromPool(data.onParticlesIncreaseSounds));
            lastFireOnIncreaseTimestamp = time;
        }
        else if (data.onParticlesDecreaseSounds.Count > 0 && currentCount < lastFrameParticlesCount && time - lastFireOnDecreaseTimestamp > data.minParticlesDecreaseSoundInterval)
        {
            //decreasing
            PlaySound(RandomSoundFromPool(data.onParticlesDecreaseSounds));
            lastFireOnDecreaseTimestamp = time;
        }

        lastFrameParticlesCount = currentCount;
    }

    void PlaySound(AudioClip clip)
    {
        AudioManager.instance.PlayOneShotSfx(transform.position, clip, attenuation, volume, pitch);

    }

    AudioClip RandomSoundFromPool(List<AudioClip> pool)
    {
        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }
}
