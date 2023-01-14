using UnityEngine;

public class Wind : NBG.Wind.WindZone
{
    [SerializeField]
    LayerMask blockerLayers;

    protected override bool IsBlockerObject(Collider obj)
    {
        return LayerUtils.IsPartOfLayer(obj.gameObject.layer, blockerLayers);
    }
}
