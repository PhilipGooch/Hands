using UnityEngine;
using NBG.LogicGraph;
using Plugs;

public class Discharger : ObjectActivator
{
    [SerializeField]
    [Range(0, 1)]
    float dischargeSpeed = 1;

    IDischargeable dischargableObject;
    Hole hole;
    bool active = true;

    void Awake()
    {
        hole = GetComponent<Hole>();
        hole.onPlugIn += ConnectDischargeable;
        hole.onPlugOut += DisconnectDischargeable;
    }

    void OnDestroy()
    {
        hole.onPlugIn -= ConnectDischargeable;
        hole.onPlugOut -= DisconnectDischargeable;
    }

    void FixedUpdate()
    {
        if (dischargableObject != null && active)
        {
            ActivationAmount = dischargableObject.Discharge(dischargeSpeed * Time.fixedDeltaTime);
        }
        else
        {
            ActivationAmount = 0;
        }
    }

    [NodeAPI("Set Active")]
    public void SetActive(bool active)
    {
        this.active = active;
    }

    void ConnectDischargeable()
    {
        dischargableObject = hole.ActivePlug.GetComponent<IDischargeable>();
    }

    void DisconnectDischargeable()
    {
        dischargableObject = null;
    }
}
