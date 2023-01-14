using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollUVsFromActivation : ActivatableNode
{
    [SerializeField]
    Vector2 uvScrollModifier = Vector2.up;
    [SerializeField]
    [Tooltip("How far to scroll the uv coordinates when going from 0 to 1 activation.")]
    float uvScrollAmount = 1f;

    Material material;
    private void Awake()
    {
        material = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        var finalScroll = ActivationValue * uvScrollModifier * uvScrollAmount;
        material.mainTextureOffset = finalScroll;
    }
}
