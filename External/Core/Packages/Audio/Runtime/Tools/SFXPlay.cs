using System.Collections.Generic;
using UnityEngine;

namespace NBG.Audio
{
    public class SFXPlay : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource = default;
        [SerializeField] List<AudioClip> audioClips = default;
        [Range(0, 1)] [SerializeField] float pitchVariation = 0;

        public void PlayClipOverride()
        {
            audioSource.clip = audioClips[Random.Range(0, audioClips.Count - 1)];
            audioSource.Play();
        }

        public void PlayClip()
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = audioClips[Random.Range(0, audioClips.Count - 1)];
                audioSource.pitch = Random.Range(1 - pitchVariation, 1 + pitchVariation);
                audioSource.Play();
            }
        }
    }
}
