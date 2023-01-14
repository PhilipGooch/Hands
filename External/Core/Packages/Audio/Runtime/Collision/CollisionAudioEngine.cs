using System.Collections.Generic;
using UnityEngine;

namespace NBG.Audio
{
    public class CollisionAudioEngine : MonoBehaviour
    {
        [HideInInspector] public static CollisionAudioEngine instance;

#pragma warning disable
        [SerializeField] private CollisionMap collisionMap;
        [SerializeField] private List<AudioSource> audioSourcePool;
#pragma warning enable
        [SerializeField] private float minAudioSourceDistance = 0.2f;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            if (instance != this)
                Destroy(this.gameObject);
        }
        private void OnDestroy()
        {
        }

        public bool ReportCollision(SurfaceType surf1, SurfaceType surf2, Vector3 contactPoint, Vector3 pos, float volume, float pitch)
        {
            // If there is another sound in the exact same point, they are the same called from the two different collision sensors, so return to only reproduce one of them.
            foreach (var audioSource in audioSourcePool)
                if (Vector3.Distance(contactPoint, audioSource.transform.position) < minAudioSourceDistance && audioSource.isPlaying)
                    return false;

            // Play if there is an audio source available in the pool.
            foreach (var audioSource in audioSourcePool)
            {
                if (!audioSource.isPlaying)
                {
                    PlayAtPoint(surf1, surf2, pos, volume, pitch);
                    return true;
                }
            }

            return false;
        }

        void PlayAtPoint(SurfaceType surf1, SurfaceType surf2, Vector3 point, float volume, float pitch)
        {
            CollisionAudioSurfSurfConfig collisionAudioSurfSurfConfig = collisionMap.GetCollisionConfig(surf1, surf2);

            if (collisionAudioSurfSurfConfig != null)
            {
                foreach (var audioSource in audioSourcePool)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.volume = volume * collisionAudioSurfSurfConfig.volume;
                        audioSource.pitch = pitch;
                        audioSource.clip = collisionAudioSurfSurfConfig.sampleLibrary.GetRandomClip();
                        audioSource.outputAudioMixerGroup = AudioRouting.GetChannel(collisionAudioSurfSurfConfig.AudioMixerGroup);
                        audioSource.transform.position = point;
                        audioSource.Play();
                        return;
                    }
                }

                if (collisionAudioSurfSurfConfig.particles != null)
                {
                    var particles = Instantiate(collisionAudioSurfSurfConfig.particles);
                    particles.transform.position = point;
                }
            }
            else
            {
                if (GameAudio.instance.debug) Debug.LogWarning("There is no CollisionAudioSurfSurfConfig for surface combination: " + surf1 + " & " + surf2);
            }
        }
    }
}
