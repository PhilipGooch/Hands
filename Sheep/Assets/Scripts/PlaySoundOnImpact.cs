using UnityEngine;

public class PlaySoundOnImpact : MonoBehaviour
{
    public Vector2 forceForSoundScaling = new Vector2(0, 100f);
    public AudioClip soundToPlay;
    AudioSource source;

    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        var force = collision.impulse.magnitude;
        if (force > forceForSoundScaling.x)
        {
            var volumeScale = Mathf.Lerp(0f, 1f, Mathf.Clamp01(force / forceForSoundScaling.y));
            source.PlayOneShot(soundToPlay, volumeScale);
        }
    }
}
