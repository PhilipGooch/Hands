using NBG.LogicGraph;
using System;
using UnityEngine;

public class HoleSocket : MonoBehaviour
{
    [NodeAPI("OnPowerChanged")]
    public event Action<float> OnPowerChanged;
    [SerializeField]
    [Tooltip("Indicate if this socket outputs power or receives it. Used for managing the flow of electricity through a wire.")]
    bool isActive = true;
    public bool IsActiveSocket => isActive;

    float power = 0f;
    public float Power
    {
        get
        {
            return power;
        }
        set
        {
            power = Mathf.Clamp01(value);
            OnPowerChanged?.Invoke(power);
        }
    }
}
