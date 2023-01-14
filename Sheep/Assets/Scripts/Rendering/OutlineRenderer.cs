using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineRenderer
{
    List<MeshFilter> meshFilters = new List<MeshFilter>();
    List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();

    Dictionary<int, Material[]> outlineMaterialArrays = new Dictionary<int, Material[]>();
    List<Material> tempMaterialList = new List<Material>();
    List<Renderer> tempRendererList = new List<Renderer>();
    Material outlineMaterial;

    int activeMeshRenderers = 0;
    int activeSkinnedMeshRenderers = 0;

    public OutlineRenderer()
    {
        CreateMeshRenderer();
        CreateSkinnedMeshRenderer();
        outlineMaterial = GameParameters.Instance.objectOutlineMaterial;
        HideOutlines();
    }

    void CreateMeshRenderer()
    {
        var meshGO = new GameObject("Mesh Outline Renderer");
        meshGO.hideFlags = HideFlags.HideInHierarchy;
        GameObject.DontDestroyOnLoad(meshGO);
        var meshFilter = meshGO.AddComponent<MeshFilter>();
        var meshRenderer = meshGO.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.enabled = false;
        meshFilters.Add(meshFilter);
        meshRenderers.Add(meshRenderer);
    }

    void CreateSkinnedMeshRenderer()
    {
        var skinnedGO = new GameObject("Skinned Mesh Outline Renderer");
        skinnedGO.hideFlags = HideFlags.HideInHierarchy;
        GameObject.DontDestroyOnLoad(skinnedGO);
        var skinnedMeshRenderer = skinnedGO.AddComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        skinnedMeshRenderer.enabled = false;
        skinnedMeshRenderers.Add(skinnedMeshRenderer);
    }

    public void ShowOutlines(GrabParamsBinding grabParams)
    {
        HideOutlines();

        grabParams.GetComponentsInChildren(tempRendererList);

        for(int i = 0; i < tempRendererList.Count; i++)
        {
            var rend = tempRendererList[i];
            if (rend.enabled)
            {
                var m = rend as MeshRenderer;
                if (m != null)
                {
                    ShowOutline(m);
                }
                else
                {
                    var skinned = rend as SkinnedMeshRenderer;
                    if (skinned != null)
                    {
                        ShowOutline(skinned);
                    }
                }
            }
        }
    }

    void ShowOutline(MeshRenderer target)
    {
        if (activeMeshRenderers >= meshRenderers.Count)
        {
            CreateMeshRenderer();
        }

        var meshRenderer = meshRenderers[activeMeshRenderers];
        meshRenderer.enabled = true;
        CopyTransform(target.transform, meshRenderer.transform);
        CopyFilter(target.GetComponent<MeshFilter>(), meshFilters[activeMeshRenderers]);
        CopyMeshRenderer(target, meshRenderer);
        activeMeshRenderers++;
    }

    void ShowOutline(SkinnedMeshRenderer target)
    {
        if (activeSkinnedMeshRenderers >= skinnedMeshRenderers.Count)
        {
            CreateSkinnedMeshRenderer();
        }

        var skinnedMeshRenderer = skinnedMeshRenderers[activeSkinnedMeshRenderers];
        skinnedMeshRenderer.enabled = true;
        CopyTransform(target.transform, skinnedMeshRenderer.transform);
        CopySkinnedMeshRenderer(target, skinnedMeshRenderer);
        activeSkinnedMeshRenderers++;
    }

    public void HideOutlines()
    {
        for(int i = 0; i < activeMeshRenderers; i++)
        {
            meshRenderers[i].enabled = false;
        }
        for(int i = 0; i < activeSkinnedMeshRenderers; i++)
        {
            skinnedMeshRenderers[i].enabled = false;
        }

        activeMeshRenderers = 0;
        activeSkinnedMeshRenderers = 0;
    }

    void CopyTransform(Transform from, Transform to)
    {
        to.SetPositionAndRotation(from.position, from.rotation);
        to.localScale = from.lossyScale;
    }

    void CopyFilter(MeshFilter from, MeshFilter to)
    {
        if (from != null)
        {
            to.sharedMesh = from.sharedMesh;
        }
        else
        {
            to.sharedMesh = null;
        }
    }

    void CopyMeshRenderer(MeshRenderer from, MeshRenderer to)
    {
        tempMaterialList.Clear();
        from.GetSharedMaterials(tempMaterialList);
        var outlines = GetOutlineMaterials(tempMaterialList.Count);
        for(int i = 0; i < outlines.Length; i++)
        {
            outlines[i].mainTexture = tempMaterialList[i].mainTexture;
        }
        to.sharedMaterials = outlines;
    }

    void CopySkinnedMeshRenderer(SkinnedMeshRenderer from, SkinnedMeshRenderer to)
    {
        if (from.rootBone != to.rootBone)
        {
            tempMaterialList.Clear();
            from.GetSharedMaterials(tempMaterialList);
            to.sharedMaterials = GetOutlineMaterials(tempMaterialList.Count);
            to.sharedMesh = from.sharedMesh;
            to.rootBone = from.rootBone;
            // LEAK!
            to.bones = from.bones;
        }
    }

    Material[] GetOutlineMaterials(int count)
    {
        if (!outlineMaterialArrays.ContainsKey(count))
        {
            Material[] mats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                mats[i] = new Material(outlineMaterial);
            }
            outlineMaterialArrays[count] = mats;
        }
        return outlineMaterialArrays[count];
    }
}
