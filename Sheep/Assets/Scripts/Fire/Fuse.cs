using NBG.LogicGraph;
using System;
using UnityEngine;

[System.Serializable]
public class Fuse : MonoBehaviour, IRespawnListener
{
    [SerializeField]
    InteractableEntity fuseEntity;
    [SerializeField]
    float burnDuration = 5f;
    [SerializeField]
    ContinuousParticleEffect sparkParticlePrefab;
    [SerializeField]
    Vector3 fuseAxis = Vector3.up;
    [SerializeField]
    bool resetStateAfterBurning = true;
    [SerializeField]
    bool canBeIgnitedWithFire = true;
    [SerializeField]
    PhysicalMaterial unignitableMaterial;
    [SerializeField]
    GameObject visuals;

    [NodeAPI("OnBurnt")]
    public event Action onBurnt;

    Vector3 startScale = Vector3.one;

    ContinuousParticleEffect sparkParticleInstance;
    PhysicalMaterial originalMaterial;
    float burnTimer = 0f;
    float fuseLength = 1f;

    bool hasBurnt;

    Transform VisualsTransform => visuals ? visuals.transform : transform;

    void OnValidate()
    {
        if (visuals == null)
            visuals = GetComponentInChildren<MeshRenderer>()?.gameObject;
    }

    private void Awake()
    {
        startScale = VisualsTransform.localScale;
        fuseAxis = fuseAxis.normalized;
        var childCollider = GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            var childBounds = childCollider.bounds.size;
            var worldAxis = transform.rotation * fuseAxis;
            // Since we're projecting scale, we need this to be positive, otherwise we will get incorrect results
            worldAxis.x = Mathf.Abs(worldAxis.x);
            worldAxis.y = Mathf.Abs(worldAxis.y);
            worldAxis.z = Mathf.Abs(worldAxis.z);

            var projected = Vector3.Project(childBounds, worldAxis);
            fuseLength = projected.magnitude;
        }
        originalMaterial = fuseEntity.physicalMaterial;
        if (!canBeIgnitedWithFire)
        {
            fuseEntity.physicalMaterial = unignitableMaterial;
        }
    }

    private void Update()
    {
        if (fuseEntity.Ignited)
        {
            if (sparkParticlePrefab != null)
            {
                if (sparkParticleInstance == null)
                {
                    sparkParticleInstance = sparkParticlePrefab.Create(transform.position, transform.rotation) as ContinuousParticleEffect;
                }

                sparkParticleInstance.transform.position = GetParticlePosition();
                sparkParticleInstance.transform.rotation = transform.rotation;
            }
        }
        else
        {
            RemoveParticles();
        }
    }

    [NodeAPI("Ignite")]
    public void Ignite()
    {
        if (hasBurnt)
            return;

        if (!canBeIgnitedWithFire)
        {
            fuseEntity.physicalMaterial = originalMaterial;
        }
        fuseEntity.IgniteObject();
    }

    void FixedUpdate()
    {
        if (fuseEntity.Ignited && hasBurnt == false)
        {
            burnTimer += Time.fixedDeltaTime;
            if (burnTimer > burnDuration)
            {
                VisualsTransform.gameObject.SetActive(false);

                onBurnt?.Invoke();
                hasBurnt = true;

                RemoveParticles();
                fuseEntity.ExtinquishObject();

                if (resetStateAfterBurning)
                {
                    ResetState();
                }
            }
            else
            {
                VisualsTransform.localScale = GetFuseScale();
            }
        }
    }

    Vector3 GetParticlePosition()
    {
        var localPosition = fuseAxis * fuseLength * (1f - GetProgress());
        return transform.position + transform.rotation * localPosition;
    }

    Vector3 GetFuseScale()
    {
        var axisScale = Vector3.Project(startScale, fuseAxis);
        var baseScale = startScale - axisScale;
        var progress = GetProgress();
        var invProgress = 1f - progress;
        return baseScale + axisScale * invProgress;
    }

    float GetProgress()
    {
        return burnTimer / burnDuration;
    }

    private void OnDestroy()
    {
        RemoveParticles();
    }

    void ResetState()
    {
        hasBurnt = false;
        VisualsTransform.gameObject.SetActive(true);

        burnTimer = 0f;
        fuseEntity.ResetState();
        VisualsTransform.localScale = startScale;

        RemoveParticles();
        if (!canBeIgnitedWithFire)
        {
            fuseEntity.physicalMaterial = unignitableMaterial;
        }
    }

    void RemoveParticles()
    {
        if (sparkParticleInstance != null)
        {
            sparkParticleInstance.StopEmittingAndDeinstantiateAfterwards();
            sparkParticleInstance = null;
        }
    }

    public void OnRespawn()
    {
        //I dont like this, but fuse does not use respawn system so cannot be enabled by it.
        //Plus fuse may not even be linked to despawning objects - cannon.
        //So either fuse is either enabled here or if resetStateAfterBurning is checked.
        //Another way would be to enable it onResetState as it was before.
        ResetState();
    }

    public void OnDespawn()
    {
        ResetState();
    }
}
