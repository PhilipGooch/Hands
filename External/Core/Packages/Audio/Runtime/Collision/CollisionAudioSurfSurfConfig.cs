using UnityEngine;

namespace NBG.Audio
{
    [CreateAssetMenu(fileName = "CollisionAudioSurfSurfConfig", menuName = "[NBG] Audio/CollisionAudioSurfSurfConfig")]
    public class CollisionAudioSurfSurfConfig : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        private int Surface1; // old
        [SerializeField]
        private int Surface2; // old

        public SurfaceType SurfaceType1;
        public SurfaceType SurfaceType2;
        public AudioChannel AudioMixerGroup;
        public float volume = 1;
        public SampleLibrary sampleLibrary;
        public GameObject particles;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            // Upgrade old format to new
            if (Surface1 != 0)
            {
                SurfaceType1 = new SurfaceType(Surface1);
                Surface1 = 0;
            }

            if (Surface2 != 0)
            {
                SurfaceType2 = new SurfaceType(Surface2);
                Surface2 = 0;
            }
        }
    }
}
