using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapMaterialsNode : ActivatableNode
{
    [SerializeField]
    float swapThreshold = 0.5f;
    [SerializeField]
    float chanceToSwapToOnMaterial = 1f;
    [SerializeField]
    float chanceToSwapToOffMaterial = 1f;
    [SerializeField]
    Material offMaterial;
    [SerializeField]
    Material onMaterial;
    [SerializeField]
    List<MeshRenderer> targets;

    List<MeshRenderer> swappedTargets = new List<MeshRenderer>();

    // Update is called once per frame
    void Update()
    {
        if (ActivationValue >= swapThreshold)
        {
            SwapMaterial(targets, swappedTargets, onMaterial, chanceToSwapToOnMaterial);
        }
        else
        {
            SwapMaterial(swappedTargets, targets, offMaterial, chanceToSwapToOffMaterial);
        }
    }

    static void SwapMaterial(List<MeshRenderer> from, List<MeshRenderer> to, Material material, float chance)
    {
        for (int i = from.Count - 1; i >= 0; i--)
        {
            var target = from[i];
            if (Random.value <= chance)
            {
                target.material = material;
                to.Add(target);
                from.RemoveAt(i);
            }
        }
    }
}
