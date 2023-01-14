using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSourceSetup : MonoBehaviour
{
    // attenuation (from calculate attenuation)
    public float maxDistance = 30;
    public float falloffStart = 1;
    public float falloffPower = .5f;
    public float lpStart = 2;
    public float lpPower = .5f;
    public float spreadNear = 1f;
    public float spreadFar = 0;
    public float spatialNear = 0.5f;
    public float spatialFar = 1;


}
