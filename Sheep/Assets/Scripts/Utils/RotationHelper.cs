using UnityEngine;
using Recoil;

public static class RotationHelper
{
    public static (Vector3 position, Quaternion rotation) GetOffsetPositionAndRotation(
        Vector3 targetRootPos, Quaternion targetRootRotation,
        Vector3 currentRootPosition, Quaternion currentRootRotation,
        Vector3 currentChildPosition, Quaternion currentChildRotation)
    {
        //rotate root back to 0 0 0
        (currentChildPosition, currentChildRotation) = GetOffsetPositionAndRotationInternal(Quaternion.identity, currentRootPosition, currentRootRotation, currentChildPosition, currentChildRotation);
        //rotate to target position
        (currentChildPosition, currentChildRotation) = GetOffsetPositionAndRotationInternal(targetRootRotation, currentRootPosition, Quaternion.identity, currentChildPosition, currentChildRotation);
        //need to actually move it to target position
        var offset = targetRootPos - currentRootPosition;

        return (currentChildPosition + offset, currentChildRotation);
    }

    static (Vector3 position, Quaternion rotation) GetOffsetPositionAndRotationInternal(
         Quaternion targetRootRotation,
         Vector3 currentRootPosition, Quaternion currentRootRotation,
         Vector3 currentChildPosition, Quaternion currentChildRotation)
    {
        var positionOffset = GetPositionOffset(currentChildPosition, currentRootPosition);
        var rotationOffset = GetRotationOffset(currentChildRotation, currentRootRotation);
        var deltaRotation = GetDeltaRotation(targetRootRotation, currentRootRotation);

        currentChildRotation = targetRootRotation * rotationOffset;
        currentChildPosition = currentRootPosition + deltaRotation * positionOffset;

        return (currentChildPosition, currentChildRotation);
    }

    public static Quaternion GetDeltaRotation(Quaternion targetPointRotation, Quaternion rootObjectRotation)
    {
        return Quaternion.Inverse(rootObjectRotation) * targetPointRotation;
    }

    public static Vector3 GetPositionOffset(Vector3 childObjectPosition, Vector3 rootObjectPosition)
    {
        return childObjectPosition - rootObjectPosition;
    }

    public static Quaternion GetRotationOffset(Quaternion childObjectRotation, Quaternion rootObjectRotation)
    {
        return Quaternion.Inverse(rootObjectRotation) * childObjectRotation;
    }

    public static Quaternion RotateToAxisWithoutTwisting(Quaternion currentRotation, Vector3 worldAxis)
    {
        var targetRot = Quaternion.LookRotation(worldAxis, Vector3.up);
        var deltaRot = targetRot * Quaternion.Inverse(currentRotation);
        re.ToSwingTwist(deltaRot, worldAxis, out var swing, out var twist);
        return Quaternion.Inverse(twist) * targetRot;
    }
}
