using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VR.System;

public class GrassShaderHelper : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    MeshFilter grassFilter;
    [SerializeField]
    Mesh originalMesh;
    [SerializeField]
    [Range(1,16)]
    int highQualityLayerCount = 8;
    [SerializeField]
    [Range(1, 16)]
    int lowQualityLayerCount = 3;
    [SerializeField]
    float highQualityHeight = 0.5f;
    [SerializeField]
    float lowQualityHeight = 0.25f;
    [SerializeField]
    [Range(0,16)]
    int targetSubmesh = 0;

    #if UNITY_EDITOR
    int cullProperty = Shader.PropertyToID("_Cull");

    int LayerCount
    {
        get
        {
            switch(VRSystem.GetQualityLevel())
            {
                case VRSystem.QualityLevel.High:
                    return highQualityLayerCount;
                case VRSystem.QualityLevel.Low:
                    return lowQualityLayerCount;
                default:
                    Debug.LogWarning("Undefined quality level!");
                    return lowQualityLayerCount;
            }
        }
    }

    float GrassHeight
    {
        get
        {
            switch(VRSystem.GetQualityLevel())
            {
                case VRSystem.QualityLevel.High:
                    return highQualityHeight;
                case VRSystem.QualityLevel.Low:
                    return lowQualityHeight;
                default:
                    Debug.LogWarning("Undefined quality level!");
                    return lowQualityHeight;
            }
        }
    }

    // Need to do this weird dance to avoid being spammed with warnings about sendmessage not working OnValidate
    void OnValidate() {
#if UNITY_EDITOR
        // During build we don't get the delayCall on time, so we must update the mesh immediately.
        // This does not throw any warnings and seems okay.
        if (UnityEditor.BuildPipeline.isBuildingPlayer)
        {
            SetupLayers(LayerCount, GrassHeight);
        }
        else
#endif
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    SetupLayers(LayerCount, GrassHeight);
                }
            };
        }


        // In case we're already subscribed, avoid adding additional events
        // Can't do this with a state bool, since unity sometimes persists it between script reloads
        UnityEditor.Lightmapping.bakeStarted -= SetupBeforeBaking;
        UnityEditor.Lightmapping.bakeStarted += SetupBeforeBaking;
        UnityEditor.Lightmapping.bakeCompleted -= CleanupAfterBaking;
        UnityEditor.Lightmapping.bakeCompleted += CleanupAfterBaking;
    }

    void SetupBeforeBaking()
    {
        if (this != null)
        {
            // We must enable culling when baking lighting, otherwise we get darkened edges around the grass mesh
            //SetupCulling(true);
            SetupLayers(1, 0f);
        }
        else
        {
            UnityEditor.Lightmapping.bakeStarted -= SetupBeforeBaking;
        }
    }

    void CleanupAfterBaking()
    {
        if (this != null)
        {
            //SetupCulling(false);
            SetupLayers(LayerCount, GrassHeight);
        }
        else
        {
            UnityEditor.Lightmapping.bakeStarted -= CleanupAfterBaking;
        }
    }

    private void SetupCulling(bool cullEnabled)
    {
        var renderer = GetComponent<Renderer>();
        renderer.sharedMaterials[targetSubmesh].SetInt(cullProperty, (int)(cullEnabled ? CullMode.Front : CullMode.Off));
    }

    private void SetupLayers(int layerCount, float grassHeight)
    {
        if (grassFilter == null)
        {
            grassFilter = GetComponent<MeshFilter>();
            originalMesh = grassFilter.sharedMesh;
        }

        if (grassFilter != null)
        {
            targetSubmesh = Mathf.Clamp(targetSubmesh, 0, originalMesh.subMeshCount - 1);
            grassFilter.mesh = SetupMesh(originalMesh, layerCount, grassHeight, targetSubmesh, gameObject);
        }
    }

    static Mesh SetupMesh(Mesh originalMesh, int layerCount, float grassHeight, int targetSubmeshIndex, GameObject targetGameObject)
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSaveInEditor;
        var distanceBetweenLayers = grassHeight / layerCount;

        UnityEngine.Rendering.SubMeshDescriptor[] descriptors = new UnityEngine.Rendering.SubMeshDescriptor[originalMesh.subMeshCount];

        int indexOffset = 0;
        int vertexOffset = 0;
        int finalVertexCount = 0;
        int finalIndexCount = 0;
        for(int i = 0; i < originalMesh.subMeshCount; i++)
        {
            var descriptor = originalMesh.GetSubMesh(i);
            if (i == targetSubmeshIndex)
            {
                indexOffset = descriptor.indexCount * (layerCount - 1);
                vertexOffset = descriptor.indexCount * (layerCount - 1);
                var originalIndexCount = descriptor.indexCount;
                descriptor.indexCount = descriptor.indexCount * layerCount;
                descriptor.vertexCount = descriptor.vertexCount + originalIndexCount * (layerCount - 1);
                finalVertexCount = descriptor.firstVertex + descriptor.vertexCount;
            }
            else
            {
                descriptor.indexStart += indexOffset;
                finalVertexCount = descriptor.firstVertex + descriptor.vertexCount + vertexOffset;
            }
            finalIndexCount += descriptor.indexCount;
            // Can't set submesh before we set the indices, so cache the value
            descriptors[i] = descriptor;
        }


        var extrudedMesh = descriptors[targetSubmeshIndex];
        // Our extruded mesh will be referencing the vertices that we add at the end of the array
        extrudedMesh.vertexCount = finalVertexCount - extrudedMesh.firstVertex;
        descriptors[targetSubmeshIndex] = extrudedMesh;
        var customVertexStart = finalVertexCount - vertexOffset;

        var originalVertices = originalMesh.vertices;
        var originalTriangles = originalMesh.triangles;
        var originalNormals = originalMesh.normals;
        var originalColors = originalMesh.colors;
        var originalUVs = originalMesh.uv;
        var originalUV2s = originalMesh.uv2;
        var originalUV3s = originalMesh.uv3;
        var originalUV4s = originalMesh.uv4;

        var finalVertices = new Vector3[finalVertexCount];
        var finalTriangles = new int[finalIndexCount];
        var finalNormals = new Vector3[finalVertexCount];
        var finalColors = new Color[finalVertexCount];
        var finalUVs = new Vector2[finalVertexCount];
        var finalUV2s = new Vector2[finalVertexCount];
        var finalUV3s = new Vector2[finalVertexCount];
        var finalUV4s = new Vector2[finalVertexCount];

        var targetSubmesh = originalMesh.GetSubMesh(targetSubmeshIndex);

        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            var currentSubmesh = descriptors[i];
            var originalSubmesh = originalMesh.GetSubMesh(i);

            //Copy existing mesh data
            for (int index = originalSubmesh.firstVertex; index < originalSubmesh.firstVertex + originalSubmesh.vertexCount; index++)
            {
                finalVertices[index] = originalVertices[index];
                finalNormals[index] = originalNormals[index];
                CopyUVs(finalUVs, index, originalUVs, index);
                CopyUVs(finalUV2s, index, originalUV2s, index);
                CopyUVs(finalUV3s, index, originalUV3s, index);
                CopyUVs(finalUV4s, index, originalUV4s, index);
                var original = originalColors.Length > 0 ? originalColors[index] : Color.black;

                // Check if we're on the grass submesh and set the vertex color accordingly. Some meshes reuse vertices, so we need to check the vertex index directly.
                if (index >= targetSubmesh.firstVertex && index < targetSubmesh.firstVertex + targetSubmesh.vertexCount)
                {
                    //Write depth data for our extruded submesh
                    finalColors[index] = new Color(0.0f, original.g, original.b, original.a);
                }
                else
                {
                    finalColors[index] = original;
                }
            }
            for (int index = 0; index < originalSubmesh.indexCount; index++)
            {
                var finalIndex = currentSubmesh.indexStart + index;
                var originalIndex = originalSubmesh.indexStart + index;
                finalTriangles[finalIndex] = originalTriangles[originalIndex];
            }

            // Append our whacky crazy extruded data to the end
            if (i == targetSubmeshIndex)
            {
                //var verticesPerLayer = originalSubmesh.vertexCount;
                var indicesPerLayer = originalSubmesh.indexCount;

                for(int z = 1; z < layerCount; z++)
                {
                    var depth = (float)z / (layerCount - 1);
                    for (int index = 0; index < indicesPerLayer; index++)
                    {
                        var finalIndex = customVertexStart + indicesPerLayer * (z - 1) + index;
                        var originalIndex = originalTriangles[originalSubmesh.indexStart + index];
                        var originalCol = originalColors.Length > 0 ? originalColors[originalIndex] : Color.black;
                        finalVertices[finalIndex] = originalVertices[originalIndex] + originalNormals[originalIndex] * distanceBetweenLayers * (1f - originalCol.r) * z;
                        finalNormals[finalIndex] = originalNormals[originalIndex];
                        CopyUVs(finalUVs, finalIndex, originalUVs, originalIndex);
                        CopyUVs(finalUV2s, finalIndex, originalUV2s, originalIndex);
                        CopyUVs(finalUV3s, finalIndex, originalUV3s, originalIndex);
                        CopyUVs(finalUV4s, finalIndex, originalUV4s, originalIndex);
                        finalColors[finalIndex] = new Color(depth, originalCol.g, originalCol.b, originalCol.a);
                    }

                    for (int index = 0; index < indicesPerLayer; index++)
                    {
                        var finalIndex = originalSubmesh.indexStart + indicesPerLayer * z + index;
                        finalTriangles[finalIndex] = customVertexStart + index + indicesPerLayer * (z - 1);
                    }
                }
            }
        }

        if (finalVertices.Length > System.UInt16.MaxValue)
        {
            Debug.LogWarning($"Mesh {originalMesh} generates a very large number of grass vertices! This might cause bad performance.", targetGameObject);
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = finalVertices;
        mesh.triangles = finalTriangles;
        mesh.normals = finalNormals;
        mesh.colors = finalColors;
        SetUVs(mesh, originalMesh, finalUVs, finalUV2s, finalUV3s, finalUV4s);

        mesh.subMeshCount = descriptors.Length;
        for(int i = 0; i < descriptors.Length; i++)
        {
            mesh.SetSubMesh(i, descriptors[i]);
        }

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }

    static void CopyUVs(Vector2[] to, int toIndex, Vector2[] from, int fromIndex)
    {
        if (from.Length > 0)
        {
            to[toIndex] = from[fromIndex];
        }
    }

    static void SetUVs(Mesh mesh, Mesh originalMesh, Vector2[] finalUVs, Vector2[] finalUV2s, Vector2[] finalUV3s, Vector2[] finalUV4s)
    {
        if (originalMesh.uv.Length > 0)
            mesh.uv = finalUVs;
        if (originalMesh.uv2.Length > 0)
            mesh.uv2 = finalUV2s;
        if (originalMesh.uv3.Length > 0)
            mesh.uv2 = finalUV3s;
        if (originalMesh.uv4.Length > 0)
            mesh.uv2 = finalUV4s;
    }
    #endif
}
