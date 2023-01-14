using NBG.LogicGraph;
using UnityEngine;

public class HingeMotor : MonoBehaviour
{
    [NodeAPI("Speed")]
    public float Speed { get; set; }

    HingeJoint hinge;
    ConfigurableJoint configurable;
    protected void Start()
    {
        hinge = GetComponent<HingeJoint>();
        configurable = GetComponent<ConfigurableJoint>();
    }
    public void FixedUpdate()
    {
        if (hinge != null)
        {
            var m = hinge.motor;
            m.targetVelocity = Speed;
            hinge.motor = m;
        }
        if (configurable != null)
            configurable.targetAngularVelocity = new Vector3(Speed, 0, 0);
    }

}
