using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableFixed : GrabbableImpaler
{
    protected override void OnImpale()
    {
        base.OnUnimpale();
        rigidbody.isKinematic = true;
    }

    protected override void OnUnimpale()
    {
        base.OnUnimpale();
        rigidbody.isKinematic = false;
    }
}
