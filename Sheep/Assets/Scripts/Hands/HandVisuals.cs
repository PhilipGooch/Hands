using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VR.System;

public class HandVisuals : MonoBehaviour
{
    HandDirection handDirection;
    //OutlineRenderer nearbyObjectOutlines;

    float recoverPhase = 0f;
    Vector3 recoverFromPosition;
    Quaternion recoverFromRotation;

    Transform Model => VRSystem.Instance.GetModel(handDirection);

    private void Awake()
    {
        //nearbyObjectOutlines = new OutlineRenderer();
        handDirection = GetComponent<Hand>().handDirection;
    }

    public void RecoverVisualPosition()
    {
        recoverPhase = 0;
        recoverFromPosition = Model.localPosition;
        recoverFromRotation = Model.localRotation;
    }

    public void UpdateVisuals(Vector3 pos, Quaternion rot, ReBody attachedReBody, GrabParams grabParams, Vector3 attachedAnchorPos, Vector3 anchorPos, Quaternion anchorRot, bool HoldingObjectWithTwoHands)
    {
        recoverPhase += Time.deltaTime / .25f; // 250ms
        if (recoverPhase > 1) recoverPhase = 1;
        var mix = 1 - (1 - recoverPhase) * (1 - recoverPhase); // easeout quad

        if (!attachedReBody.BodyExists) // no object - use controller position / rotation
        {
            Model.localPosition = Vector3.Lerp(recoverFromPosition, Vector3.zero, mix);
            Model.localRotation = Quaternion.Slerp(recoverFromRotation, Quaternion.identity, mix);

            transform.rotation = rot;// model.transform.rotation;
            transform.position = pos;// model.transform.position;
        }
        else if (attachedReBody.isKinematic) // kinematic object - glue visuals to object
        {
            transform.rotation = Model.rotation = rot;
            transform.position = Model.position = pos;
        }
        else if (HoldingObjectWithTwoHands || !grabParams.angularControl) // two hand - position glues to object, but rotation from controller
        {
            transform.position = Model.position = attachedReBody.TransformPoint(attachedAnchorPos) - Model.rotation * anchorPos;

            Model.localRotation = Quaternion.Slerp(recoverFromRotation, Quaternion.identity, mix);
            transform.rotation = rot;// model.transform.rotation;
        }
        else // normal object - glue visuals to it
        {
            transform.rotation = Model.rotation = attachedReBody.rotation * Quaternion.Inverse(anchorRot);
            transform.position = Model.position = attachedReBody.TransformPoint(attachedAnchorPos) - Model.rotation * anchorPos;
        }
    }

    public void UpdateOutline(Collider nearestCollider, Rigidbody attachedBody, bool isThreat)
    {
        //nearbyObjectOutlines.HideOutlines();
        //
        //if (attachedBody != null)
        //{
        //    var grabParams = attachedBody.GetComponent<GrabParamsBinding>();
        //    if (grabParams != null)
        //    {
        //        nearbyObjectOutlines.ShowOutlines(grabParams);
        //    }
        //}
        //else
        //{
        //    if (nearestCollider != null && !isThreat)
        //    {
        //        var grabParams = nearestCollider.GetComponentInParent<GrabParamsBinding>();
        //        if (grabParams != null && grabParams.Grabbable)
        //        {
        //            nearbyObjectOutlines.ShowOutlines(grabParams);
        //        }
        //    }
        //}
    }
}
