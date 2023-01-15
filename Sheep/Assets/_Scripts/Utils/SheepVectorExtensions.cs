using UnityEngine;
using System.Collections;

public static class SheepVectorExtensions 
{
    public static bool IsFinite(this Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsInfinity(v.x)
        && !float.IsNaN(v.y) && !float.IsInfinity(v.y)
        && !float.IsNaN(v.z) && !float.IsInfinity(v.z);
    }

    public static bool AssertIsFinite(this Vector3 v)
    {
        if (!v.IsFinite())
        {
            Debug.LogAssertionFormat("Vector {0} is not finite", v);
            return false;
        }
        return true;
    }
}
