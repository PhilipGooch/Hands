using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuPortalHelper : MonoBehaviour
{
    List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    [SerializeField]
    Material[] originalMaterials;
    [HideInInspector]
    [SerializeField]
    new Renderer renderer;
    [SerializeField]
    [HideInInspector]
    int targetStencil = -1;
    [SerializeField]
    bool refresh;

    private void Awake()
    {
        SetupMaterialInstances();
    }

    void SetupMaterialInstances()
    {
        if (renderer != null && originalMaterials != null && originalMaterials.Length > 0 && targetStencil > 0)
        {
            var instances = new Material[originalMaterials.Length];
            for(int i = 0; i < originalMaterials.Length; i++)
            {
                var matInstance = Instantiate(originalMaterials[i]);
                matInstance.SetFloat("_StencilMask", targetStencil);
                matInstance.SetFloat("_StencilComp", 3);
                matInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 3;
                instances[i] = matInstance;
            }
            renderer.sharedMaterials = instances;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                _OnValidate();
            }
        };
    }

    private void _OnValidate()
    {
        if (renderer == null)
        {
            renderer = GetComponent<Renderer>();
        }

        if (renderer)
        {
            Shader stencilWriteShader = Shader.Find("Universal Render Pipeline/Menu Stencil Writer");
            Shader painterlyShader = Shader.Find("Universal Render Pipeline/Lit Painterly");

            // This happens after applying a material instance on a prefab. In that case, bring back the original material.
            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
            {
                renderer.sharedMaterials = originalMaterials;
            }

            if ((originalMaterials == null || originalMaterials.Length == 0) && renderer.sharedMaterial != null && UnityEditor.AssetDatabase.Contains(renderer.sharedMaterial))
            {
                originalMaterials = renderer.sharedMaterials;
            }

            if (originalMaterials != null && originalMaterials.Length > 0)
            {
                GetComponentsInParent(false, meshRenderers);

                foreach (var parentRenderer in meshRenderers)
                {
                    if (parentRenderer.sharedMaterial != null)
                    {
                        if (parentRenderer.sharedMaterial.shader == stencilWriteShader)
                        {
                            targetStencil = Mathf.RoundToInt(parentRenderer.sharedMaterial.GetFloat("_StencilMask"));
                            break;
                        }
                    }
                }
            }
        }

        SetupMaterialInstances();
    }
    #endif
}
