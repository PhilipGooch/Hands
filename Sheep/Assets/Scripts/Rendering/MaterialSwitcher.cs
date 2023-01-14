using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSwitcher : MonoBehaviour
{
    [SerializeField]
    Material steamVRMaterial;
    [SerializeField]
    Material oculusVRMaterial;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Material targetMaterial = null;
#if STEAMVR
        targetMaterial = steamVRMaterial;
#elif OCULUSVR
        targetMaterial = oculusVRMaterial;
#endif
        if (targetMaterial != null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = targetMaterial;
            }
        }
    }
#endif
}
