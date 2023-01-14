using UnityEngine;

namespace NBG.Audio
{
    public class AmbienceZone : MonoBehaviour
    {
        public int priority;
        public float transitionDuration = 3;
        public float mainVerbLevel;
        public float musicLevel = -10;
        public float ambienceLevel;
        public float effectsLevel;
        public float effectsLowpass = 22000;
        public float physicsLevel;
        public float characterLevel;
        public float ambienceFxLevel;

        public Collider boxCollider;
        public SphereCollider sphereCollider;

        public AmbienceSource[] sources;
        public float[] volumes;

        public AudioSource transitionAudioSource;
        public AudioSource transitionEnter;
        public AudioSource transitionExit;

        public float GetLevel(AmbienceSource source)
        {
            if (sources == null) return 0;
            for (int i = 0; i < sources.Length; i++)
                if (sources[i] == source)
                    return volumes[i];
            return 0;
        }

        private void Start()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            if (sphereCollider == null)
                sphereCollider = GetComponent<SphereCollider>();
        }
    }
}
