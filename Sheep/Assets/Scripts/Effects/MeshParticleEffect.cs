using UnityEngine;

public class MeshParticleEffect : ContinuousParticleEffect
{
    [SerializeField]
    bool initializeOnStart = false;
    [SerializeField]
    MeshFilter targetFilter;
    bool staticBatched;

    private void Start()
    {
        if (initializeOnStart)
            Initialize();
    }

    protected override void ResetState()
    {
        base.ResetState();
        targetFilter = null;
        var shape = particles.shape;
        shape.useMeshMaterialIndex = false;
        shape.meshMaterialIndex = 0;
    }

    protected override void Update()
    {
        if (!staticBatched)
        {
            base.Update();
        }
    }

    public void Initialize()
    {
        if (targetFilter != null)
        {
            var target = targetFilter.GetComponent<MeshRenderer>();
            if (target != null)
                Initialize(target);
        }
    }

    public void Initialize(MeshRenderer target)
    {
        staticBatched = target.isPartOfStaticBatch;
        targetFilter = target.GetComponent<MeshFilter>();
        if (targetFilter != null)
        {
            ParticleUtils.SetupParticleMeshShape(transform, particles, target, targetFilter);
            if (!staticBatched)
            {
                EnableFollow(targetFilter.transform);
            }
        }
        else
        {
            Deinstantiate();
        }
    }

    public void Initialize(SkinnedMeshRenderer target)
    {
        if (target.sharedMesh != null)
        {
            staticBatched = target.isPartOfStaticBatch;
            ParticleUtils.SetupParticleMeshShape(transform, particles, target);
        }
        else
        {
            Deinstantiate();
        }
    }
}
