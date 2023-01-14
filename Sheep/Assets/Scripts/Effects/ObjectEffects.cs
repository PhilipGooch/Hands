using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectEffects
{
    List<MeshRenderer> activeMeshRenderers = new List<MeshRenderer>();
    List<SkinnedMeshRenderer> activeSkinnedMeshRenderers = new List<SkinnedMeshRenderer>();
    List<MeshParticleEffect> activeMeshParticleEffects = new List<MeshParticleEffect>();
    List<ContinuousParticleEffect> activeContinuousParticleEffects = new List<ContinuousParticleEffect>();
    List<Color> originalTints;
    GameObject target;
    bool HasMesh => activeMeshRenderers.Count > 0 || activeSkinnedMeshRenderers.Count > 0;

    public ObjectEffects(GameObject target)
    {
        var renderers = target.GetComponentsInChildren<MeshRenderer>();
        originalTints = new List<Color>();
        this.target = target;
        var skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var renderer in renderers)
        {
            if (renderer.enabled)
            {
                activeMeshRenderers.Add(renderer);
                originalTints.Add(renderer.sharedMaterial.color);
            }
        }

        foreach (var skinned in skinnedRenderers)
        {
            if (skinned.enabled)
            {
                activeSkinnedMeshRenderers.Add(skinned);
                originalTints.Add(skinned.sharedMaterial.color);
            }
        }
    }

    public void EnableFire(bool spawnParticles)
    {
        if (spawnParticles)
        {
            if (HasMesh)
                EnableMeshEffect(GameParameters.Instance.meshFireParticles);
            else
                EnableContinuousEffect(GameParameters.Instance.continuousFireParticles);
        }

        AudioManager.instance.PlayFire(target.transform);
    }

    public void DisableFire()
    {
        DisableMeshEffect(GameParameters.Instance.meshFireParticles);
        DisableContinuousEffect(GameParameters.Instance.continuousFireParticles);

        AudioManager.instance.ReleaseFire(target.transform);
    }

    void EnableContinuousEffect(ContinuousParticleEffect effect)
    {
        var instance = effect.Create() as ContinuousParticleEffect;
        instance.EnableFollow(target.transform);
        activeContinuousParticleEffects.Add(instance);
    }

    void DisableContinuousEffect(ContinuousParticleEffect effect)
    {
        for (int i = activeContinuousParticleEffects.Count - 1; i >= 0; i--)
        {
            var activeEffect = activeContinuousParticleEffects[i];
            if (activeEffect.OriginalPrefab == effect)
            {
                activeEffect.StopEmittingAndDeinstantiateAfterwards();
                activeContinuousParticleEffects.RemoveAt(i);
            }
        }
    }

    void EnableMeshEffect(MeshParticleEffect effect)
    {
        foreach (var mesh in activeMeshRenderers)
        {
            var instance = effect.Create() as MeshParticleEffect;
            instance.Initialize(mesh);
            activeMeshParticleEffects.Add(instance);
        }
        foreach (var skinned in activeSkinnedMeshRenderers)
        {
            var instance = effect.Create() as MeshParticleEffect;
            instance.Initialize(skinned);
            activeMeshParticleEffects.Add(instance);
        }
    }

    void DisableMeshEffect(MeshParticleEffect effect)
    {
        for (int i = activeMeshParticleEffects.Count - 1; i >= 0; i--)
        {
            var activeEffect = activeMeshParticleEffects[i];
            if (activeEffect.OriginalPrefab == effect)
            {
                activeEffect.StopEmittingAndDeinstantiateAfterwards();
                activeMeshParticleEffects.RemoveAt(i);
            }
        }
    }

    public void DisableAllEffects()
    {
        foreach (var effect in activeMeshParticleEffects)
        {
            effect.StopEmittingAndDeinstantiateAfterwards();
        }
        activeMeshParticleEffects.Clear();

        foreach (var effect in activeContinuousParticleEffects)
        {
            effect.StopEmittingAndDeinstantiateAfterwards();
        }
        activeContinuousParticleEffects.Clear();

        AudioManager.instance.ReleaseAllLoopingSoundsForTarget(target.transform);
    }

    public void ResetState()
    {
        DisableAllEffects();
        SetTint(Color.white, 0f);
    }

    public void SetTint(Color targetColor, float amount)
    {
        for (int i = 0; i < activeMeshRenderers.Count; i++)
        {
            var meshRenderer = activeMeshRenderers[i];
            meshRenderer.material.color = Color.Lerp(originalTints[i], targetColor, amount);
        }
        for (int i = 0; i < activeSkinnedMeshRenderers.Count; i++)
        {
            var skinned = activeSkinnedMeshRenderers[i];
            skinned.material.color = Color.Lerp(originalTints[activeMeshRenderers.Count + i], targetColor, amount);
        }
    }
}
