using UnityEngine;

[RequireComponent(typeof(AttenuatedAudioPool))]
public class SoundFromActivation : ActivatableNode
{
    [SerializeField]
    PlaySoundFromChange soundPlayer;

    AttenuatedAudioPool audioPool;
    protected override void Awake()
    {
        base.Awake();
        audioPool = GetComponent<AttenuatedAudioPool>();

        soundPlayer.Initialize(ActivationValue, audioPool, transform);
    }
}
