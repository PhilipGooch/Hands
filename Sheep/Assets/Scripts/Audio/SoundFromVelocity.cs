using Recoil;
using System.Collections.Generic;
using UnityEngine;

public class SoundFromVelocity : MonoBehaviour, IRespawnListener
{
    AudioSource audioSource;
    [SerializeField]
    new Rigidbody rigidbody;
    [SerializeField]
    float soundFromVelThresh = 15f;
    [SerializeField]
    float speedForMaxPitch = 100f;
    [SerializeField]
    float speedForMaxVolume = 70f;
    [Range(0, 1)]
    [SerializeField]
    float maxVolume = 1;
    [Range(0, 3)]
    [SerializeField]
    float basePitch = 3; //3 because max pitch is 3
    const int framesToStore = 10;

    ReBody reBody;

    Queue<float> velocityMagPerFrames = new Queue<float>(framesToStore);

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (rigidbody == null)
            rigidbody = GetComponentInParent<Rigidbody>();
        reBody = new ReBody(rigidbody);
    }

    void FixedUpdate()
    {
        if (reBody.BodyExists)
        {

            if (velocityMagPerFrames.Count == framesToStore)
                velocityMagPerFrames.Dequeue();

            velocityMagPerFrames.Enqueue(reBody.velocity.magnitude);

            float avg = 0;

            foreach (var item in velocityMagPerFrames)
            {
                avg += item;
            }

            avg = avg / framesToStore;

            if (avg >= soundFromVelThresh && !audioSource.isPlaying)
                audioSource.Play();
            else if (avg < soundFromVelThresh && audioSource.isPlaying)
                audioSource.Stop();

            float adjustedVel = Mathf.Clamp(avg - soundFromVelThresh, 0, 1000);

            if (audioSource.isPlaying)
            {
                audioSource.pitch = (basePitch * adjustedVel / speedForMaxPitch);
                audioSource.volume = Mathf.Clamp((adjustedVel / speedForMaxVolume), 0, maxVolume);
            }
        }
    }

    public void OnRespawn()  {}

    public void OnDespawn()
    {
        velocityMagPerFrames.Clear();
        audioSource.Stop();
    }
}
