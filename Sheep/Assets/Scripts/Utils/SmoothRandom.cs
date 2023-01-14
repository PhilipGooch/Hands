using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothRandom
{
    public float min=0;
    public float max=1;
    public float minDuration=1;
    public float maxDuration=5;

    float from;
    float to;
    float duration;
    float time;
    public float value => Mathf.Lerp(from, to, time/duration);
    public void Step(float dt)
    {
        time += dt;
        if(time>duration)
        {
            from = to;
            time = 0;
            duration = Random.Range(minDuration, maxDuration);
            to = Random.Range(min, max);
        }
    }
    
}
