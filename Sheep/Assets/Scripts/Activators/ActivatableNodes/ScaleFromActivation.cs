using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleFromActivation : ActivatableNode
{
    Vector3 startSize;

    void Start()
    {
        startSize = transform.localScale;
    }

    void Update()
    {
        transform.localScale = startSize * ActivationValue;
    }
}
