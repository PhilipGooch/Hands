using Recoil;
using UnityEngine;

public class AddImpulse : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    new private Rigidbody rigidbody;
    private ReBody reBody;
    [SerializeField]
    Vector3 torque;
    [SerializeField]
    Vector3 force;

    private void OnValidate()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        reBody = new ReBody(rigidbody);
        AddImpulseAndTorque();
    }

    [ContextMenu("Add Impulse and Torque")]
    void AddImpulseAndTorque()
    {
        reBody.AddTorque(torque, ForceMode.Impulse);
        reBody.AddForce(force, ForceMode.Impulse);
    }
}
