using UnityEngine;

namespace NBG.Audio
{
    [CreateAssetMenu(fileName = "SampleLibrary", menuName = "[NBG] Audio/SampleLibrary")]
    public class SampleLibrary : ScriptableObject
    {
        public AudioClip[] Clips;

        public AudioClip GetRandomClip()
        {
            if (Clips.Length > 0)
                return Clips[Random.Range(0, Clips.Length)];
            else
                Debug.LogWarning("SampleLibrary is empty:" + this.name);

            return null;
        }
    }
}
