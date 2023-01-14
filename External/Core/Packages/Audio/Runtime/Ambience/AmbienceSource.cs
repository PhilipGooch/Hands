using UnityEngine;

namespace NBG.Audio
{
    public class AmbienceSource : MonoBehaviour
    {
        [HideInInspector] public float transitionFrom = 0;
        [HideInInspector] public float transitionTo = 0;

        [SerializeField] AudioClip audioClip = default;

        AudioSource audioSource;
        float transitionSpeed = 10000000;
        float transitionPhase = 0;

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if (transitionPhase < 1)
            {
                transitionPhase = Mathf.Clamp01(transitionPhase + Time.unscaledDeltaTime * transitionSpeed);
                audioSource.volume = Mathf.Lerp(transitionFrom, transitionTo, Mathf.Sqrt(transitionPhase));
            }
        }

        internal void FadeVolume(float volume, float duration)
        {
            if (duration == 0)
                throw new System.ArgumentException("duration can't be 0", "duration");
            transitionFrom = audioSource.volume;
            transitionTo = volume;
            transitionSpeed = 1 / duration;
            transitionPhase = 0;
        }

        private void Initialize()
        {
            // Gets reference (or Add) the AudioSource of this gameobject.
            audioSource = transform.gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = transform.gameObject.AddComponent<AudioSource>();
            audioSource.clip = audioClip;

            // Setup ambience parameters.
            var matchingGroups = GameAudio.instance.mainMixer.FindMatchingGroups("Ambience");
            if (matchingGroups.Length == 0)
                return;
            audioSource.outputAudioMixerGroup = matchingGroups[0];
            audioSource.priority = 10;
            audioSource.playOnAwake = true;
            audioSource.loop = true;
            audioSource.volume = 0;
            audioSource.spatialBlend = 0;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 5000;
            audioSource.maxDistance = 5000;
            audioSource.Play();
        }
    }
}
