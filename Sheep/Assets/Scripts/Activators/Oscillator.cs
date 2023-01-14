using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oscillator : ObjectActivator
{
    [SerializeField]
    float frequency = 1f;

    float timer = 0f;

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer > frequency)
            timer = 0f;

        var progress = timer / frequency;

        ActivationAmount = 1f - (Mathf.Cos(Mathf.PI * 2 * progress) + 1f) / 2f;
    }
}
