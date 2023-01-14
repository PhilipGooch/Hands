using Plugs;
using UnityEngine;

[RequireComponent(typeof(AttenuatedAudioPool))]
public class SoundFromSocketEvents : MonoBehaviour
{
    [SerializeField]
    AudioClip plugInSound;
    [SerializeField]
    AudioClip plugOutSound;
    [SerializeField]
    AttenuatedAudioPool audioPool;
    [SerializeField]
    Hole hole;

    private void OnValidate()
    {
        if (audioPool == null)
            audioPool = GetComponent<AttenuatedAudioPool>();
        if (hole == null)
            hole = GetComponent<Hole>();
    }

    private void Awake()
    {
        hole.onPlugIn += OnPlugIn;
        hole.onPlugOut += OnPlugOut;
    }

    private void OnDestroy()
    {
        hole.onPlugIn -= OnPlugIn;
        hole.onPlugOut -= OnPlugOut;
    }

    private void OnPlugOut()
    {
        audioPool.PlayOneShot(transform.position, plugOutSound);
    }

    private void OnPlugIn()
    {
        audioPool.PlayOneShot(transform.position, plugInSound);
    }
}
