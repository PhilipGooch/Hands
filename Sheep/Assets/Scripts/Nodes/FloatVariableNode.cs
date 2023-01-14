using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.LogicGraph;

public class FloatVariableNode : MonoBehaviour
{
    [NodeAPI("Float Variable")]
    public float Value
    {
        get { return variableValue; }
        set { variableValue = value; }
    }

    [SerializeField]
    float variableValue = 0f;
}
