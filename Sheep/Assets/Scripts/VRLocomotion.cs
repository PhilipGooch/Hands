using NBG.Core;
using Unity.Mathematics;
using UnityEngine;
using VR.System;

public enum LocomotionMode
{
    ROOM_SCALE,
    JOYSTICK,
    TELEPORTATION
}

public class VRLocomotion : MonoBehaviour
{
    public Player player;
    //public Hand left;
    //public Hand right;

    bool grabL = false;
    bool grabR = false;
    float3 posL;
    float3 posR;

    public bool LimitedMovement { private get; set; }

    public void ResetPosition()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }


    internal void OnFixedStep(ref RigidTransform xL, ref Vector6 vL, ref RigidTransform xR, ref Vector6 vR)
    {
        if (LimitedMovement)
            return;

        var left = player.leftHand;
        var right = player.rightHand;
        //var poseL = (Vector3)xL.pos;
        //var poseR = xR.pos;
        if (left.GetInput(HandInputType.trackpadDown))
        {
            grabL = true;
            posL = xL.pos;
        }
        if (right.GetInput(HandInputType.trackpadDown))
        {
            grabR = true;
            posR = xR.pos;
        }

        //velocity = Vector6.zero;
        if (grabL && grabR)
        {
            var oldcenter = (posL + posR) / 2;
            var center = (xL.pos + xR.pos) / 2;
            var deltaRot = Quaternion.FromToRotation(((Vector3)(xR.pos - xL.pos)).ZeroY(), ((Vector3)(posR - posL)).ZeroY());
            var deltaPos = oldcenter - center;// (posR + posL - xR.pos - xL.pos) / 2;

            var T = new RigidTransform(deltaRot, math.rotate(deltaRot, -oldcenter) + oldcenter + deltaPos); // rotate around oldcenter, then move by deltaPos


            transform.rotation = deltaRot * transform.rotation;
            transform.position = math.transform(T, transform.position);
            xL = math.mul(T, xL);
            xR = math.mul(T, xR);

            // adjust velocity
            var velL = vL.linear;
            var velR = vR.linear;
            var linear = (velL + velR) / 2;
            var angular = Vector3.Cross(xR.pos - xL.pos, velR - velL) / math.lengthsq(xL.pos - xR.pos);

            var velocity = new Vector6(angular, linear);
            var l = new PluckerTranslate(.5f * xL.pos - .5f * xR.pos).TransformVelocity(velocity);
            var r = new PluckerTranslate(.5f * xR.pos - .5f * xL.pos).TransformVelocity(velocity);
            vL.angular -= l.angular; vL.linear -= l.linear;
            vR.angular -= r.angular; vR.linear -= r.linear;
        }
        else if (grabL)
        {
            var deltaPos = posL - xL.pos;
            vR.linear -= vL.linear;
            vL.linear = float3.zero;

            transform.position += (Vector3)deltaPos;
            xL.pos += deltaPos;
            xR.pos += deltaPos;

        }
        else if (grabR)
        {
            var deltaPos = posR - xR.pos;
            vL.linear -= vR.linear;
            vR.linear = float3.zero;
            transform.position += (Vector3)deltaPos;
            xL.pos += deltaPos;
            xR.pos += deltaPos;
        }

        if (left.GetInput(HandInputType.trackpadDown))
            posL = xL.pos;

        if (right.GetInput(HandInputType.trackpadDown))
            posR = xR.pos;

        if (grabR && right.GetInput(HandInputType.trackpadUp))
        {
            grabR = false;
            posL = xL.pos; // recapture position to prevent snap
        }
        if (grabL && left.GetInput(HandInputType.trackpadUp))
        {
            grabL = false;
            posR = xR.pos;// recapture position to prevent snap

        }

    }


}
