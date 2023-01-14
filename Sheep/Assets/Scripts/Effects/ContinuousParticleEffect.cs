using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousParticleEffect : Poolable
{
    [SerializeField]
    protected ParticleSystem particles;
    internal ParticleSystem Particles => particles;

    [Tooltip("Optional. Set object you want particles to follow.")]
    [SerializeField]
    Transform followTarget;

    void Awake()
    {
        particles = GetComponent<ParticleSystem>();
    }

    protected virtual void Update()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;
        }
    }

    public void EnableFollow(Transform target)
    {
        followTarget = target;
    }

    protected override void ResetState()
    {
        base.ResetState();
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        followTarget = null;
    }

    public void StopEmittingAndDeinstantiateAfterwards()
    {
        if (gameObject.activeInHierarchy)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(DeinstantiateAfterLastParticles());
        }
    }

    IEnumerator DeinstantiateAfterLastParticles()
    {
        yield return new WaitUntil(() => !particles.IsAlive(true));
        if (gameObject.activeInHierarchy)
        {
            Deinstantiate();
        }
    }
}
