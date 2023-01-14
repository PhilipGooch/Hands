using UnityEngine;
using NBG.LogicGraph;

public class Battery : MonoBehaviour, IChargeable, IDischargeable
{
    [SerializeField]
    float capacity = 3;
    [SerializeField]
    float power = 0;

    [NodeAPI("Get Power")]
    public float GetPower()
    {
        return power;
    }

    [NodeAPI("Get Capacity")]
    public float GetCapacity()
    {
        return capacity; 
    }

    public void Charge(float power)
    {
        this.power += power;
        this.power = Mathf.Clamp(this.power, 0, capacity);
    }

    public float Discharge(float power)
    {
        this.power -= power;
        this.power = Mathf.Clamp(this.power, 0, capacity);
        return this.power > 0 ? 1 : 0;
    }
}
