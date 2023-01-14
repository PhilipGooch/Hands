using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorGradingData : SingletonBehaviour<ColorGradingData>
{
    [SerializeField]
    Texture2D lookUpTexture;
    [SerializeField]
    [Range(0f,1f)]
    float contribution = 1f;

    public Texture2D LUT => lookUpTexture;
    public float Contribution => contribution;
}
