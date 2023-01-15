using UnityEngine;

public interface ITriggerHandler
{
    void OnHandTrigger(float pressure);
}

public interface IProjectOnCollider
{
    Vector3 Project(Vector3 wordPos);
}

public interface IPositionBasedDynamics
{
    void ApplyPosition(Vector3 pos, Quaternion rot, Vector3 anchor);
}

public interface IOverrideGrabAnchor
{
    /// <summary>
    /// Allows to change grab anchor position and rotation. Gives world position and rotation, expects world position and rotation to be returned.
    /// </summary>
    /// <param name="currentGrabPosition">grab world position at grab frame</param>
    /// <param name="currentGrabRotation">hand world rotation at grab frame</param>
    (Vector3 grabPosition, Quaternion grabRotation) Reanchor(Vector3 currentGrabPosition, Quaternion currentGrabRotation);
}
