using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkLights : MonoBehaviour
{
    [SerializeField]
    float offDuration = 1f;
    [SerializeField]
    float onDuration = 0.1f;
    [SerializeField]
    float transitionTime = 0.05f;
    [SerializeField]
    Light targetLight;
    [SerializeField]
    MeshRenderer targetRenderer;
    [SerializeField]
    [ColorUsage(true, true)]
    Color offEmission;
    [SerializeField]
    [ColorUsage(true, true)]
    Color onEmission;
    [SerializeField]
    float timerOffset;

    int emissionId = Shader.PropertyToID("_EmissionColor");
    float timer = 0f;
    float originalLightIntensity = 1f;
    bool turnedOn = false;
    MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        if (targetLight)
        {
            originalLightIntensity = targetLight.intensity;
        }
        SetInitialState();
    }

    void SetInitialState()
    {
        var maxTime = onDuration + offDuration;
        var offset = timerOffset % maxTime;
        turnedOn = offset > offDuration;
        timer = offset;
        UpdateState();
    }

    void Update()
    {
        timer += Time.deltaTime;
        var targetTime = turnedOn ? onDuration : offDuration;
        if (timer > targetTime + transitionTime)
        {
            timer = 0f;
            turnedOn = !turnedOn;
        }

        UpdateState();
    }

    void UpdateState()
    {
        float transitionProgress = Mathf.Clamp01(timer / transitionTime);

        if (targetLight)
        {
            var lightOn = turnedOn || transitionProgress < 1f;
            targetLight.enabled = lightOn;
            var intensityProgress = originalLightIntensity * (turnedOn ? transitionProgress : 1f - transitionProgress);
            targetLight.intensity = intensityProgress;
        }

        if (targetRenderer)
        {
            var targetEmission = Color.Lerp(offEmission, onEmission, turnedOn ? transitionProgress : 1f - transitionProgress);
            propertyBlock.SetColor(emissionId, targetEmission);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
