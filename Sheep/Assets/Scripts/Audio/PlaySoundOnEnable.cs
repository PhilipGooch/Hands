using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Attenuation))]
public class PlaySoundOnEnable : MonoBehaviour
{
    [SerializeField]
    AudioClip clip;
    [SerializeField]
    float volume = 1f;
    [SerializeField]
    float pitch = 1f;
    [SerializeField]
    Attenuation attenuation;

    private void OnEnable()
    {
        AudioManager.instance.PlayOneShotSfx(transform.position, clip, attenuation, volume, pitch);
    }

    private void OnValidate()
    {
        if (!attenuation)
        {
            attenuation = GetComponent<Attenuation>();
        }
    }
}
