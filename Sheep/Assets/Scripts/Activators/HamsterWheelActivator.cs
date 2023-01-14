using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HamsterWheelActivator : ObjectActivator
{
    [SerializeField]
    float rotationNeededToGeneratePower = 0.5f;
    HingeJoint joint;
    // Start is called before the first frame update
    void Awake()
    {
        joint = GetComponent<HingeJoint>();
    }

    float lastAngle = 0f;

    // Update is called once per frame
    void FixedUpdate()
    {
        var currentAngle = joint.angle;
        var delta = Mathf.Abs(Mathf.DeltaAngle(currentAngle, lastAngle));
        ActivationAmount = Mathf.Clamp01(delta / rotationNeededToGeneratePower);
        lastAngle = currentAngle;
    }
}
