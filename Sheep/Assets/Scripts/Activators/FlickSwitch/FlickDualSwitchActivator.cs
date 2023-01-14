using NBG.LogicGraph;
using System;
using UnityEngine;

public class FlickDualSwitchActivator : FlickSwitchBase, IProjectHandAnchor
{
    [NodeAPI("OnActivationChange")]
    public event Action<float> onActivationChange;

    protected override void StartSetup()
    {
        anchorRotationAxis = Vector3.right;
        flickDirection = Vector3.forward;

        SetAngle(GetDegreesFromStep(startSnapPosition), anchorRotationAxis);
        UpdateActivation(startSnapPosition);
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        if (runAnimation)
            return;

        var signedHandPosSide = GetSignedMagnitudeOnAxis(vrPos, Parent.TransformDirection(flickDirection));
        UpdateSnap(signedHandPosSide, anchorRotationAxis);
    }

    protected override void UpdateActivation(int step)
    {
        var activation = invertValue ? 1 - GetNormalizedStep(step) : GetNormalizedStep(step);
        onActivationChange?.Invoke(activation);
    }
}
