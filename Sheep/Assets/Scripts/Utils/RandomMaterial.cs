using UnityEngine;

public class RandomMaterial : MonoBehaviour
{
    [SerializeField]
    Material[] materials;
    
    void Awake()
    {
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        Material material = materials[Random.Range(0, materials.Length)];
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.material = material;
        }
    }
}

