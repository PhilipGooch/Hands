using UnityEngine;
using VR.System;

public class HandVisuals : MonoBehaviour
{
    HandDirection handDirection;

    float recoverPhase = 0f;
    Vector3 recoverFromPosition;
    Quaternion recoverFromRotation;

    Transform Model => VRSystem.Instance.GetModel(handDirection);

    private void Awake()
    {
        handDirection = GetComponent<Hand>().handDirection;
    }

    public void RecoverVisualPosition()
    {
        recoverPhase = 0;
        recoverFromPosition = Model.localPosition;
        recoverFromRotation = Model.localRotation;
    }

    public void UpdateVisuals(Vector3 pos, Quaternion rot)
    {
        recoverPhase += Time.deltaTime / .25f; // 250ms
        if (recoverPhase > 1) recoverPhase = 1;
        var mix = 1 - (1 - recoverPhase) * (1 - recoverPhase); // easeout quad
    
        Model.localPosition = Vector3.Lerp(recoverFromPosition, Vector3.zero, mix);
        Model.localRotation = Quaternion.Slerp(recoverFromRotation, Quaternion.identity, mix);
    
        transform.SetPositionAndRotation(pos, rot);
    }
}
