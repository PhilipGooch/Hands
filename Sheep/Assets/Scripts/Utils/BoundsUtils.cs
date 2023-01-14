using System.Collections.Generic;
using UnityEngine;

//These might not always be good to use in production, but very nice to have for testing
public static class BoundsUtils
{
    //cannot be returned or accessed from elsewhere.
    static List<Collider> colliders = new List<Collider>();

    public static Bounds GetCombinedBoundsFromColliders(GameObject root)
    {
        colliders.Clear();

        Bounds bounds = new Bounds();

        root.GetComponentsInChildren(colliders);
        for (int i = 0; i < colliders.Count; i++)
        {
            if (i == 0)
                bounds = colliders[i].bounds;
            else
                bounds.Encapsulate(colliders[i].bounds);

        }

        return bounds;
    }
}
