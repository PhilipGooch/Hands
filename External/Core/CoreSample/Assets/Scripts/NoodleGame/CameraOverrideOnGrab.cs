using NBG.Entities;
using Noodles;
using UnityEngine;

public class CameraOverrideOnGrab : MonoBehaviour, Noodles.IGrabbable, ICameraModeOverride
{
    public int handsNeeded = 1;
    [SerializeField]
    private CameraMode _cameraMode;
    public CameraMode cameraMode => _cameraMode;
    public int priority => (int)_cameraMode;

    int grabCount = 0;
    // Start is called before the first frame update
    public void OnGrab(Entity noodle, NoodleHand hand)
    {
        grabCount++;
        if (grabCount == handsNeeded)
            CameraOverrideList<ICameraModeOverride>.AddOverride(noodle, this);
    }

    public void OnRelease(Entity noodle, NoodleHand hand)
    {
        grabCount--;
        if (grabCount == handsNeeded - 1)
            CameraOverrideList<ICameraModeOverride>.RemoveOverride(noodle, this);
    }

}
