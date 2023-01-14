using NBG.Entities;
using Noodles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

public class CameraOverrideOnTrigger : MonoBehaviour, ICameraModeOverride
{
    [SerializeField]
    private CameraMode _cameraMode;
    public CameraMode cameraMode => _cameraMode;
    public int priority => (int)_cameraMode;
    TriggerProximityList<Noodle> nearbyNoodles = new TriggerProximityList<Noodle>(ProximityListComponentSearch.InParents);

    private void OnTriggerEnter(Collider other)
    {
        var noodle = nearbyNoodles.OnTriggerEnter(other);
        if (noodle != null)
            CameraOverrideList<ICameraModeOverride>.AddOverride(noodle.entity, this);
    }
    private void OnTriggerExit(Collider other)
    {
        var noodle = nearbyNoodles.OnTriggerLeave(other);
        if (noodle != null)
            CameraOverrideList<ICameraModeOverride>.RemoveOverride(noodle.entity, this);
    }
}
