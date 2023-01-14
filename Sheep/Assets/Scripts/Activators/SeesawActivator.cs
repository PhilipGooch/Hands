using NBG.LogicGraph;
using System;
using UnityEngine;

public class SeesawActivator : MonoBehaviour
{
    [NodeAPI("OnActivation")]
    public event Activation onActivation;

    public delegate void Activation(float right, float left);

    [SerializeField]
    HingeJoint joint;

    float maxLimit;
    float minLimit;

    private void OnValidate()
    {
        if (joint == null)
        {
            joint = GetComponent<HingeJoint>();
        }
    }

    void Start()
    {
        maxLimit = joint.limits.max;
        minLimit = joint.limits.min;
    }

    void FixedUpdate()
    {
        var activation = Mathf.InverseLerp(maxLimit, minLimit, joint.angle);

        onActivation?.Invoke(activation, 1 - activation);

    }
}
