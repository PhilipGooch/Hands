using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotParticleEffect : Poolable
{
    public ParticleSystem Particles { get { return allParticles[0]; } }

    protected ParticleSystem[] allParticles = null;

    // Start is called before the first frame update
    void Awake()
    {
        allParticles = GetComponentsInChildren<ParticleSystem>();
    }

    protected override void ResetState()
    {
        base.ResetState();
        SetVelocity(Vector3.zero);
    }

    public void SetVelocity(Vector3 velocity)
    {;
        foreach(var particle in allParticles)
        {
            var velModule = particle.velocityOverLifetime;
            velModule.x = velocity.x;
            velModule.y = velocity.y;
            velModule.z = velocity.z;
            velModule.space = ParticleSystemSimulationSpace.World;
        }
    }

    private void Update()
    {
        bool stoppedPlaying = true;
        foreach(var particle in allParticles)
        {
            if (particle.IsAlive())
            {
                stoppedPlaying = false;
                break;
            }
        }

        if (stoppedPlaying)
        {
            Deinstantiate();
        }
    }
}
