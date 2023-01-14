using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepAudio : MonoBehaviour
{
    float cooldown;
    float treshold;
    Sheep sheep;

    public System.Action onSheepVocalize;

    bool CanPlayAudio
    {
        get
        {
            return AudioManager.instance != null;
        }
    }

    private void Awake()
    {
        sheep = GetComponentInParent<Sheep>();
        treshold = Random.Range(0, 10);
        cooldown = Random.Range(0, 10);
        accumulatedThreat = treshold * Random.value;
    }
    private void Step()
    {
        if (sheep.Grounded)
        {
            if (CanPlayAudio)
            {
                AudioManager.instance.PlayStep(sheep.reHead.position, Mathf.Lerp(.8f, 1.2f, Mathf.InverseLerp(0.25f, 2f, sheep.speed)));
            }
        }
    }
    float accumulatedThreat;
    private void Update()
    {
        cooldown -= Time.deltaTime;
        accumulatedThreat += .1f*Mathf.Max(.1f, sheep.threatLevel )* Time.deltaTime;

        if (cooldown<=0 && ( accumulatedThreat > treshold || Random.value < Time.deltaTime *  Mathf.Lerp(0,.5f, Mathf.InverseLerp(1,1.2f, sheep.threatLevel) ))) 
            PlayVoice(Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(.5f, 2f, sheep.threatLevel)));

    }

    public void PlayVoice(float volume)
    {
        if (CanPlayAudio)
        {
            AudioManager.instance.PlayVoice(sheep.head.transform, volume);
            treshold = Random.Range(0, 10);
            accumulatedThreat = 0;
            cooldown = Random.Range(1, 10);
            onSheepVocalize?.Invoke();
        }
    }
    public void PlayGrabbedVoice(float volume)
    {
        if (CanPlayAudio)
        {
            AudioManager.instance.PlayGrabbedVoice(sheep.head.transform);
            treshold = Random.Range(0, 10);
            accumulatedThreat = 0;
            cooldown = Random.Range(1, 10);
        }
    }
}
