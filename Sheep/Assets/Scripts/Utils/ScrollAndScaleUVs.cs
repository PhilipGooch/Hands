using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollAndScaleUVs : MonoBehaviour
{
    [SerializeField]
    Transform target;
    [SerializeField]
    Vector3 axis = Vector3.up;
    [SerializeField]
    Vector2 uvScrollModifier = new Vector2(0, 0);
    [SerializeField]
    Vector2 uvScaleModifier = new Vector2(0, 1);

    float baseScale;

    Material material;

    // Start is called before the first frame update
    void Awake()
    {
        material = GetComponent<MeshRenderer>().material;
        baseScale = Vector3.Project(target.localScale, axis).magnitude;
    }

    // Update is called once per frame
    void Update()
    {
        var targetScale = Vector3.Project(target.localScale, axis).magnitude / baseScale;
        var invScale = 1f - targetScale;
        Vector2 currentScroll = material.mainTextureOffset;
        Vector2 currentScale = material.mainTextureScale;
        if (uvScrollModifier.x != 0)
        {
            currentScroll.x = invScale * uvScrollModifier.x;
        }
        if (uvScrollModifier.y != 0)
        {
            currentScroll.y = invScale * uvScrollModifier.y;
        }
        if (uvScaleModifier.x != 0)
        {
            currentScale.x = targetScale * uvScaleModifier.x;
        }
        if (uvScaleModifier.y != 0)
        {
            currentScale.y = targetScale * uvScaleModifier.y;
        }
        material.mainTextureOffset = currentScroll;
        material.mainTextureScale = currentScale;
    }
}
