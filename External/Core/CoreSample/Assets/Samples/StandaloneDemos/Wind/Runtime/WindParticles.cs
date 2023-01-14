using NBG.Core;
using NBG.LogicGraph;
using NBG.Wind;
using UnityEngine;

public class WindParticles : MonoBehaviour, IManagedBehaviour
{
    [SerializeField]
    private ParticleSystem airParticlesBlow;
    [SerializeField]
    private ParticleSystem airParticlesSuck;

    private WindDemo wind;

    private float power;
    [NodeAPI("Power")]
    public float Power
    {
        set
        {
            power = value;
            UpdateParticles(power, distance);
        }
    }

    private float distance;
    [NodeAPI("Distance")]
    public float Distance
    {
        set
        {
            distance = value;
            UpdateParticles(power, distance);
        }
    }

    public void OnLevelLoaded()
    {
        wind = GetComponent<WindDemo>();
        ParticlesSetup();
    }

    public void OnAfterLevelLoaded() { }

    public void OnLevelUnloaded() { }

    private void ParticlesSetup()
    {
        if (airParticlesBlow != null)
            SetupParticle(airParticlesBlow);
        if (airParticlesSuck != null)
            SetupParticle(airParticlesSuck);
    }

    private void SetupParticle(ParticleSystem system)
    {
        var emission = system.emission;
        emission.enabled = false;
    }

    private void UpdateParticles(float power, float distance)
    {
        UpdateParticlesEmmision(power);
        if (wind.Mode == WindmakerMode.Suck)
        {
            if (airParticlesSuck != null)
            {
                //need to readjust particle emitter position based on farthest obstacle
                airParticlesSuck.transform.position = wind.AirZoneStart + wind.WindDirection * distance;

                AdjustParticlesLifetime(airParticlesSuck, distance);
            }
        }
        else
        {
            if (airParticlesBlow != null)
            {
                AdjustParticlesLifetime(airParticlesBlow, distance);
            }
        }
    }

    private void UpdateParticlesEmmision(float power)
    {
        if (airParticlesBlow != null)
        {
            var emission = airParticlesBlow.emission;
            emission.enabled = power > NBG.Wind.WindZone.powerOffAccuracy && wind.Mode == WindmakerMode.Blow;
        }

        if (airParticlesSuck != null)
        {
            var emission = airParticlesSuck.emission;
            emission.enabled = power > NBG.Wind.WindZone.powerOffAccuracy && wind.Mode == WindmakerMode.Suck;
        }
    }

    private void AdjustParticlesLifetime(ParticleSystem system, float distance)
    {
        var main = system.main;
        var lifetime = main.startLifetime;
        lifetime.constant = distance / main.startSpeed.constant;
        main.startLifetime = lifetime;
    }
}

