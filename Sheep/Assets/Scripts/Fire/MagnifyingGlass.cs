using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnifyingGlass : MonoBehaviour
{
    [SerializeField]
    Light lightSpec;
    [SerializeField]
    Transform glassTransform;
    [SerializeField]
    float focalLength = 2f;
    [SerializeField]
    SingleShotParticleEffect smokeParticles;
    [SerializeField]
    float timeToSmoke = 2f;
    [SerializeField]
    [Range(0f, 1f)]
    float accuracyForBurning = 0.7f;
    [SerializeField]
    float glassRadius = 0.5f;
    
    float MagnifierRange { get { return focalLength * 2f; } }

    [SerializeField]
    float minLightAngle = 3f;
    [SerializeField]
    float maxLightAngle = 45f;
    [SerializeField]
    float minLightIntensity = 0f;
    [SerializeField]
    float maxLightIntensity = 20f;

    InteractableEntity currentTarget;
    float smokeTimer = 0f;

    Light sun;

    // Start is called before the first frame update
    void Start()
    {
        var allLights = FindObjectsOfType<Light>();
        foreach(var light in allLights)
        {
            if (light.enabled && light.type == LightType.Directional)
            {
                sun = light;
                break;
            }
        }

        if (sun == null)
        {
            Debug.LogError("No directional lights detected for magnifying glass!");
        }

        lightSpec.range = MagnifierRange;
    }

    void FixedUpdate()
    {
        float lightIntensity = 0f;
        float heatIntensity = 0f;
        var burnDirection = glassTransform.forward;
        Vector3 burnPoint = Vector3.zero;

        if (sun != null)
        {
            var sunDirection = sun.transform.forward;
            var glassForward = glassTransform.forward;
            var glassBackward = -glassTransform.forward;

            var forwardDot = Vector3.Dot(sunDirection, glassForward);
            var backwardDot = Vector3.Dot(sunDirection, glassBackward);
            var maxDot = Mathf.Max(forwardDot, backwardDot);

            burnDirection = glassTransform.forward * (forwardDot > backwardDot ? 1 : -1);

            var raycastDistance = MagnifierRange;
            RaycastHit hitInfo;
            // Wide spherecast to adjust light
            if (Physics.SphereCast(glassTransform.position, glassRadius, burnDirection, out hitInfo, raycastDistance, (int)(Layers.Object | Layers.Walls)))
            {
                lightIntensity = CalculateIntensity(hitInfo, maxDot);
            }
            // Very accurate raycast to detect the burn point in the middle
            if (Physics.Raycast(glassTransform.position, burnDirection, out hitInfo, raycastDistance, (int)(Layers.Object | Layers.Walls)))
            {
                heatIntensity = CalculateIntensity(hitInfo, maxDot);
                burnPoint = hitInfo.point;
            }
            var burning = heatIntensity > accuracyForBurning;

            UpdateLightSpec(burnDirection, lightIntensity);
            if (burning)
            {
                var target = hitInfo.collider.GetComponentInParent<InteractableEntity>();
                if (currentTarget == target && currentTarget != null && currentTarget.physicalMaterial.Flammable)
                {
                    smokeTimer += Time.fixedDeltaTime;
                    if (smokeTimer > timeToSmoke)
                    {
                        EmitParticles(burnPoint);
                        target.NotifyObjectInsideFire();
                    }
                }
                else
                {
                    smokeTimer = 0f;
                    currentTarget = target;
                }
            }
            else
            {
                smokeTimer = 0f;
                currentTarget = null;
            }
        }
    }

    float CalculateIntensity(RaycastHit hitInfo, float maxDot)
    {
        var focalDiff = Mathf.Abs(hitInfo.distance - focalLength);
        var intensity = Mathf.Clamp01(maxDot);
        intensity -= focalDiff / (MagnifierRange);
        return intensity;
    }

    void UpdateLightSpec(Vector3 direction, float intensity)
    {
        lightSpec.transform.rotation = Quaternion.LookRotation(direction);
        lightSpec.innerSpotAngle = 1f;
        lightSpec.spotAngle = Mathf.Lerp(maxLightAngle, minLightAngle, intensity);
        lightSpec.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, Mathf.Pow(intensity, 4));
    }

    void EmitParticles(Vector3 position)
    {
        var particleInstance = smokeParticles.Create(position) as SingleShotParticleEffect;
        particleInstance.Particles.Emit(1);
    }

    private void OnDrawGizmosSelected()
    {
        var spheres = Mathf.RoundToInt(MagnifierRange / glassRadius);
        for(int i = 0; i < spheres; i++)
        {
            var progress = MagnifierRange * (float)i / spheres;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + transform.forward * progress, glassRadius);
            Gizmos.DrawWireSphere(transform.position - transform.forward * progress, glassRadius);
        }
    }
}
