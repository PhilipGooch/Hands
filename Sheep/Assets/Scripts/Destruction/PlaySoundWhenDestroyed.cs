using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Attenuation))]
public class PlaySoundWhenDestroyed : MonoBehaviour
{
    [SerializeField]
    AudioClip clip;
    [SerializeField]
    float volume = 1f;
    [SerializeField]
    float pitch = 1f;
    DestructibleObject target;
    Attenuation attenuation;
    // Start is called before the first frame update
    void Awake()
    {
        target = GetComponent<DestructibleObject>();
        attenuation = GetComponent<Attenuation>();
    }

    private void OnEnable()
    {
        target.onDestroyed += PlaySound;
    }

    private void OnDisable()
    {
        target.onDestroyed -= PlaySound;
    }

    void PlaySound()
    {
        AudioManager.instance.PlayOneShotSfx(transform.position, clip, attenuation, volume, pitch);
    }
}
