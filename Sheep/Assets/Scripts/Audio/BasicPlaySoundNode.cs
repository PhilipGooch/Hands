using NBG.LogicGraph;
using UnityEngine;

[RequireComponent(typeof(AttenuatedAudioPool))]
public class BasicPlaySoundNode  : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    AttenuatedAudioPool audioPool;

    [SerializeField]
    AudioClip sound;
    [SerializeField]
    float volume = 1;
    [SerializeField]
    float pitch = 1;

    private void OnValidate()
    {
        if (audioPool == null)
            audioPool = GetComponent<AttenuatedAudioPool>();
    }

    [NodeAPI("PlaySound")]
    public void PlaySound()
    {
        audioPool.PlayOneShot(transform, sound, volume, pitch);
    }
}
