using UnityEngine;
using NBG.LogicGraph;
using Plugs;

[RequireComponent(typeof(Hole))]
public class Charger : ActivatableNode
{
    [SerializeField]
    float chargeSpeed = 1;

    IChargeable chargableObject;
    Hole hole;

    protected override void Awake()
    {
        base.Awake();
        hole = GetComponent<Hole>();
        hole.onPlugIn += ConnectChargeable;
        hole.onPlugOut += DisconnectChargeable;
    }

    void OnDestroy()
    {
        hole.onPlugIn -= ConnectChargeable;
        hole.onPlugOut -= DisconnectChargeable;
    }

    void FixedUpdate()
    {
        if (chargableObject != null)
        {
            chargableObject.Charge(ActivationValue * chargeSpeed * Time.fixedDeltaTime);
        }
    }

    void ConnectChargeable()
    {
        chargableObject = hole.ActivePlug.GetComponent<IChargeable>();
    }

    void DisconnectChargeable()
    {
        chargableObject = null;
    }
}
