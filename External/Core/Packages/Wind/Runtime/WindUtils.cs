using System;
using UnityEngine;

namespace NBG.Wind
{
    internal static class WindUtils
    {
        internal static int GetBodyRigidbodyCount(this Rigidbody body)
        {
            int bodycount = 1;
            Rigidbody parentRig = body.transform.GetComponentInParent<Rigidbody>();
            if (parentRig == null)
                return bodycount;

            Rigidbody rootRigidbody = parentRig;
            while (parentRig != null)
            {
                if (parentRig.transform.parent == null)
                    break;

                if (parentRig.transform.parent.TryGetComponent(out parentRig))
                {
                    rootRigidbody = parentRig;
                }
            }

            int count = 0;
            rootRigidbody.transform.GetRigidbodiesInChildrenCount(ref count);
            return count;
        }

        //does not generate garbage
        static void GetRigidbodiesInChildrenCount(this Transform transform, ref int count)
        {
            if (transform.TryGetComponent<Rigidbody>(out _))
                count++;

            for (int i = 0; i < transform.childCount; i++)
            {
                GetRigidbodiesInChildrenCount(transform.GetChild(i), ref count);
            }
        }

        internal static Func<RaycastHit, RaycastHit, bool> Comparer = (a, b) => a.distance > b.distance;
    }
}
