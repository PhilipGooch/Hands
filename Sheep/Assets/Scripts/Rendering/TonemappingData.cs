using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TonemappingData : SingletonBehaviour<TonemappingData>
{
    [SerializeField]
    float gamma = 1.5f;
    public float Gamma => gamma;
}
