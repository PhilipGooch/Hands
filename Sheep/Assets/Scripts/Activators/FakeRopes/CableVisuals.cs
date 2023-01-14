using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableVisuals : MonoBehaviour
{
    [SerializeField]
    Transform visualizer;
    [SerializeField]
    Transform cableBody;
    [SerializeField]
    bool adjustCableScale = true;
    [SerializeField]
    bool invertScaling = false;
    [SerializeField]
    bool rotateViaOffset = true;

    CableActivator targetCable;
    float lastActivationValue = -1;
    float baseLength = 1f;

    Vector3 CableRootPosition
    {
        get
        {
            return targetCable.GetCableConnectionPosition() + targetCable.WorldAxis * AxisDirectionModifier * LengthFromActivation;
        }
    }

    float LengthFromActivation
    {
        get
        {
            return targetCable.length * (invertScaling ? targetCable.ActivationAmount : 1f - targetCable.ActivationAmount);
        }
    }

    float AxisDirectionModifier
    {
        get
        {
            return invertScaling ? -1f : 1f;
        }
    }


    // Start is called before the first frame update
    void Awake()
    {
        targetCable = GetComponent<CableActivator>();
        if (adjustCableScale)
        {
            var currentRopeSize = transform.InverseTransformVector(cableBody.GetComponent<Collider>().bounds.size);
            var currentRopeScale = cableBody.localScale;
            var unscaledSize = new Vector3(currentRopeSize.x / currentRopeScale.x, currentRopeSize.y / currentRopeScale.y, currentRopeSize.z / currentRopeScale.z);
            baseLength = Vector3.Project(unscaledSize, targetCable.Axis).magnitude;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (visualizer != null)
        {
            visualizer.position = targetCable.GetCableConnectionPosition();
            if (rotateViaOffset)
            {
                visualizer.position += targetCable.EndOffset;
            }
        }

        var worldAxis = targetCable.WorldAxis;
        var activationAmount = targetCable.invertCableDirection ? 1f - targetCable.ActivationAmount : targetCable.ActivationAmount;

        if (adjustCableScale)
        {
            var axis = targetCable.Axis.normalized;
            var wantedLength = LengthFromActivation;
            if (rotateViaOffset)
            {
                var wantedPosition = targetCable.GetCableConnectionPosition() + targetCable.EndOffset;
                var cableRoot = CableRootPosition;
                var diff = wantedPosition - cableRoot;
                wantedLength = diff.magnitude;
            }
            var scaledLength = wantedLength / baseLength;
            var baseScale = cableBody.localScale - Vector3.Project(cableBody.localScale, axis);
            cableBody.localScale = baseScale + axis * scaledLength;

            var invertedModifier = (invertScaling ? -1f : 1f);
            // If pivot is in the center
            //cableBody.localPosition = worldAxis * (targetCable.length - wantedLength) * 0.5f * invertedModifier;
            if (!invertScaling)
            {
                cableBody.position = CableRootPosition - worldAxis * wantedLength;
            }
            lastActivationValue = activationAmount;
        }

        if (rotateViaOffset)
        {
            var cableRoot = CableRootPosition;
            var cableEnd = targetCable.GetCableConnectionPosition();
            var wantedPosition = cableEnd + targetCable.EndOffset;
            var wantedVector = wantedPosition - cableRoot;
            var currentVector = targetCable.WorldAxis * (invertScaling ? 1f : -1f);
            var forwardAxis = Vector3.Cross(targetCable.WorldAxis, targetCable.EndOffset.normalized);
            var angle = Vector3.SignedAngle(currentVector, wantedVector, forwardAxis);
            //cableBody.transform.rotation = Quaternion.AngleAxis(angle, forwardAxis);
            // If pivot is not on the root
            cableBody.transform.localRotation = Quaternion.identity;
            cableBody.RotateAround(cableRoot, forwardAxis, angle);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (targetCable != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetCable.GetCableConnectionPosition() + targetCable.EndOffset, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(CableRootPosition, 0.1f);
        }
    }
}
