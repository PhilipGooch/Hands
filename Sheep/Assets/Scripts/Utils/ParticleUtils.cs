using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ParticleUtils
{
    public static void SetupParticleMeshShape(Transform transform, ParticleSystem particles, MeshRenderer target, MeshFilter targetFilter)
    {
        var staticBatched = target.isPartOfStaticBatch;
        var shape = particles.shape;
        shape.mesh = targetFilter.sharedMesh;
        shape.scale = target.transform.lossyScale;

        if (staticBatched)
        {
            shape.useMeshMaterialIndex = true;
            shape.meshMaterialIndex = target.subMeshStartIndex;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
        else
        {
            transform.position = target.transform.position;
            transform.rotation = target.transform.rotation;
        }
        shape.shapeType = ParticleSystemShapeType.Mesh;
    }

    public static void SetupParticleMeshShape(Transform transform, ParticleSystem particles, SkinnedMeshRenderer target)
    {
        var shape = particles.shape;
        shape.skinnedMeshRenderer = target;
        shape.scale = target.transform.lossyScale;

        transform.position = target.transform.position;
        transform.rotation = target.transform.rotation;

        shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
    }
}
