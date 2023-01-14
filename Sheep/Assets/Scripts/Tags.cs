using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tags 
{
    public static bool IsPlayer(GameObject go)
    {
        return go.CompareTag("Player");
    }
    public static bool IsGrab(GameObject go)
    {
        return false;
//        return !go.CompareTag("noGrab") &&
//            (go.CompareTag("grab") ||
//#if CHEATS_ENABLED
//            go.CompareTag("sheep") ||
//#endif
//            go.GetComponentInParent<GrabParamsBinding>() != null ||
//            go.GetComponentInParent<GrabbableImpaler>() != null);
    }
    public static bool IsSheep(GameObject go)
    {
        return go.CompareTag("sheep");
    }
}
