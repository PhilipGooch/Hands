using NBG.Core;
using System.Collections;
using UnityEngine;

namespace NBG.Audio
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager instance;
        public AudioClip[] songs;
        AudioSource audioSource;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            if (instance != this)
            {
                Destroy(this);
                return;
            }
            audioSource = GetComponent<AudioSource>();
        }

        public void PlayMusic(string clipName)
        {
            if (audioSource.clip?.name == clipName && audioSource.isPlaying) 
                return;
            StartCoroutine(PlaySong(clipName));
        }

        public IEnumerator PlaySong(string clipName)
        {
            AudioClip clip = null;
            for (int i = 0; i < songs.Length; i++)
            {
                if (songs[i].name == clipName)
                {
                    clip = songs[i];
                    break;
                }
            }

            if (clip != null)
            {
                if (audioSource.isPlaying)
                {
                    StartFade(FadeTo(0, 2));
                    while (isFading)
                        yield return null;
                }
                audioSource.volume = 1;
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        bool isFading => fadeHandle.Status == CoroutineStatus.Running;
        public void Stop()
        {
            audioSource.Stop();
        }

        IEnumerator FadeTo(float targetVolume, float duration, bool pause = false, bool resume = false)
        {
            if (resume)
            {
                audioSource.volume = 0;
                audioSource.UnPause();
            }

            while (audioSource.volume != targetVolume)
            {
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, Time.unscaledDeltaTime / duration);
                yield return null;
            }

            if (pause)
                audioSource.Pause();
        }

        void Fade(float target, float dur, bool pause = false, bool resume = false)
        {
            StartFade(FadeTo(target, dur, pause, resume));
        }

        ICoroutine fadeHandle;
        void StartFade(IEnumerator fade)
        {
            if (fadeHandle != null && fadeHandle.Status == CoroutineStatus.Running)
                fadeHandle.Stop();
            fadeHandle = Coroutines.StartManagedCoroutine(fade);
        }

        public void FadeOutMusic()
        {
            StartFade(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            float originalVolume = audioSource.volume;
            while (audioSource.volume > 0)
            {
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0, Time.unscaledDeltaTime / 1.5f);
                yield return null;
            }
            audioSource.Stop();
            audioSource.volume = originalVolume;
        }

        public void Pause()
        {
            Fade(0, 1, pause: true);
        }

        public void Resume()
        {
            Fade(1, 1, resume: true);
        }

        public void SetLoop(bool state)
        {
            if (audioSource == null) return; //Happens when game is shutting down
            audioSource.loop = state;
        }
    }
}
