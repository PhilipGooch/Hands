using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotParticleMeshEffect : SingleShotParticleEffect
{
    public void SetTargetMesh(MeshRenderer target)
    {
        if (target != null)
        {
            var targetFilter = target.GetComponent<MeshFilter>();
            if (targetFilter != null)
            {
                foreach (var particle in allParticles)
                {
                    ParticleUtils.SetupParticleMeshShape(transform, particle, target, targetFilter);
                }
            }
            else
            {
                Deinstantiate();
            }
        }
        else
        {
            Deinstantiate();
        }

    }

    public void SetTargetMesh(SkinnedMeshRenderer target)
    {
        if (target != null)
        {
            foreach (var particle in allParticles)
            {
                ParticleUtils.SetupParticleMeshShape(transform, particle, target);

            }
        }
        else
        {
            Deinstantiate();
        }
    }


    protected override void ResetState()
    {
        base.ResetState();
        foreach (var particle in allParticles)
        {
            var shape = particle.shape;
            shape.meshRenderer = null;
            shape.mesh = null;
            shape.useMeshMaterialIndex = false;
        }
    }

}
