using UnityEngine;
using WindZone = NBG.Wind.WindZone;

/// <summary>
/// Test scripts implements optional networking
/// </summary>
public class WindDemo : WindZone
{
    protected override bool IsBlockerObject(Collider obj)
    {
        return obj.GetComponentInChildren<WindBlockerTag>() != null;
    }

    protected override bool IgnoreObject(Collider obj)
    {
        return obj.GetComponentInChildren<WindIgnoreTag>() != null;

    }
}

