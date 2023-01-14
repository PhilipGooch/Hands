using System.Collections;
using UnityEngine;

namespace NBG.Audio
{
    public class UISFX : MonoBehaviour
    {
        [SerializeField] AudioClip[] buttonHighlight = default;
        [SerializeField] AudioClip buttonSelect = default;
        [SerializeField] AudioClip backScreen = default;
        [SerializeField] AudioClip tabSwitch = default;
        [SerializeField] AudioClip[] dioramaSlides = default;
        int dioramaSlidesIndex = 0;
        [SerializeField] AudioClip levelSelect = default;
        [SerializeField] AudioClip levelNext = default;
        [SerializeField] AudioClip gamePause = default;
        [SerializeField] AudioClip playerJoins = default;
        [SerializeField] AudioClip playerLeaves = default;

        [SerializeField] AudioSource audioSource = default;
        [SerializeField] AudioSource windAudioSource = default;
        [SerializeField] float droneVolume = 0.3f;

        public void PlayButtonHighlight()
        {
            audioSource.PlayOneShot(buttonHighlight[Random.Range(0, buttonHighlight.Length)], Random.Range(0.45f, 0.5f));
        }

        public void PlayButtonSelect()
        {
            audioSource.PlayOneShot(buttonSelect, 1f);
        }

        public void PlayBack()
        {
            audioSource.PlayOneShot(backScreen, 1f);
        }

        public void PlayTabSwitch()
        {
            audioSource.PlayOneShot(tabSwitch, 0.8f);
        }

        public void PlayDioramaSlide()
        {
            audioSource.PlayOneShot(dioramaSlides[dioramaSlidesIndex], Random.Range(0.5f, 0.6f));
            dioramaSlidesIndex = Mathf.Clamp(dioramaSlidesIndex++, 0, dioramaSlides.Length - 1);
        }

        public void PlayEnterLevel()
        {
            audioSource.PlayOneShot(levelSelect, 1);
        }

        public void PlayLevelNext()
        {
            audioSource.PlayOneShot(levelNext, 1);
        }

        public void PlayGamePause()
        {
            audioSource.PlayOneShot(gamePause, 0.6f);
        }

        public void PlayPlayerJoins()
        {
            audioSource.PlayOneShot(playerJoins, 0.5f);
        }

        public void PlayPlayerLeaves()
        {
            audioSource.PlayOneShot(playerLeaves, 0.5f);
        }

        public void PlayWind()
        {
            StartCoroutine(WindFadeTo(droneVolume, 4));
        }

        public void StopWind()
        {
            StartCoroutine(WindFadeTo(0, 4));
        }

        IEnumerator WindFadeTo(float targetVolume, float duration)
        {
            float startVolume = windAudioSource.volume;

            while (windAudioSource.volume != targetVolume)
            {
                windAudioSource.volume = Mathf.MoveTowards(windAudioSource.volume, targetVolume, Time.unscaledDeltaTime / duration);
                yield return null;
            }
        }
    }
}
