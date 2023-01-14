using NBG.LogicGraph;
using System;
using UnityEngine;

public class FlickQuadSwitchActivator : FlickSwitchBase, IProjectHandAnchor
{
    public enum FlickSwitchDirection
    {
        Forward,
        Sideways
    }

    [NodeAPI("OnForwardActivationChange")]
    public event Action<float> forwardOutput;
    [NodeAPI("OnSidewaysActivationChange")]
    public event Action<float> sidewaysOutput;

    protected Action<float> currentOutput;

    [SerializeField]
    private FlickSwitchDirection flickSwitchStartDirection;

    private bool IsInNeutral => lastSnapPosition == snapPositionCount / 2;

    protected override void StartSetup()
    {
        SetDirectionData(flickSwitchStartDirection);
        SetAngle(GetDegreesFromStep(startSnapPosition), anchorRotationAxis);
        UpdateActivation(startSnapPosition);
    }

    void SetDirectionData(FlickSwitchDirection direction)
    {
        if (direction == FlickSwitchDirection.Forward)
        {
            anchorRotationAxis = Vector3.right;
            flickDirection = Vector3.forward;
            currentOutput = forwardOutput;
        }
        else
        {
            anchorRotationAxis = -Vector3.forward;
            flickDirection = Vector3.right;
            currentOutput = sidewaysOutput;
        }
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        if (runAnimation)
            return;

        if (!IsInNeutral)
        {
            var signedHandPosSide = GetSignedMagnitudeOnAxis(vrPos, Parent.TransformDirection(flickDirection));
            UpdateSnap(signedHandPosSide, anchorRotationAxis);
        }
        //if switch is not in neutral, need to decide in which direction it should go
        else
        {
            var signedHandPosForward = GetSignedMagnitudeOnAxis(vrPos, Parent.TransformDirection(Vector3.forward));
            var signedHandPosSide = GetSignedMagnitudeOnAxis(vrPos, Parent.TransformDirection(Vector3.right));

            //check which direction is dominant, try and snap switch in that direction
            if (Mathf.Abs(signedHandPosForward) > Mathf.Abs(signedHandPosSide))
            {
                SetDirectionData(FlickSwitchDirection.Forward);
                UpdateSnap(signedHandPosForward, anchorRotationAxis);
            }
            else
            {
                SetDirectionData(FlickSwitchDirection.Sideways);
                UpdateSnap(signedHandPosSide, anchorRotationAxis);
            }
        }
    }

    protected override void UpdateActivation(int step)
    {
        //Update activation. Neutral activation is 0.5 if the object has an uneven amount of snap positions AND neutral position doesnt exist if theres an even amount of snap positions.
        //HOWEVER, quad switch should never have an even amount of snap positions.
        var activationValue = invertValue ? 1 - GetNormalizedStep(step) : GetNormalizedStep(step);

        //Because there are 4 directions, all directions activation has to be reset when reaching neutral

        currentOutput?.Invoke(activationValue);

        if (IsInNeutral)
        {
            if (currentOutput == forwardOutput)
            {
                sidewaysOutput?.Invoke(0.5f);
            }
            else
            {
                forwardOutput?.Invoke(0.5f);
            }
        }
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        //draw ray to display total distance hands needs to travel in order to fully switch
        {
            var totalArcLength = GetArcLength(rotationArcDegrees);
            var dir = Parent.TransformDirection(Vector3.right);
            Gizmos.DrawRay(Parent.position + dir * (-totalArcLength / 2), dir * totalArcLength);
        }

        //draw rays to display current rotationArcDegrees
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(Parent.position, Quaternion.AngleAxis(rotationArcDegrees / 2, Parent.forward) * Parent.up);
            Gizmos.DrawRay(Parent.position, Quaternion.AngleAxis(-rotationArcDegrees / 2, Parent.forward) * Parent.up);
        }
    }
}
